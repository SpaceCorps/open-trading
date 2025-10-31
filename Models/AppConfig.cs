using System.Text.Json.Serialization;

namespace OpenTrading.Models;

public class DateRange
{
    public DateTime InitDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ModelConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseModel { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    
    [JsonIgnore]
    public string? ApiKey { get; set; }
}

public class AgentConfigSection
{
    public int MaxSteps { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public double BaseDelay { get; set; } = 1.0;
    public decimal InitialCash { get; set; } = 10000.0m;
}

public class LogConfig
{
    public string LogPath { get; set; } = "./Data/Agents";
}

public class AppConfig
{
    public string AgentType { get; set; } = "BaseAgent";
    public DateRange DateRange { get; set; } = new();
    public List<ModelConfig> Models { get; set; } = new();
    public AgentConfigSection AgentConfig { get; set; } = new();
    public LogConfig LogConfig { get; set; } = new();
}

