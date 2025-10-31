using OpenTrading.Models;
using OpenTrading.Services;
using System.Collections.Immutable;
using System.Diagnostics;

namespace OpenTrading.Apps.Simulations;

[App(icon: Icons.DollarSign, title: "Trading Arena")]
public class TradingArenaApp : ViewBase
{
    private IConfigService? _configService;
    private IStockDataService? _stockDataService;
    private IPositionService? _positionService;
    private IAgentService? _agentService;
    private ILogService? _logService;
    private ISimulationService? _simulationService;

    public override object? Build()
    {
        _configService = UseService<IConfigService>();
        _stockDataService = UseService<IStockDataService>();
        _positionService = UseService<IPositionService>();
        _agentService = UseService<IAgentService>();
        _logService = UseService<ILogService>();
        _simulationService = UseService<ISimulationService>();

        var config = _configService.LoadConfigAsync().Result;
        var selectedDate = UseState(config.DateRange.InitDate);
        var selectedAgent = UseState(config.Models.FirstOrDefault()?.Name ?? "");
        var isRunning = UseState(false);
        var runDateRange = UseState(false);
        var startDate = UseState(config.DateRange.InitDate);
        var endDate = UseState(config.DateRange.EndDate);

        return Layout.Vertical()
            .Gap(4)
            .Padding(2)
            | Text.H2("AI Trading Arena")
            | Text.Block("Watch multiple AI agents compete in stock trading")
            | new Separator()
            | Layout.Horizontal()
                .Gap(2)
                | (Layout.Vertical()
                    | Text.Small("Start Date")
                    | selectedDate.ToDateInput())
                | (Layout.Vertical()
                    | Text.Small("Agent")
                    | selectedAgent.ToSelectInput(
                        config.Models.Where(m => m.Enabled).Select(m => m.Name).ToOptions()))
                | new Button("Run Single Agent", 
                    onClick: async (Event<Button> e) =>
                    {
                        isRunning.Set(true);
                        await RunSimulationAsync(selectedDate.Value, selectedAgent.Value);
                        isRunning.Set(false);
                    },
                    variant: ButtonVariant.Primary)
                    .Disabled(isRunning.Value)
                | new Button("Run All Agents", 
                    onClick: async (Event<Button> e) =>
                    {
                        isRunning.Set(true);
                        await RunMultiAgentSimulationAsync(selectedDate.Value);
                        isRunning.Set(false);
                    },
                    variant: ButtonVariant.Secondary)
                    .Disabled(isRunning.Value)
                | new Button("Run Date Range Simulation",
                    onClick: (Event<Button> e) => { runDateRange.Set(true); },
                    variant: ButtonVariant.Outline)
            | (runDateRange.Value
                ? Layout.Vertical()
                    .Gap(2)
                    | Text.H3("Date Range Simulation")
                    | Layout.Horizontal()
                        .Gap(2)
                        | (Layout.Vertical()
                            | Text.Small("Start Date")
                            | startDate.ToDateInput())
                        | (Layout.Vertical()
                            | Text.Small("End Date")
                            | endDate.ToDateInput())
                        | new Button("Run Date Range Simulation",
                            onClick: async (Event<Button> e) =>
                            {
                                isRunning.Set(true);
                                await RunDateRangeSimulationAsync(startDate.Value, endDate.Value);
                                isRunning.Set(false);
                                runDateRange.Set(false);
                            },
                            variant: ButtonVariant.Primary)
                            .Disabled(isRunning.Value)
                        | new Button("Cancel",
                            onClick: (Event<Button> e) => { runDateRange.Set(false); },
                            variant: ButtonVariant.Secondary)
                : isRunning.Value
                    ? Layout.Vertical()
                        .Gap(2)
                        | Text.Block("Running simulation... Please wait.")
                        | Text.Block("This may take a few moments depending on the number of agents and trading days.")
                    : BuildAgentPerformanceViewAsync(selectedDate.Value, selectedAgent.Value).Result);
    }

