using OpenTrading.Models;

namespace OpenTrading.Services;

public interface IConfigService
{
    Task<AppConfig> LoadConfigAsync(string? configPath = null);
    Task SaveConfigAsync(AppConfig config, string? configPath = null);
    AppConfig GetDefaultConfig();
}

