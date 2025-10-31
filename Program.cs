using OpenTrading.Apps;
using OpenTrading.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

var server = new Server();

// Register services
server.Services.AddSingleton<IHttpClientFactory, DefaultHttpClientFactory>();
server.Services.AddHttpClient();
server.Services.AddSingleton<IStockDataService, StockDataService>();
server.Services.AddSingleton<ITradingService, TradingService>();
server.Services.AddSingleton<IPositionService, PositionService>();
server.Services.AddSingleton<IAgentService, BaseAgentService>();
server.Services.AddSingleton<IConfigService, ConfigService>();
server.Services.AddSingleton<ILogService, LogService>();

#if DEBUG
server.UseHotReload();
#endif

server.AddAppsFromAssembly();
server.AddConnectionsFromAssembly();

var chromeSettings = new ChromeSettings().DefaultApp<TradingArenaApp>().UseTabs(preventDuplicates: true);
server.UseChrome(chromeSettings);
await server.RunAsync();

// Simple implementation for IHttpClientFactory
internal class DefaultHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new HttpClient();
}
