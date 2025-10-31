namespace OpenTrading.Models;

public enum ActionType
{
    Buy,
    Sell,
    Hold
}

public class TradingAction
{
    public ActionType Action { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public int Amount { get; set; }
    public DateTime Date { get; set; }
    public decimal Price { get; set; }
    public decimal? TotalCost { get; set; }
    public string? AgentId { get; set; }
    public string? Reasoning { get; set; }
}

