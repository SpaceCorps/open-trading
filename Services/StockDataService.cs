using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class StockDataService : IStockDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockDataService> _logger;
    private readonly IConfiguration? _configuration;
    private readonly Dictionary<string, List<StockPrice>> _cache = new();
    private readonly string _dataPath = "./Data/Prices";
    private string? _alphaVantageApiKey;
    
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

    public StockDataService(
        IHttpClientFactory httpClientFactory, 
        ILogger<StockDataService> logger,
        IConfiguration? configuration = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _configuration = configuration;
        
        // Get API key from user secrets or environment variable
        _alphaVantageApiKey = _configuration?["ALPHA_VANTAGE_API_KEY"] 
            ?? Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY");
        
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
            // Try to fetch from Alpha Vantage API
            if (!string.IsNullOrEmpty(_alphaVantageApiKey))
            {
                try
                {
                    prices = await FetchFromAlphaVantageAsync(symbol, startDate, endDate);
                    if (prices.Any())
                    {
                        await SavePricesToFileAsync(symbol, prices);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch data from Alpha Vantage for {Symbol}, falling back to mock data", symbol);
                }
            }
            
            // Fall back to mock data if API fetch failed or no API key
            if (!prices.Any())
            {
                _logger.LogInformation("Using mock data for {Symbol} - configure ALPHA_VANTAGE_API_KEY for real data", symbol);
                prices = GenerateMockPrices(symbol, startDate, endDate);
                await SavePricesToFileAsync(symbol, prices);
            }
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
        
        _logger.LogDebug("Fetching prices for {Count} symbols on {Date}", symbols.Count, date);
        
        // Use parallel processing for better performance
        var tasks = symbols.Select(async symbol =>
        {
            try
            {
                var price = await GetPriceAsync(symbol, date);
                return new { Symbol = symbol, Price = price };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get price for {Symbol} on {Date}", symbol, date);
                return new { Symbol = symbol, Price = (StockPrice?)null };
            }
        });

        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            if (result.Price != null)
            {
                prices[result.Symbol] = result.Price;
            }
        }
        
        _logger.LogDebug("Successfully fetched prices for {Count}/{Total} symbols on {Date}", 
            prices.Count, symbols.Count, date);
        
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

    private async Task<List<StockPrice>> FetchFromAlphaVantageAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrEmpty(_alphaVantageApiKey))
        {
            return new List<StockPrice>();
        }

        var prices = new List<StockPrice>();
        var client = _httpClientFactory.CreateClient();
        
        // Alpha Vantage API endpoint
        var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY&symbol={symbol}&apikey={_alphaVantageApiKey}&outputsize=full&datatype=json";
        
        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            // Check for API errors
            if (root.TryGetProperty("Error Message", out var errorMsg))
            {
                _logger.LogWarning("Alpha Vantage API error: {Error}", errorMsg.GetString());
                return prices;
            }
            
            if (root.TryGetProperty("Note", out var note))
            {
                _logger.LogWarning("Alpha Vantage API rate limit: {Note}", note.GetString());
                return prices;
            }
            
            // Parse time series data
            if (root.TryGetProperty("Time Series (Daily)", out var timeSeries))
            {
                foreach (var property in timeSeries.EnumerateObject())
                {
                    var dateStr = property.Name;
                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        if (date >= startDate && date <= endDate)
                        {
                            var data = property.Value;
                            if (data.TryGetProperty("1. open", out var openEl) &&
                                data.TryGetProperty("2. high", out var highEl) &&
                                data.TryGetProperty("3. low", out var lowEl) &&
                                data.TryGetProperty("4. close", out var closeEl) &&
                                data.TryGetProperty("5. volume", out var volumeEl))
                            {
                                prices.Add(new StockPrice
                                {
                                    Symbol = symbol,
                                    Date = date,
                                    Open = decimal.Parse(openEl.GetString() ?? "0"),
                                    High = decimal.Parse(highEl.GetString() ?? "0"),
                                    Low = decimal.Parse(lowEl.GetString() ?? "0"),
                                    Close = decimal.Parse(closeEl.GetString() ?? "0"),
                                    Volume = long.Parse(volumeEl.GetString() ?? "0")
                                });
                            }
                        }
                    }
                }
            }
            
            // Sort by date ascending
            prices = prices.OrderBy(p => p.Date).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data from Alpha Vantage for {Symbol}", symbol);
            throw;
        }
        
        return prices;
    }

    private async Task SavePricesToFileAsync(string symbol, List<StockPrice> prices)
    {
        var filePath = System.IO.Path.Combine(_dataPath, $"{symbol}.jsonl");
        
        // Read existing prices and merge
        var existingPrices = new List<StockPrice>();
        if (File.Exists(filePath))
        {
            var lines = await File.ReadAllLinesAsync(filePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var price = JsonSerializer.Deserialize<StockPrice>(line);
                    if (price != null)
                    {
                        existingPrices.Add(price);
                    }
                }
                catch { }
            }
        }
        
        // Merge and deduplicate
        var allPrices = existingPrices
            .Concat(prices)
            .GroupBy(p => p.Date)
            .Select(g => g.First())
            .OrderBy(p => p.Date)
            .ToList();
        
        var linesToWrite = allPrices.Select(p => JsonSerializer.Serialize(p));
        await File.WriteAllLinesAsync(filePath, linesToWrite);
    }
}

