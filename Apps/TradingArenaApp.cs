using OpenTrading.Models;
using OpenTrading.Services;
using System.Collections.Immutable;

namespace OpenTrading.Apps;

[App(icon: Icons.DollarSign, title: "Trading Arena")]
public class TradingArenaApp : ViewBase
{
    private IConfigService? _configService;
    private IStockDataService? _stockDataService;
    private IPositionService? _positionService;
    private IAgentService? _agentService;
    private ILogService? _logService;

    public override object? Build()
    {
        _configService = UseService<IConfigService>();
        _stockDataService = UseService<IStockDataService>();
        _positionService = UseService<IPositionService>();
        _agentService = UseService<IAgentService>();
        _logService = UseService<ILogService>();

        var config = _configService.LoadConfigAsync().Result;
        var selectedDate = UseState(config.DateRange.InitDate);
        var selectedAgent = UseState(config.Models.FirstOrDefault()?.Name ?? "");
        var isRunning = UseState(false);

        return Layout.Vertical()
            .Gap(4)
            .Padding(2)
            | new Card(
                Layout.Vertical()
                    .Gap(3)
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
                        | new Button("Run Simulation", 
                            onClick: async (Event<Button> e) =>
                            {
                                isRunning.Set(true);
                                await RunSimulationAsync(selectedDate.Value, selectedAgent.Value);
                                isRunning.Set(false);
                            },
                            variant: ButtonVariant.Primary)
                            .Disabled(isRunning.Value)
                )
                .Width(Size.Units(120).Max(1000))
            | (isRunning.Value
                ? new Card(Text.Block("Running simulation... Please wait."))
                : BuildAgentPerformanceView(selectedDate.Value, selectedAgent.Value));
    }

    private object? BuildAgentPerformanceView(DateTime date, string agentId)
    {
        if (_positionService == null || _stockDataService == null || _logService == null)
            return Text.Block("Services not initialized");

        var position = _positionService.GetCurrentPositionAsync(agentId, date).Result;
        if (position == null)
            return Text.Block($"No position data available for agent {agentId} on {date:yyyy-MM-dd}");

        var prices = _stockDataService.GetPricesForDateAsync(date).Result;
        var logs = _logService.GetLogsAsync(agentId, date).Result;
        var portfolioValue = position.GetPortfolioValue(
            prices.ToDictionary(p => p.Key, p => p.Value.Close));

        return Layout.Vertical()
            .Gap(4)
            | new Card(
                Layout.Vertical()
                    .Gap(2)
                    | Text.H3($"Agent: {agentId}")
                    | Text.Block($"Date: {date:yyyy-MM-dd}")
                    | new Separator()
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
                )
                .Width(Size.Units(120).Max(1000))
            | new Card(
                Layout.Vertical()
                    .Gap(2)
                    | Text.H3("Current Holdings")
                    | (position.Holdings.Any()
                        ? BuildHoldingsTable(position, prices)
                        : Text.Block("No holdings"))
                )
                .Width(Size.Units(120).Max(1000))
            | new Card(
                Layout.Vertical()
                    .Gap(2)
                    | Text.H3("Trading Log")
                    | BuildTradingLog(logs)
                )
                .Width(Size.Units(120).Max(1000))
                .Height(Size.Units(40));
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

        return holdingsData.ToTable()
            .Width(Size.Full())
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

        return logData.ToTable()
            .Width(Size.Full())
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
}

