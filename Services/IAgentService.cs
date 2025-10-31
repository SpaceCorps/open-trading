using OpenTrading.Models;

namespace OpenTrading.Services;

public class AgentContext
{
    public Position CurrentPosition { get; set; } = null!;
    public DateTime CurrentDate { get; set; }
    public Dictionary<string, StockPrice> AvailablePrices { get; set; } = new();
    public List<TradingAction> PreviousActions { get; set; } = new();
    public string AgentId { get; set; } = string.Empty;
    public AgentConfig Config { get; set; } = null!;
}

public interface IAgentService
{
    Task<TradingAction> DecideActionAsync(AgentContext context);
    Task<string> GetReasoningAsync(string prompt, AgentContext context);
    Task<List<TradingAction>> RunTradingDayAsync(DateTime date, string agentId, AgentConfig config);
}

