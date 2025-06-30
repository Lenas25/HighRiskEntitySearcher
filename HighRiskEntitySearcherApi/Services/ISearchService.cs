
using HighRiskEntitySearcherApi.Models;

namespace HighRiskEntitySearcherApi.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchEntityAsync(string entityName);
}
