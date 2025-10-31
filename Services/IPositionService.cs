using OpenTrading.Models;

namespace OpenTrading.Services;

public interface IPositionService
{
    Task<Position?> GetCurrentPositionAsync(string agentId, DateTime date);
    Task SavePositionAsync(Position position);
    Task<Dictionary<string, decimal>> CalculatePortfolioValueAsync(string agentId, DateTime date, List<StockPrice> prices);
    Task<List<Position>> GetPositionHistoryAsync(string agentId, DateTime startDate, DateTime endDate);
}

