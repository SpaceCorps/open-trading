using System.Text.Json;
using System.IO;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class LogService : ILogService
{
    private readonly string _basePath = "./Data/Agents";

    public LogService()
    {
        Directory.CreateDirectory(_basePath);
    }

    public async Task SaveLogAsync(TradingLog log)
    {
        var logPath = System.IO.Path.Combine(_basePath, log.AgentId, "logs", log.Date.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(logPath);
        
        var filePath = System.IO.Path.Combine(logPath, "log.jsonl");
        
        var json = JsonSerializer.Serialize(log);
        await File.AppendAllTextAsync(filePath, json + Environment.NewLine);
    }

    public async Task<List<TradingLog>> GetLogsAsync(string agentId, DateTime date)
    {
        var filePath = System.IO.Path.Combine(_basePath, agentId, "logs", date.ToString("yyyy-MM-dd"), "log.jsonl");
        
        if (!File.Exists(filePath))
        {
            return new List<TradingLog>();
        }

        var lines = await File.ReadAllLinesAsync(filePath);
        var logs = new List<TradingLog>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            try
            {
                var log = JsonSerializer.Deserialize<TradingLog>(line);
                if (log != null)
                {
                    logs.Add(log);
                }
            }
            catch (Exception)
            {
                // Skip invalid entries
            }
        }

        return logs.OrderBy(l => l.Step).ToList();
    }

    public async Task<List<TradingLog>> GetLogsAsync(string agentId, DateTime startDate, DateTime endDate)
    {
        var allLogs = new List<TradingLog>();
        
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var logs = await GetLogsAsync(agentId, date);
            allLogs.AddRange(logs);
        }

        return allLogs.OrderBy(l => l.Date).ThenBy(l => l.Step).ToList();
    }
}

