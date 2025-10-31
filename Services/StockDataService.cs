using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class StockDataService : IStockDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockDataService> _logger;
    private readonly Dictionary<string, List<StockPrice>> _cache = new();
    private readonly string _dataPath = "./Data/Prices";
    
    // NASDAQ 100 symbols
    private static readonly string[] Nasda100Symbols = new[]
    {
        "AAPL", "MSFT", "GOOGL", "GOOG", "AMZN", "NVDA", "META", "TSLA", "AVGO", "COST",
        "NFLX", "AMD", "PEP", "ADBE", "CSCO", "CMCSA", "QCOM", "INTU", "AMGN", "ISRG",
        "BKNG", "AMAT", "VRSK", "ASML", "HON", "ADI", "PAYX", "SBUX", "FISV", "KLAC",
        "CDNS", "SNPS", "CTSH", "AEP", "DXCM", "MAR", "FTNT", "NXPI", "MRVL", "EXC",
        "FAST", "ODFL", "LRCX", "KDP", "ANSS", "WBD", "VRTX", "CRWD", "IDXX", "CTAS",
        "BKR", "GEHC", "ZS", "WDAY", "DDOG", "ON", "TTD", "TEAM", "GFS", "DOCN",
        "PCAR", "ALGN", "ROST", "ADSK", "ENPH", "CPRT", "CDW", "PDD", "CHH", "ZS",
        "MNST", "ALGN", "ROST", "ADSK", "ENPH", "CPRT", "CDW", "PDD", "CHH", "ZS",
        "MNST", "LCID", "RIVN", "MRNA", "PTON", "AFRM", "SNAP", "ROKU", "HOOD", "COIN",
        "SOFI", "PLTR", "UPST", "OPEN", "W", "GME", "AMC", "BBBY", "CLOV", "WISH"
    };

    public StockDataService(IHttpClientFactory httpClientFactory, ILogger<StockDataService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        
        Directory.CreateDirectory(_dataPath);
    }

    public Task<List<string>> GetSymbolsAsync()
    {
        return Task.FromResult(Nasda100Symbols.ToList());
    }

    public async Task<List<StockPrice>> GetDailyPricesAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        var key = $"{symbol}_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var filePath = System.IO.Path.Combine(_dataPath, $"{symbol}.jsonl");
        var prices = new List<StockPrice>();

        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var price = JsonSerializer.Deserialize<StockPrice>(line);
                    if (price != null && price.Date >= startDate && price.Date <= endDate)
                    {
                        prices.Add(price);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse price data: {Line}", line);
                }
            }
        }

        if (!prices.Any())
        {
            // Generate mock data for testing
            prices = GenerateMockPrices(symbol, startDate, endDate);
            await SavePricesToFileAsync(symbol, prices);
        }

        _cache[key] = prices;
        return prices;
    }

    public async Task<StockPrice?> GetPriceAsync(string symbol, DateTime date)
    {
        var prices = await GetDailyPricesAsync(symbol, date.Date, date.Date.AddDays(1));
        return prices.FirstOrDefault(p => p.Date.Date == date.Date);
    }

    public async Task<Dictionary<string, StockPrice>> GetPricesForDateAsync(DateTime date)
    {
        var symbols = await GetSymbolsAsync();
        var prices = new Dictionary<string, StockPrice>();
        
        foreach (var symbol in symbols)
        {
            var price = await GetPriceAsync(symbol, date);
            if (price != null)
            {
                prices[symbol] = price;
            }
        }
        
        return prices;
    }

    public async Task LoadOrFetchDataAsync(DateTime startDate, DateTime endDate)
    {
        var symbols = await GetSymbolsAsync();
        foreach (var symbol in symbols)
        {
            var prices = await GetDailyPricesAsync(symbol, startDate, endDate);
            if (!prices.Any())
            {
                // Generate mock data
                var mockPrices = GenerateMockPrices(symbol, startDate, endDate);
                await SavePricesToFileAsync(symbol, mockPrices);
            }
        }
    }

    private List<StockPrice> GenerateMockPrices(string symbol, DateTime startDate, DateTime endDate)
    {
        var random = new Random(symbol.GetHashCode());
        var basePrice = 100 + random.NextDouble() * 200;
        var prices = new List<StockPrice>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var change = (random.NextDouble() - 0.5) * 10;
            basePrice += change;
            basePrice = Math.Max(10, Math.Min(500, basePrice));

            prices.Add(new StockPrice
            {
                Symbol = symbol,
                Date = date,
                Open = (decimal)basePrice,
                High = (decimal)(basePrice + random.NextDouble() * 5),
                Low = (decimal)(basePrice - random.NextDouble() * 5),
                Close = (decimal)(basePrice + (random.NextDouble() - 0.5) * 3),
                Volume = (long)(random.NextDouble() * 10000000 + 1000000)
            });
        }

        return prices;
    }

    private async Task SavePricesToFileAsync(string symbol, List<StockPrice> prices)
    {
        var filePath = System.IO.Path.Combine(_dataPath, $"{symbol}.jsonl");
        var lines = prices.Select(p => JsonSerializer.Serialize(p));
        await File.WriteAllLinesAsync(filePath, lines);
    }
}

