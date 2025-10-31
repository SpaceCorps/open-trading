using OpenTrading.Models;
using OpenTrading.Services;

namespace OpenTrading.Apps;

[App(icon: Icons.TrendingUp, title: "Agent Performance")]
public class AgentPerformanceApp : ViewBase
{
    private IConfigService? _configService;
    private IPositionService? _positionService;
    private IStockDataService? _stockDataService;

    public override object? Build()
    {
        _configService = UseService<IConfigService>();
        _positionService = UseService<IPositionService>();
        _stockDataService = UseService<IStockDataService>();

        var config = _configService.LoadConfigAsync().Result;
        var startDate = UseState(config.DateRange.InitDate);
        var endDate = UseState(config.DateRange.EndDate);

        var agents = config.Models.Where(m => m.Enabled).Select(m => m.Name).ToList();
        
        if (!agents.Any())
            return Text.Block("No enabled agents found");

        return Layout.Vertical()
            .Gap(4)
            .Padding(2)
            | new Card(
                Layout.Vertical()
                    .Gap(3)
                    | Text.H2("Agent Performance Comparison")
                    | Layout.Horizontal()
                        .Gap(2)
                        | (Layout.Vertical()
                            | Text.Small("Start Date")
                            | startDate.ToDateInput())
                        | (Layout.Vertical()
                            | Text.Small("End Date")
                            | endDate.ToDateInput())
                )
                .Width(Size.Units(120).Max(1000))
            | BuildPerformanceComparison(agents, startDate.Value, endDate.Value);
    }

    private object BuildPerformanceComparison(List<string> agents, DateTime startDate, DateTime endDate)
    {
        if (_positionService == null || _stockDataService == null)
            return Text.Block("Services not initialized");

        var performanceData = new List<object>();

        foreach (var agent in agents)
        {
            var positions = _positionService.GetPositionHistoryAsync(agent, startDate, endDate).Result;
            if (!positions.Any())
                continue;

            var initialPosition = positions.First();
            var finalPosition = positions.Last();
            
            var prices = _stockDataService.GetPricesForDateAsync(finalPosition.Date).Result;
            var finalValue = finalPosition.GetPortfolioValue(
                prices.ToDictionary(p => p.Key, p => p.Value.Close));
            
            var initialPrices = _stockDataService.GetPricesForDateAsync(initialPosition.Date).Result;
            var initialValue = initialPosition.GetPortfolioValue(
                initialPrices.ToDictionary(p => p.Key, p => p.Value.Close));

            var returnPercent = initialValue > 0 
                ? ((finalValue - initialValue) / initialValue) * 100 
                : 0;

            performanceData.Add(new
            {
                Agent = agent,
                StartValue = initialValue,
                EndValue = finalValue,
                Return = finalValue - initialValue,
                ReturnPercent = returnPercent,
                Trades = positions.Count(p => p.LastAction != null)
            });
        }

        if (!performanceData.Any())
            return Text.Block("No performance data available for the selected date range");

        var typedData = performanceData
            .Cast<dynamic>()
            .Select(p => new
            {
                Agent = (string)p.Agent,
                StartValue = (decimal)p.StartValue,
                EndValue = (decimal)p.EndValue,
                Return = (decimal)p.Return,
                ReturnPercent = (double)p.ReturnPercent,
                Trades = (int)p.Trades
            })
            .AsQueryable();

        return Layout.Vertical()
            .Gap(4)
            | new Card(
                typedData.ToDataTable()
                    .Header(p => p.Agent, "Agent")
                    .Header(p => p.StartValue, "Start Value")
                    .Header(p => p.EndValue, "End Value")
                    .Header(p => p.Return, "Return ($)")
                    .Header(p => p.ReturnPercent, "Return (%)")
                    .Header(p => p.Trades, "Trades")
                    .Align(p => p.StartValue, Align.Right)
                    .Align(p => p.EndValue, Align.Right)
                    .Align(p => p.Return, Align.Right)
                    .Align(p => p.ReturnPercent, Align.Right)
                )
                .Width(Size.Units(120).Max(1000))
            | new Card(
                BuildPerformanceChart(performanceData)
                )
                .Width(Size.Units(120).Max(1000))
                .Height(Size.Units(40));
    }

    private object BuildPerformanceChart(List<object> performanceData)
    {
        var chartData = performanceData
            .Cast<dynamic>()
            .Select(p => new
            {
                Agent = (string)p.Agent,
                ReturnPercent = (double)p.ReturnPercent
            })
            .ToArray();

        return chartData.ToBarChart()
            .Dimension("Agent", e => e.Agent)
            .Measure("ReturnPercent", e => e.Sum(f => f.ReturnPercent));
    }
}

