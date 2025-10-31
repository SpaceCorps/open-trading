using OpenTrading.Models;
using OpenTrading.Services;

namespace OpenTrading.Apps;

[App(icon: Icons.TrendingUp, title: "Stock Data")]
public class StockDataApp : ViewBase
{
    private IStockDataService? _stockDataService;

    public override object? Build()
    {
        _stockDataService = UseService<IStockDataService>();

        var symbols = _stockDataService?.GetSymbolsAsync().Result ?? new List<string>();
        var selectedSymbol = UseState(symbols.FirstOrDefault() ?? "AAPL");
        var startDate = UseState(DateTime.Today.AddMonths(-1));
        var endDate = UseState(DateTime.Today);

        return Layout.Vertical()
            .Gap(4)
            .Padding(2)
            | new Card(
                Layout.Vertical()
                    .Gap(3)
                    | Text.H2("Stock Price Data")
                    | Layout.Horizontal()
                        .Gap(2)
                        | (Layout.Vertical()
                            | Text.Small("Symbol")
                            | selectedSymbol.ToSelectInput(symbols.Take(50).ToOptions()))
                        | (Layout.Vertical()
                            | Text.Small("Start Date")
                            | startDate.ToDateInput())
                        | (Layout.Vertical()
                            | Text.Small("End Date")
                            | endDate.ToDateInput())
                        | new Button("Load Data",
                            onClick: async (Event<Button> e) =>
                            {
                                await _stockDataService?.LoadOrFetchDataAsync(startDate.Value, endDate.Value)!;
                            },
                            variant: ButtonVariant.Primary)
                )
                .Width(Size.Units(120).Max(1000))
            | BuildStockDataView(selectedSymbol.Value, startDate.Value, endDate.Value);
    }

    private object? BuildStockDataView(string symbol, DateTime startDate, DateTime endDate)
    {
        if (_stockDataService == null)
            return Text.Block("Service not initialized");

        var prices = _stockDataService.GetDailyPricesAsync(symbol, startDate, endDate).Result;
        
        if (!prices.Any())
            return new Card(Text.Block($"No price data available for {symbol} in the selected date range"));

        return Layout.Vertical()
            .Gap(4)
            | new Card(
                Layout.Vertical()
                    .Gap(2)
                    | Text.H3($"Price Chart: {symbol}")
                    | BuildPriceChart(prices)
                )
                .Width(Size.Units(120).Max(1000))
                .Height(Size.Units(40))
            | new Card(
                Layout.Vertical()
                    .Gap(2)
                    | Text.H3("Price Data Table")
                    | BuildPriceTable(prices)
                )
                .Width(Size.Units(120).Max(1000))
                .Height(Size.Units(40));
    }

    private object BuildPriceChart(List<StockPrice> prices)
    {
        var chartData = prices
            .OrderBy(p => p.Date)
            .Select(p => new
            {
                Date = p.Date,
                Close = p.Close,
                Open = p.Open,
                High = p.High,
                Low = p.Low
            })
            .ToArray();

        return chartData.ToLineChart(style: LineChartStyles.Dashboard)
            .Dimension("Date", e => e.Date)
            .Measure("Close", e => e.Sum(f => f.Close));
    }

    private object BuildPriceTable(List<StockPrice> prices)
    {
        var tableData = prices
            .OrderByDescending(p => p.Date)
            .Select(p => new
            {
                Date = p.Date.ToString("yyyy-MM-dd"),
                Open = p.Open,
                High = p.High,
                Low = p.Low,
                Close = p.Close,
                Volume = p.Volume
            })
            .AsQueryable();

        return tableData.ToDataTable()
            .Header(p => p.Date, "Date")
            .Header(p => p.Open, "Open")
            .Header(p => p.High, "High")
            .Header(p => p.Low, "Low")
            .Header(p => p.Close, "Close")
            .Header(p => p.Volume, "Volume");
    }
}

