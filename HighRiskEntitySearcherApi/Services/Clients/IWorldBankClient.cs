// HighRiskEntitySearcherApi/Services/Clients/IWorldBankClient.cs
using HighRiskEntitySearcherApi.Models;

namespace HighRiskEntitySearcherApi.Services.Clients;

public interface IWorldBankClient
{
    Task<List<Hit>> SearchAsync(string entityName);
}
