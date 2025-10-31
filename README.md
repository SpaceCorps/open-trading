# Open Trading - AI Trading Arena

A C# port of the [AI-Trader repository](https://github.com/HKUDS/AI-Trader) using the [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework). This application allows multiple AI agents to compete in a simulated stock trading environment using NASDAQ 100 stocks.

## Features

- 🤖 **Multiple AI Agents**: Configure and run multiple AI models (Claude, GPT-4, etc.) simultaneously
- 📈 **Real-time Trading**: Watch agents make trading decisions in real-time
- 📊 **Performance Analytics**: Compare agent performance with charts and metrics
- 💾 **Data Persistence**: All positions and trading logs are saved in JSONL format
- 🎨 **Modern UI**: Built with Ivy Framework for a reactive, modern web interface
- ⚙️ **Configurable**: Easy configuration via JSON files

## Architecture

### Core Components

- **Models**: Data structures for positions, trading actions, stock prices, and logs
- **Services**: Business logic for trading, data fetching, position management, and AI agents
- **Apps**: Three main Ivy apps:
  - `TradingArenaApp`: Main trading dashboard
  - `AgentPerformanceApp`: Agent comparison and analytics
  - `StockDataApp`: Stock price data viewer

### Key Services

- `IStockDataService`: Fetches and caches stock price data
- `ITradingService`: Executes buy/sell orders
- `IPositionService`: Tracks agent positions over time
- `IAgentService`: AI agent reasoning and decision-making
- `IConfigService`: Configuration management
- `ILogService`: Trading log persistence

## Setup

### Prerequisites

- .NET 9.0 SDK
- Anthropic API key (for Claude models) or OpenAI API key
- Ivy Framework (included via NuGet)

### Configuration

1. **Set up user secrets** (recommended for API keys):
   ```bash
   # Set Alpha Vantage API key
   dotnet user-secrets set ALPHA_VANTAGE_API_KEY your-api-key-here
   
   # Set Anthropic API key (for AI agents)
   dotnet user-secrets set ANTHROPIC_API_KEY your-api-key-here
   ```

   Alternatively, you can set environment variables:
   ```bash
   export ANTHROPIC_API_KEY=your-api-key-here
   export ALPHA_VANTAGE_API_KEY=your-api-key-here
   ```

2. Edit `Configs/default.json` to configure:
   - Date range for backtesting
   - AI models to use
   - Agent parameters (max steps, initial cash, etc.)

### Running

```bash
dotnet watch
```

The application will start and open in your browser at `http://localhost:5000` (or the configured port).

## Usage

### Trading Arena

1. Select a date and agent from the dropdown
2. Click "Run Simulation" to start a trading session
3. Watch the agent make trading decisions
4. View current holdings and portfolio value

### Agent Performance

1. Select a date range
2. Compare performance metrics across all enabled agents
3. View return percentages and trade counts

### Stock Data

1. Select a stock symbol
2. Choose a date range
3. View price charts and historical data

## API Keys

This application requires API keys for:

- **Alpha Vantage** (for stock price data): Get a free API key at [Alpha Vantage](https://www.alphavantage.co/support/#api-key)
- **Anthropic** (for AI agents): Get an API key at [Anthropic](https://console.anthropic.com/)

### Setting Up User Secrets

User secrets are stored securely and not committed to git:

```bash
# Set Alpha Vantage API key
dotnet user-secrets set ALPHA_VANTAGE_API_KEY your-key-here

# Set Anthropic API key  
dotnet user-secrets set ANTHROPIC_API_KEY your-key-here

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove ALPHA_VANTAGE_API_KEY
```

The application will automatically fall back to mock data if API keys are not configured, but real data requires the Alpha Vantage API key.

## Data Storage

All data is stored in the `Data` directory:

```
Data/
├── Agents/
│   └── {agent-id}/
│       ├── positions/
│       │   └── position.jsonl
│       └── logs/
│           └── {date}/
│               └── log.jsonl
└── Prices/
    └── {symbol}.jsonl
```

## Configuration Format

```json
{
  "agentType": "BaseAgent",
  "dateRange": {
    "initDate": "2025-01-01T00:00:00",
    "endDate": "2025-01-31T00:00:00"
  },
  "models": [
    {
      "name": "claude-3-7-sonnet",
      "baseModel": "claude-3-7-sonnet-20241022",
      "signature": "claude-3-7-sonnet",
      "enabled": true
    }
  ],
  "agentConfig": {
    "maxSteps": 30,
    "maxRetries": 3,
    "baseDelay": 1.0,
    "initialCash": 10000.0
  }
}
```

## Development

### Project Structure

```
OpenTrading/
├── Apps/              # Ivy UI applications
├── Services/          # Business logic services
├── Models/            # Data models
├── Data/              # Data storage
├── Configs/           # Configuration files
└── Program.cs         # Entry point
```

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Differences from Python Version

- **No LangChain/MCP**: Direct API integration with Anthropic/OpenAI
- **Ivy UI**: Modern reactive web UI instead of static HTML
- **Service-based**: Clean separation of concerns with dependency injection
- **Type-safe**: Full C# type safety throughout

## License

MIT License

## Acknowledgments

- Original [AI-Trader](https://github.com/HKUDS/AI-Trader) project
- [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) for the amazing web framework
