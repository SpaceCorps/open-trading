using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Microsoft.Extensions.Configuration;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class ConfigService : IConfigService
{
    private readonly string _configPath = "./Configs/default.json";
    private readonly IConfiguration? _configuration;

    public ConfigService(IConfiguration? configuration = null)
    {
        _configuration = configuration;
        Directory.CreateDirectory("./Configs");
    }

    public async Task<AppConfig> LoadConfigAsync(string? configPath = null)
    {
        var path = configPath ?? _configPath;
        
        if (!File.Exists(path))
        {
            var defaultConfig = GetDefaultConfig();
            await SaveConfigAsync(defaultConfig, path);
            return defaultConfig;
        }

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (config == null)
        {
            return GetDefaultConfig();
        }

        // Resolve API keys from user secrets or environment variables
        foreach (var model in config.Models)
        {
            if (model.ApiKey?.StartsWith("env:") == true || model.ApiKey?.StartsWith("secret:") == true)
            {
                var key = model.ApiKey.Substring(model.ApiKey.IndexOf(':') + 1);
                
                // Try user secrets first, then environment variable
                var apiKey = _configuration?[key] ?? Environment.GetEnvironmentVariable(key);
                model.ApiKey = apiKey;
            }
        }

        return config;
    }

    public async Task SaveConfigAsync(AppConfig config, string? configPath = null)
    {
        var path = configPath ?? _configPath;
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await File.WriteAllTextAsync(path, json);
    }

    public AppConfig GetDefaultConfig()
    {
        return new AppConfig
        {
            AgentType = "BaseAgent",
            DateRange = new DateRange
            {
                InitDate = DateTime.Today.AddMonths(-1),
                EndDate = DateTime.Today
            },
            Models = new List<ModelConfig>
            {
                new ModelConfig
                {
                    Name = "claude-3-7-sonnet",
                    BaseModel = "claude-3-7-sonnet-20241022",
                    Signature = "claude-3-7-sonnet",
                    Enabled = true,
                    ApiKey = "secret:ANTHROPIC_API_KEY"
                },
                new ModelConfig
                {
                    Name = "gpt-4o",
                    BaseModel = "gpt-4o",
                    Signature = "gpt-4o",
                    Enabled = false,
                    ApiKey = "secret:OPENAI_API_KEY"
                }
            },
            AgentConfig = new AgentConfigSection
            {
                MaxSteps = 30,
                MaxRetries = 3,
                BaseDelay = 1.0,
                InitialCash = 10000.0m
            },
            LogConfig = new LogConfig
            {
                LogPath = "./Data/Agents"
            }
        };
    }
}