    private async Task<object?> BuildAgentPerformanceViewAsync(DateTime date, string agentId)
    {
        if (_positionService == null || _stockDataService == null || _logService == null || _configService == null)
            return Text.Block("Services not initialized");

        var position = await _positionService.GetCurrentPositionAsync(agentId, date);
        if (position == null)
            return Text.Block($"No position data available for agent {agentId} on {date:yyyy-MM-dd}");

        var prices = await _stockDataService.GetPricesForDateAsync(date);
        var logs = await _logService.GetLogsAsync(agentId, date);
        var portfolioValue = position.GetPortfolioValue(
            prices.ToDictionary(p => p.Key, p => p.Value.Close));

        // Get position history for chart
        var config = await _configService.LoadConfigAsync();
        var positionHistory = await _positionService.GetPositionHistoryAsync(agentId, config.DateRange.InitDate, date);
        
        return Layout.Vertical()
            .Gap(4)
            | Text.H3($"Agent: {agentId}")
            | Text.Block($"Date: {date:yyyy-MM-dd}")
            | Layout.Horizontal()
                .Gap(4)
                | (Layout.Vertical()
                    | Text.Small("Portfolio Value")
                    | Text.Large($"${portfolioValue:F2}"))
                | (Layout.Vertical()
                    | Text.Small("Cash")
                    | Text.Large($"${position.Cash:F2}"))
                | (Layout.Vertical()
                    | Text.Small("Holdings Value")
                    | Text.Large($"${portfolioValue - position.Cash:F2}"))
            | new Separator()
            | (positionHistory.Count > 1
                ? Layout.Vertical()
                    .Gap(2)
                    | Text.H3("Portfolio Value Over Time")
                    | BuildPortfolioChartAsync(positionHistory).Result
                : null)
            | new Separator()
            | Text.H3("Current Holdings")
            | (position.Holdings.Any()
                ? BuildHoldingsTable(position, prices)
                : Text.Block("No holdings"))
            | new Separator()
            | Text.H3("Trading Log")
            | BuildTradingLog(logs);
    }

    private object BuildHoldingsTable(Position position, Dictionary<string, StockPrice> prices)
    {
        var holdingsData = position.Holdings
            .Select(h => new
            {
                Symbol = h.Key,
                Shares = h.Value,
                Price = prices.GetValueOrDefault(h.Key)?.Close ?? 0m,
                Value = h.Value * (prices.GetValueOrDefault(h.Key)?.Close ?? 0m)
            })
            .OrderByDescending(h => h.Value)
            .AsQueryable();

        return holdingsData.ToDataTable()
            .Header(h => h.Symbol, "Symbol")
            .Header(h => h.Shares, "Shares")
            .Header(h => h.Price, "Current Price")
            .Header(h => h.Value, "Total Value")
            .Align(h => h.Price, Align.Right)
            .Align(h => h.Value, Align.Right);
    }

    private object BuildTradingLog(List<TradingLog> logs)
    {
        if (!logs.Any())
            return Text.Block("No trading activity for this date");

        var logData = logs
            .OrderBy(l => l.Step)
            .Select(l => new
            {
                Step = l.Step,
                Time = l.Timestamp.ToString("HH:mm:ss"),
                Message = l.Message,
                Reasoning = l.Reasoning?.Substring(0, Math.Min(100, l.Reasoning?.Length ?? 0)) ?? ""
            })
            .AsQueryable();

        return logData.ToDataTable()
            .Header(l => l.Step, "Step")
            .Header(l => l.Time, "Time")
            .Header(l => l.Message, "Message")
            .Header(l => l.Reasoning, "Reasoning");
    }

