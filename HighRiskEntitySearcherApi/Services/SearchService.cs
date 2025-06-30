using HighRiskEntitySearcherApi.Models;
using HighRiskEntitySearcherApi.Services.Clients;

namespace HighRiskEntitySearcherApi.Services;


public class SearchService : ISearchService
{
    private readonly IOffshoreLeaksClient _offshoreLeaksClient;
    private readonly IWorldBankClient _worldBankClient;
    private readonly IOFACClient _ofacClient; 

    public SearchService(
        IOffshoreLeaksClient offshoreLeaksClient,
        IWorldBankClient worldBankClient,
        IOFACClient ofacClient)
    {
        _offshoreLeaksClient = offshoreLeaksClient;
        _worldBankClient = worldBankClient;
        _ofacClient = ofacClient;
    }

    public async Task<SearchResponse> SearchEntityAsync(string entityName)
    {
        // Añadir las tareas de busqueda
        var offshoreTask = _offshoreLeaksClient.SearchAsync(entityName);
        var worldBankTask = _worldBankClient.SearchAsync(entityName);
        var ofacTask = _ofacClient.SearchAsync(entityName);

        // Esperar a que todas terminen
        await Task.WhenAll(offshoreTask, worldBankTask, ofacTask);

        var allHits = new List<Hit>();
        allHits.AddRange(await offshoreTask);
        allHits.AddRange(await worldBankTask);
        allHits.AddRange(await ofacTask);

        return new SearchResponse
        {
            HitsFound = allHits.Count,
            Results = allHits
        };
    }
}
