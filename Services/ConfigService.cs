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
            // If ApiKey is already set with a prefix (env: or secret:), resolve it
            if (model.ApiKey?.StartsWith("env:") == true || model.ApiKey?.StartsWith("secret:") == true)
            {
                var key = model.ApiKey.Substring(model.ApiKey.IndexOf(':') + 1);
                
                // Try user secrets first, then environment variable
                var apiKey = _configuration?[key] ?? Environment.GetEnvironmentVariable(key);
                model.ApiKey = apiKey;
            }
            // If ApiKey is null or empty, auto-resolve based on model type
            else if (string.IsNullOrEmpty(model.ApiKey))
            {
                string? secretKey = null;
                
                // Determine which API key to use based on the model
                if (model.BaseModel.Contains("claude", StringComparison.OrdinalIgnoreCase) ||
                    model.BaseModel.Contains("anthropic", StringComparison.OrdinalIgnoreCase))
                {
                    secretKey = "ANTHROPIC_API_KEY";
                }
                else if (model.BaseModel.Contains("gpt", StringComparison.OrdinalIgnoreCase) ||
                         model.BaseModel.Contains("openai", StringComparison.OrdinalIgnoreCase))
                {
                    secretKey = "OPENAI_API_KEY";
                }
                
                if (secretKey != null)
                {
                    // Try user secrets first, then environment variable
                    var apiKey = _configuration?[secretKey] ?? Environment.GetEnvironmentVariable(secretKey);
                    model.ApiKey = apiKey;
                }
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
                    Name = "claude-sonnet-4",
                    BaseModel = "claude-sonnet-4-20250514",
                    Signature = "claude-sonnet-4",
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

