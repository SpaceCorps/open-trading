namespace OpenTrading.Models;

public class TradingLog
{
    public DateTime Date { get; set; }
    public string AgentId { get; set; } = string.Empty;
    public int Step { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Reasoning { get; set; }
    public TradingAction? Action { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

