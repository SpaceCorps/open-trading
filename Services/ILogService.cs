using OpenTrading.Models;

namespace OpenTrading.Services;

public interface ILogService
{
    Task SaveLogAsync(TradingLog log);
    Task<List<TradingLog>> GetLogsAsync(string agentId, DateTime date);
    Task<List<TradingLog>> GetLogsAsync(string agentId, DateTime startDate, DateTime endDate);
}