    private async Task RunSimulationAsync(DateTime date, string agentId)
    {
        if (_agentService == null || _configService == null)
            return;

        var config = await _configService.LoadConfigAsync();
        var agentConfig = config.Models.FirstOrDefault(m => m.Name == agentId);
        
        if (agentConfig == null)
            return;

        var agentConfigFull = new AgentConfig
        {
            Name = agentConfig.Name,
            BaseModel = agentConfig.BaseModel,
            Signature = agentConfig.Signature,
            Enabled = agentConfig.Enabled,
            MaxSteps = config.AgentConfig.MaxSteps,
            MaxRetries = config.AgentConfig.MaxRetries,
            BaseDelay = config.AgentConfig.BaseDelay,
            InitialCash = config.AgentConfig.InitialCash,
            ApiKey = agentConfig.ApiKey
        };

        await _agentService.RunTradingDayAsync(date, agentId, agentConfigFull);
    }

    private async Task RunMultiAgentSimulationAsync(DateTime date)
    {
        if (_simulationService == null || _configService == null)
            return;

        var config = await _configService.LoadConfigAsync();
        var enabledAgents = config.Models.Where(m => m.Enabled).ToList();
        
        if (!enabledAgents.Any())
        {
            return;
        }

        var agentConfigs = new Dictionary<string, AgentConfig>();
        foreach (var agent in enabledAgents)
        {
            agentConfigs[agent.Name] = new AgentConfig
            {
                Name = agent.Name,
                BaseModel = agent.BaseModel,
                Signature = agent.Signature,
                Enabled = agent.Enabled,
                MaxSteps = config.AgentConfig.MaxSteps,
                MaxRetries = config.AgentConfig.MaxRetries,
                BaseDelay = config.AgentConfig.BaseDelay,
                InitialCash = config.AgentConfig.InitialCash,
                ApiKey = agent.ApiKey
            };
        }

        var agentIds = enabledAgents.Select(a => a.Name).ToList();
        await _simulationService.RunMultiAgentSimulationAsync(date, agentIds, agentConfigs);
    }

    private async Task<object> BuildPortfolioChartAsync(List<Position> positionHistory)
    {
        var chartData = new List<(DateTime Date, decimal PortfolioValue)>();
        
        foreach (var p in positionHistory.OrderBy(p => p.Date))
        {
            var prices = _stockDataService != null 
                ? await _stockDataService.GetPricesForDateAsync(p.Date)
                : new Dictionary<string, StockPrice>();
            var portfolioValue = p.GetPortfolioValue(prices.ToDictionary(pr => pr.Key, pr => pr.Value.Close));
            chartData.Add((p.Date, portfolioValue));
        }

        var chartArray = chartData
            .Select(d => new { Date = d.Date, PortfolioValue = d.PortfolioValue })
            .ToArray();

        return chartArray.ToLineChart(style: LineChartStyles.Dashboard)
            .Dimension("Date", e => e.Date)
            .Measure("PortfolioValue", e => e.Sum(f => f.PortfolioValue));
    }

    private async Task RunDateRangeSimulationAsync(DateTime startDate, DateTime endDate)
    {
        if (_simulationService == null || _configService == null)
            return;

        var config = await _configService.LoadConfigAsync();
        var enabledAgents = config.Models.Where(m => m.Enabled).ToList();
        
        if (!enabledAgents.Any())
        {
            return;
        }

        var agentConfigs = new Dictionary<string, AgentConfig>();
        foreach (var agent in enabledAgents)
        {
            agentConfigs[agent.Name] = new AgentConfig
            {
                Name = agent.Name,
                BaseModel = agent.BaseModel,
                Signature = agent.Signature,
                Enabled = agent.Enabled,
                MaxSteps = config.AgentConfig.MaxSteps,
                MaxRetries = config.AgentConfig.MaxRetries,
                BaseDelay = config.AgentConfig.BaseDelay,
                InitialCash = config.AgentConfig.InitialCash,
                ApiKey = agent.ApiKey
            };
        }

        var agentIds = enabledAgents.Select(a => a.Name).ToList();
        await _simulationService.RunDateRangeSimulationAsync(startDate, endDate, agentIds, agentConfigs);
    }
}

