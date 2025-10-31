using OpenTrading.Models;

namespace OpenTrading.Services;

public interface IStockDataService
{
    Task<List<StockPrice>> GetDailyPricesAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<StockPrice?> GetPriceAsync(string symbol, DateTime date);
    Task<List<string>> GetSymbolsAsync(); // NASDAQ 100 list
    Task<Dictionary<string, StockPrice>> GetPricesForDateAsync(DateTime date);
    Task LoadOrFetchDataAsync(DateTime startDate, DateTime endDate);
}

