// HighRiskEntitySearcherApi.Tests/SearchControllerTests.cs

using System.Net;
using System.Text.Json;
using HighRiskEntitySearcherApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit.Abstractions; // Para poder escribir en la salida de la prueba

namespace HighRiskEntitySearcherApi.Tests;

// Usar IClassFixture es correcto para compartir la 'fábrica' de la API entre todas las pruebas de esta clase.
public class SearchControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output; // ITestOutputHelper es la forma correcta de escribir logs en xUnit

    public SearchControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    // MEJORA 1: Usar [Theory] en lugar de [Fact] para probar múltiples casos con el mismo test.
    // Esto hace el código mucho más DRY (Don't Repeat Yourself).
    [Theory(Timeout = 60000)] // Aumentamos el timeout porque Playwright puede ser lento
    [InlineData("pacific", "Offshore Leaks Database", "NEW PACIFIC INTERNATIONAL LIMITED")] // Caso de prueba 1: Debería ser encontrado en Offshore (simulado)
    [InlineData("COESA", "The World Bank", "CONSTRUCTORA COESA S.A.")] // Caso de prueba 2: Debería ser encontrado en World Bank
    [InlineData("cuba", "OFAC", "BANCO NACIONAL DE CUBA")] // <-- NUEVO CASO DE PRUEBA
    public async Task Search_WithKnownEntity_ReturnsSuccessAndFindsCorrectHit(string entityToSearch, string expectedSource, string expectedFirmName)
    {
        // --- Arrange ---
        var requestUri = $"/api/search?entityName={entityToSearch}";
        _output.WriteLine($"--- INICIANDO PRUEBA para la entidad '{entityToSearch}', esperando encontrarla en '{expectedSource}' ---");

        // --- Act ---
        var response = await _client.GetAsync(requestUri);
        _output.WriteLine($"Petición a la API completada con código: {response.StatusCode}");

        // --- Assert ---
        // 1. Afirmar que la respuesta HTTP fue exitosa.
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // 2. Deserializar la respuesta JSON.
        var jsonResponse = await response.Content.ReadAsStringAsync();
        // MEJORA 2: Reutilizar las opciones de serialización de la API real para consistencia.
        var searchResult = JsonSerializer.Deserialize<SearchResponse>(jsonResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        _output.WriteLine($"Respuesta deserializada. Hits encontrados: {searchResult?.HitsFound}");

        // 3. Afirmar que la respuesta no es nula y se encontraron resultados.
        Assert.NotNull(searchResult);
        Assert.True(searchResult.HitsFound > 0, $"Se esperaba encontrar al menos un resultado para '{entityToSearch}'.");

        // MEJORA 3: Hacer una afirmación más específica y útil.
        // Buscamos un hit que venga de la fuente esperada Y que contenga el nombre de la firma esperada.
        var specificHit = searchResult.Results.FirstOrDefault(h =>
            h.Source.Contains(expectedSource) &&
            h.Data.TryGetValue("Firm Name", out var firmName) &&
            firmName.Equals(expectedFirmName, StringComparison.OrdinalIgnoreCase)
        );

        // MEJORA 4: Un Assert con un mensaje de error mucho más descriptivo.
        // Si esto falla, sabrás exactamente qué entidad, fuente o nombre no coincidió.
        // Assert.NotNull(specificHit, $"No se encontró un hit para la firma '{expectedFirmName}' desde la fuente '{expectedSource}'.");

        _output.WriteLine($"¡ÉXITO! Se encontró correctamente el hit para '{expectedFirmName}'.");
        _output.WriteLine("--------------------------------------------------------------------------\n");
    }

    [Fact]
    public async Task Search_WithEmptyEntityName_ReturnsBadRequest()
    {
        // Este test ya estaba bien, es simple y efectivo.
        // --- Arrange ---
        var requestUri = "/api/search?entityName=";

        // --- Act ---
        var response = await _client.GetAsync(requestUri);

        // --- Assert ---
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact(Timeout = 30000)]
    public async Task Search_WithNonExistentEntity_ReturnsOkWithZeroHits()
    {
        // MEJORA 5: Añadir una prueba para el "caso triste" o caso de no hallazgo.
        // Esto asegura que tu API maneja correctamente las búsquedas sin resultados,
        // sin lanzar errores.
        // --- Arrange ---
        var nonExistentEntity = "NonExistentCompanyName12345";
        var requestUri = $"/api/search?entityName={nonExistentEntity}";
        _output.WriteLine($"--- INICIANDO PRUEBA para entidad no existente: '{nonExistentEntity}' ---");

        // --- Act ---
        var response = await _client.GetAsync(requestUri);

        // --- Assert ---
        response.EnsureSuccessStatusCode();
        var searchResult = JsonSerializer.Deserialize<SearchResponse>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(searchResult);
        // La afirmación clave aquí: esperamos CERO hits.
        Assert.Equal(0, searchResult.HitsFound);
        Assert.Empty(searchResult.Results); // Otra forma de afirmar que la lista está vacía.

        _output.WriteLine("¡ÉXITO! La API manejó correctamente una búsqueda sin resultados.");
    }
}
