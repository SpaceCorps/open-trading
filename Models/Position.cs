namespace OpenTrading.Models;

public class Position
{
    public DateTime Date { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public Dictionary<string, int> Holdings { get; set; } = new();
    public decimal Cash { get; set; }
    public TradingAction? LastAction { get; set; }
    
    public decimal GetPortfolioValue(Dictionary<string, decimal> prices)
    {
        var holdingsValue = Holdings.Sum(h => h.Value * prices.GetValueOrDefault(h.Key, 0m));
        return holdingsValue + Cash;
    }
}

