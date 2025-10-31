namespace OpenTrading.Models;

public class StockPrice
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
    
    // Aliases for buy/sell prices (using open for buy, close for sell in original)
    public decimal BuyPrice => Open;
    public decimal SellPrice => Close;
}

