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
            | Text.H2("Stock Price Data")
            | Layout.Horizontal()
                | new Field(selectedSymbol.ToSelectInput(symbols.Take(50).ToOptions()), "Symbol")
                | new Field(startDate.ToDateInput(), "Start Date")
                | new Field(endDate.ToDateInput(), "End Date")
                | Layout.Vertical()
                    | Text.Block("") // Spacer for button alignment
                    | new Button("Load Data",
                        onClick: async (Event<Button> e) =>
                        {
                            await _stockDataService?.LoadOrFetchDataAsync(startDate.Value, endDate.Value)!;
                        },
                        variant: ButtonVariant.Primary)
            | BuildStockDataView(selectedSymbol.Value, startDate.Value, endDate.Value);
    }

    private object? BuildStockDataView(string symbol, DateTime startDate, DateTime endDate)
    {
        if (_stockDataService == null)
            return Text.Block("Service not initialized");

        var prices = _stockDataService.GetDailyPricesAsync(symbol, startDate, endDate).Result;
        
        if (!prices.Any())
            return Text.Block($"No price data available for {symbol} in the selected date range");

        return Layout.Vertical()
            | Text.H3($"Price Chart: {symbol}")
            | BuildPriceChart(prices)
            | Text.H3("Price Data Table")
            | BuildPriceTable(prices);
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

        // var chart = chartData.ToLineChart(style: LineChartStyles.Dashboard)
        //     .Dimension("Date", e => e.Date)
        //     .Measure("Close", e => e.Sum(f => f.Close));

        var chart = new LineChart(chartData, "Date", "Close")
            .Height(Size.Units(100));
        
        // Wrap in container with explicit height
        return chart;
    }

    private object BuildPriceTable(List<StockPrice> prices)
    {
        var tableData = prices
            .OrderByDescending(p => p.Date)
            .Select(p => new
            {
                Date = p.Date.ToString("yyyy-MM-dd"),
                Open = p.Open.ToString("F2"),
                High = p.High.ToString("F2"),
                Low = p.Low.ToString("F2"),
                Close = p.Close.ToString("F2"),
                Volume = p.Volume.ToString("N0")
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

