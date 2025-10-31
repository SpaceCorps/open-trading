using Microsoft.Extensions.Logging;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class SimulationService : ISimulationService
{
    private readonly IAgentService _agentService;
    private readonly ILogger<SimulationService> _logger;

    public SimulationService(
        IAgentService agentService,
        ILogger<SimulationService> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    public async Task<Dictionary<string, List<TradingAction>>> RunMultiAgentSimulationAsync(
        DateTime date,
        List<string> agentIds,
        Dictionary<string, AgentConfig> agentConfigs)
    {
        var results = new Dictionary<string, List<TradingAction>>();
        
        _logger.LogInformation("Starting multi-agent simulation for {Date} with {Count} agents", date, agentIds.Count);

        // Run agents in parallel
        var tasks = agentIds.Select(async agentId =>
        {
            if (!agentConfigs.TryGetValue(agentId, out var config))
            {
                _logger.LogWarning("Config not found for agent {AgentId}", agentId);
                return new { AgentId = agentId, Actions = new List<TradingAction>() };
            }

            try
            {
                _logger.LogInformation("Running agent {AgentId} for date {Date}", agentId, date);
                var actions = await _agentService.RunTradingDayAsync(date, agentId, config);
                return new { AgentId = agentId, Actions = actions };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running agent {AgentId} for date {Date}", agentId, date);
                return new { AgentId = agentId, Actions = new List<TradingAction>() };
            }
        });

        var agentResults = await Task.WhenAll(tasks);
        
        foreach (var result in agentResults)
        {
            results[result.AgentId] = result.Actions;
        }

        _logger.LogInformation("Completed multi-agent simulation for {Date}. Total actions: {Count}",
            date, results.Sum(r => r.Value.Count));

        return results;
    }

    public async Task<Dictionary<string, List<TradingAction>>> RunDateRangeSimulationAsync(
        DateTime startDate,
        DateTime endDate,
        List<string> agentIds,
        Dictionary<string, AgentConfig> agentConfigs,
        IProgress<SimulationProgress>? progress = null)
    {
        var allResults = new Dictionary<string, List<TradingAction>>();
        
        // Get all trading days (exclude weekends)
        var tradingDays = new List<DateTime>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                tradingDays.Add(date);
            }
        }

        var totalDates = tradingDays.Count;
        var completedDates = 0;

        _logger.LogInformation("Starting date range simulation from {StartDate} to {EndDate} ({Count} trading days) with {AgentCount} agents",
            startDate, endDate, totalDates, agentIds.Count);

        foreach (var date in tradingDays)
        {
            var dateResults = await RunMultiAgentSimulationAsync(date, agentIds, agentConfigs);
            
            // Merge results
            foreach (var agentResult in dateResults)
            {
                if (!allResults.ContainsKey(agentResult.Key))
                {
                    allResults[agentResult.Key] = new List<TradingAction>();
                }
                allResults[agentResult.Key].AddRange(agentResult.Value);
            }

            completedDates++;
            
            progress?.Report(new SimulationProgress
            {
                CurrentDate = date,
                TotalDates = totalDates,
                CompletedDates = completedDates,
                TotalAgents = agentIds.Count,
                CompletedAgents = agentIds.Count
            });
        }

        _logger.LogInformation("Completed date range simulation. Total actions across all agents: {Count}",
            allResults.Sum(r => r.Value.Count));

        return allResults;
    }
}

