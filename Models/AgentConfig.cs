namespace OpenTrading.Models;

public class AgentConfig
{
    public string Name { get; set; } = string.Empty;
    public string BaseModel { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int MaxSteps { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
    public double BaseDelay { get; set; } = 1.0;
    public decimal InitialCash { get; set; } = 10000.0m;
    public string? ApiKey { get; set; }
}

