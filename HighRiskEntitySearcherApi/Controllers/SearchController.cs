
using HighRiskEntitySearcherApi.Models;
using HighRiskEntitySearcherApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HighRiskEntitySearcherApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;
    private readonly ILogger<SearchController> _logger;

    public SearchController(ISearchService searchService, ILogger<SearchController> logger)
    {
        _searchService = searchService;
        _logger = logger;
    }

    // Aplica la política definida
    [HttpGet]
    [EnableRateLimiting("SearchApiMinuteLimit")]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return BadRequest("El parámetro 'entityName' es requerido.");
        }

        try
        {
            _logger.LogInformation("Iniciando búsqueda para la entidad: {EntityName}", entityName);
            var result = await _searchService.SearchEntityAsync(entityName);
            _logger.LogInformation("Búsqueda finalizada para {EntityName}, se encontraron {HitCount} resultados.", entityName, result.HitsFound);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error al buscar la entidad: {EntityName}", entityName);
            return StatusCode(500, "Ocurrió un error interno en el servidor. Revisa los logs para más detalles.");
        }
    }
}
