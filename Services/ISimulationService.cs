using OpenTrading.Models;

namespace OpenTrading.Services;

public interface ISimulationService
{
    Task<Dictionary<string, List<TradingAction>>> RunMultiAgentSimulationAsync(
        DateTime date, 
        List<string> agentIds,
        Dictionary<string, AgentConfig> agentConfigs);
    
    Task<Dictionary<string, List<TradingAction>>> RunDateRangeSimulationAsync(
        DateTime startDate,
        DateTime endDate,
        List<string> agentIds,
        Dictionary<string, AgentConfig> agentConfigs,
        IProgress<SimulationProgress>? progress = null);
}

public class SimulationProgress
{
    public DateTime CurrentDate { get; set; }
    public string? CurrentAgent { get; set; }
    public int TotalDates { get; set; }
    public int CompletedDates { get; set; }
    public int TotalAgents { get; set; }
    public int CompletedAgents { get; set; }
    public double ProgressPercent => TotalDates > 0 
        ? (double)CompletedDates / TotalDates * 100 
        : 0;
}

