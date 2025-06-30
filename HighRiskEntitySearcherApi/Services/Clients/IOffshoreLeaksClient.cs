// HighRiskEntitySearcherApi/Services/Clients/IOffshoreLeaksClient.cs

using HighRiskEntitySearcherApi.Models;

namespace HighRiskEntitySearcherApi.Services.Clients;

public interface IOffshoreLeaksClient
{
    Task<List<Hit>> SearchAsync(string entityName);
}
