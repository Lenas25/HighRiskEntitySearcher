using HighRiskEntitySearcherApi.Models;
using Microsoft.Playwright;
using System.Text.Json;
using System.Threading.Tasks;

namespace HighRiskEntitySearcherApi.Services.Clients;

public class WorldBankClient : IWorldBankClient
{
    private const string PageUrl = "https://projects.worldbank.org/en/projects-operations/procurement/debarred-firms";
    private const string InterceptUrlPattern = "https://apigwext.worldbank.org/dvsvc/v1.0/json/APPLICATION/ADOBE_EXPRNCE_MGR/FIRM/SANCTIONED_FIRM";

    public WorldBankClient() { }

    public async Task<List<Hit>> SearchAsync(string entityName)
    {
        var jsonContent = await InterceptApiCallAsync();


        if (string.IsNullOrEmpty(jsonContent))
        {
            return new List<Hit>();
        }

        var fullList = ParseJson(jsonContent);

        var results = fullList
            .Where(hit =>
                hit.Data.TryGetValue("Firm Name", out var firmName) &&
                firmName.Contains(entityName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Console.WriteLine("----------- RESULTADOS DEL WORLD BANK -----------");
        Console.WriteLine($"Se encontraron {results.Count} resultados para '{entityName}'");
        Console.WriteLine("----------------------------------------------------------");
        return results;
    }

    private async Task<string?> InterceptApiCallAsync()
    {
        var tcs = new TaskCompletionSource<string?>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        page.Response += async (_, response) =>
        {
            if (response.Url.StartsWith(InterceptUrlPattern) && response.Status == 200)
            {
                var json = await response.TextAsync();
                tcs.TrySetResult(json);
            }
        };

        try
        {
            await page.GotoAsync(PageUrl);
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(20000));
            return (completedTask == tcs.Task) ? await tcs.Task : null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error durante la intercepción con Playwright: {ex.Message}");
            return null;
        }
        finally
        {
            await browser.CloseAsync();
        }
    }

    private List<Hit> ParseJson(string jsonContent)
    {
        var results = new List<Hit>();
        try
        {
            var jsonDocument = JsonDocument.Parse(jsonContent);

            // Navegamos por la estructura real del JSON
            if (jsonDocument.RootElement.TryGetProperty("response", out var responseObject) &&
                responseObject.TryGetProperty("ZPROCSUPP", out var dataArray) &&
                dataArray.ValueKind == JsonValueKind.Array)
            {
                Console.WriteLine($"ParseJson: ¡Éxito! Se encontró el array 'ZPROCSUPP' con {dataArray.GetArrayLength()} elementos.");

                foreach (var firmElement in dataArray.EnumerateArray())
                {
                    // Extraer los atributos del JSON formando instancia conocida
                    var hitData = new Dictionary<string, string>
                    {
                        { "Firm Name", firmElement.TryGetProperty("SUPP_NAME", out var fn) ? fn.GetString() ?? "N/A" : "N/A" },
                        { "Address", firmElement.TryGetProperty("SUPP_ADDR", out var ad) ? ad.GetString() ?? "N/A" : "N/A" },
                        { "City", firmElement.TryGetProperty("SUPP_CITY", out var city) ? city.GetString() ?? "N/A" : "N/A" },
                        { "Country", firmElement.TryGetProperty("COUNTRY_NAME", out var co) ? co.GetString() ?? "N/A" : "N/A" },
                        { "From Date", firmElement.TryGetProperty("DEBAR_FROM_DATE", out var fd) ? fd.GetString() ?? "N/A" : "N/A" },
                        { "To Date", firmElement.TryGetProperty("DEBAR_TO_DATE", out var td) ? td.GetString() ?? "N/A" : "N/A" },
                        { "Grounds", firmElement.TryGetProperty("DEBAR_REASON", out var gr) ? gr.GetString() ?? "N/A" : "N/A" }
                    };
                    results.Add(new Hit { Source = "The World Bank", Data = hitData });
                }
            }
            else
            {
                Console.WriteLine("ParseJson ERROR: No se pudo encontrar la ruta 'response.ZPROCSUPP' en el JSON.");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error fatal al parsear el JSON: {ex.Message}");
        }
        return results;
    }
}
