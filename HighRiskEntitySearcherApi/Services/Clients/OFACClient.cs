using HighRiskEntitySearcherApi.Models;
using Microsoft.Playwright;
using HtmlAgilityPack;

namespace HighRiskEntitySearcherApi.Services.Clients;

public class OFACClient : IOFACClient
{
    private const string SearchUrl = "https://sanctionssearch.ofac.treas.gov/";

    public async Task<List<Hit>> SearchAsync(string entityName)
    {
        var results = new List<Hit>();
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync();

        try
        {
            Console.WriteLine("Navegando a la página de búsqueda de OFAC...");
            await page.GotoAsync(SearchUrl);

            // --- Simulación de la Interacción del Usuario ---
            await page.FillAsync("#ctl00_MainContent_txtLastName", entityName);

            // Hacer clic en el botón de búsqueda.
            Console.WriteLine($"Buscando '{entityName}' en OFAC...");
            await page.ClickAsync("#ctl00_MainContent_btnSearch");

            // Esperar a que la página de resultados cargue.
            // Esperamos a que la tabla de resultados sea visible.
            var resultTableSelector = "#gvSearchResults";
            Console.WriteLine("Esperando la tabla de resultados de OFAC...");
            await page.WaitForSelectorAsync(resultTableSelector, new() { Timeout = 15000 });
            Console.WriteLine("Tabla de resultados de OFAC encontrada.");

            // Extraer el HTML de la página de resultados
            var htmlContent = await page.ContentAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // El selector busca las filas de la tabla de resultados, saltando la cabecera.
            var resultRows = htmlDoc.DocumentNode.SelectNodes("//table[@id='gvSearchResults']/tbody/tr");

            if (resultRows != null)
            {

                foreach (var row in resultRows)
                {
                    var nameNode = row.SelectSingleNode(".//td[1]/a");

                    var addressNode = row.SelectSingleNode(".//td[2]");
                    var typeNode = row.SelectSingleNode(".//td[3]");
                    var programNode = row.SelectSingleNode(".//td[4]");
                    var listNode = row.SelectSingleNode(".//td[5]");
                    var scoreNode = row.SelectSingleNode(".//td[6]");

                    if (nameNode != null)
                    {
                        var hitData = new Dictionary<string, string>
                        {
                            // Usamos el operador '??' para poner "N/A" si algún nodo es nulo.
                            { "Name", nameNode.InnerText.Trim() },
                            { "Address", addressNode?.InnerText.Trim() ?? "N/A" },
                            { "Type", typeNode?.InnerText.Trim() ?? "N/A" },
                            { "Program(s)", programNode?.InnerText.Trim() ?? "N/A" },
                            { "List", listNode?.InnerText.Trim() ?? "N/A" },
                            {"Score", scoreNode?.InnerText.Trim() ?? "N/A"}
                        };
                        results.Add(new Hit { Source = "OFAC", Data = hitData });
                    }
                }
            }
            else
            {
                Console.WriteLine($"No se encontraron resultados en OFAC para '{entityName}'.");
            }
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Timeout o no se encontraron resultados en OFAC para '{entityName}'.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ocurrió un error inesperado al buscar en OFAC: {ex.Message}");
        }
        finally
        {
            await browser.CloseAsync();
        }

        Console.WriteLine("----------- RESULTADOS DEL OFAC -----------");
        Console.WriteLine($"Se encontraron {results.Count} resultados para '{entityName}'");
        Console.WriteLine("----------------------------------------------------------");

        return results;
    }
}
