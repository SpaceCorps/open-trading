namespace OpenTrading.Models;

public class TradingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Position? UpdatedPosition { get; set; }
    public TradingAction? Action { get; set; }
}

