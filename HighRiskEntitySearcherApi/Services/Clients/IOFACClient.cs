using HighRiskEntitySearcherApi.Models;

namespace HighRiskEntitySearcherApi.Services.Clients;

public interface IOFACClient
{
    Task<List<Hit>> SearchAsync(string entityName);
}
