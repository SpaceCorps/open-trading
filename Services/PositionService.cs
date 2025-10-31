using System.IO;
using System.Text.Json;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class PositionService : IPositionService
{
    private readonly string _basePath = "./Data/Agents";

    public PositionService()
    {
        Directory.CreateDirectory(_basePath);
    }

    public async Task<Position?> GetCurrentPositionAsync(string agentId, DateTime date)
    {
        var filePath = System.IO.Path.Combine(_basePath, agentId, "positions", "position.jsonl");
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        Position? latestPosition = null;
        DateTime latestDate = DateTime.MinValue;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var entry = JsonSerializer.Deserialize<PositionJsonlEntry>(line);
                if (entry != null && entry.Date <= date && entry.Date > latestDate)
                {
                    var positions = entry.Positions ?? new Dictionary<string, decimal>();
                    latestPosition = new Position
                    {
                        Date = entry.Date,
                        AgentId = entry.AgentId,
                        Holdings = positions.Where(p => p.Key != "CASH")
                            .ToDictionary(p => p.Key, p => (int)p.Value),
                        Cash = positions.GetValueOrDefault("CASH", 0)
                    };
                    latestDate = entry.Date;
                }
            }
            catch (Exception)
            {
                // Skip invalid entries
            }
        }

        return latestPosition;
    }

    public async Task SavePositionAsync(Position position)
    {
        var agentPath = System.IO.Path.Combine(_basePath, position.AgentId, "positions");
        Directory.CreateDirectory(agentPath);
        
        var filePath = System.IO.Path.Combine(agentPath, "position.jsonl");
        
        var entry = new PositionJsonlEntry
        {
            Date = position.Date,
            AgentId = position.AgentId,
            Positions = position.Holdings.ToDictionary(
                h => h.Key,
                h => (decimal)h.Value
            )
        };
        entry.Positions["CASH"] = position.Cash;

        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
    }

    public Task<Dictionary<string, decimal>> CalculatePortfolioValueAsync(string agentId, DateTime date, List<StockPrice> prices)
    {
        var position = GetCurrentPositionAsync(agentId, date).Result;
        if (position == null)
        {
            return Task.FromResult(new Dictionary<string, decimal>());
        }

        var priceDict = prices.ToDictionary(p => p.Symbol, p => p.Close);
        var portfolioValue = position.Holdings.ToDictionary(
            h => h.Key,
            h => (decimal)h.Value * priceDict.GetValueOrDefault(h.Key, 0m)
        );
        portfolioValue["CASH"] = position.Cash;

        return Task.FromResult(portfolioValue);
    }

    public async Task<List<Position>> GetPositionHistoryAsync(string agentId, DateTime startDate, DateTime endDate)
    {
        var filePath = System.IO.Path.Combine(_basePath, agentId, "positions", "position.jsonl");
        
        if (!File.Exists(filePath))
        {
            return new List<Position>();
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        var positions = new List<Position>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var entry = JsonSerializer.Deserialize<PositionJsonlEntry>(line);
                if (entry != null && entry.Date >= startDate && entry.Date <= endDate)
                {
                    var entryPositions = entry.Positions ?? new Dictionary<string, decimal>();
                    positions.Add(new Position
                    {
                        Date = entry.Date,
                        AgentId = entry.AgentId,
                        Holdings = entryPositions.Where(p => p.Key != "CASH")
                            .ToDictionary(p => p.Key, p => (int)p.Value),
                        Cash = entryPositions.GetValueOrDefault("CASH", 0)
                    });
                }
            }
            catch (Exception)
            {
                // Skip invalid entries
            }
        }

        return positions.OrderBy(p => p.Date).ToList();
    }

    private class PositionJsonlEntry
    {
        public DateTime Date { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public Dictionary<string, decimal>? Positions { get; set; }
    }
}

