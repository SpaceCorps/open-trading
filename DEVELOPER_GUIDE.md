# Developer Guide - Open Trading

## Architecture Overview

Open Trading is a C# port of the AI-Trader Python project, built using the Ivy Framework. It enables multiple AI agents to compete in simulated stock trading using NASDAQ 100 stocks.

## Project Structure

```
OpenTrading/
├── Apps/                    # Ivy UI Applications
│   ├── TradingArenaApp.cs   # Main trading dashboard
│   ├── AgentPerformanceApp.cs # Agent comparison view
│   └── StockDataApp.cs      # Stock price data viewer
├── Services/                # Business Logic Services
│   ├── BaseAgentService.cs  # AI agent implementation
│   ├── StockDataService.cs  # Stock price data fetching
│   ├── TradingService.cs    # Trade execution
│   ├── PositionService.cs   # Position tracking
│   ├── ConfigService.cs     # Configuration management
│   ├── LogService.cs        # Trading log persistence
│   └── SimulationService.cs # Multi-agent simulation runner
├── Models/                   # Data Models
│   ├── AgentConfig.cs       # Agent configuration
│   ├── TradingAction.cs     # Buy/Sell actions
│   ├── Position.cs          # Current holdings
│   ├── StockPrice.cs        # Stock price data
│   ├── TradingLog.cs        # Trading log entries
│   └── AppConfig.cs         # Application configuration
├── Data/                     # Data Storage
│   ├── Agents/              # Agent-specific data
│   └── Prices/               # Stock price cache
└── Configs/                  # Configuration Files
    └── default.json          # Default configuration
```

## Key Components

### Services

#### BaseAgentService
The core AI agent service that:
- Integrates with OpenAI and Anthropic APIs
- Implements retry logic with exponential backoff
- Parses AI responses to extract trading decisions
- Manages the agent reasoning loop

#### StockDataService
Handles stock price data:
- Fetches from Alpha Vantage API
- Caches data locally in JSONL format
- Falls back to mock data if API unavailable
- Parallel fetching for better performance

#### SimulationService
Orchestrates multi-agent simulations:
- Runs multiple agents simultaneously
- Supports date range simulations
- Progress tracking (future enhancement)

### Models

#### AgentConfig
Configuration for each AI agent:
- API keys (from user secrets)
- Model selection (OpenAI or Anthropic)
- Trading parameters (max steps, retries, etc.)

#### Position
Represents agent holdings:
- Cash balance
- Stock holdings (symbol -> shares)
- Portfolio value calculation

#### TradingAction
Represents a trading decision:
- Action type (Buy/Sell/Hold)
- Symbol and amount
- Price and reasoning

## Configuration

### User Secrets Setup

```bash
# Set Alpha Vantage API key
dotnet user-secrets set ALPHA_VANTAGE_API_KEY your-key

# Set Anthropic API key
dotnet user-secrets set ANTHROPIC_API_KEY your-key

# Set OpenAI API key
dotnet user-secrets set OPENAI_API_KEY your-key
```

### Configuration File

Edit `Configs/default.json` to:
- Configure date ranges
- Enable/disable agents
- Set trading parameters
- Customize agent settings

## API Integration

### Alpha Vantage
- Free tier: 5 API calls/minute, 500 calls/day
- Endpoint: `TIME_SERIES_DAILY`
- Auto-falls back to mock data if rate limited

### Anthropic (Claude)
- Direct HTTP integration
- Models: claude-sonnet-4, claude-sonnet-4-20250514, etc.
- System prompts for trading instructions

### OpenAI (GPT)
- Direct HTTP integration
- Models: gpt-4o, gpt-4, etc.
- JSON response format for structured output

## Data Storage

### JSONL Format
All data is stored in JSONL (JSON Lines) format:
- One JSON object per line
- Easy to append and parse
- Compatible with original Python format

### File Structure
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

## Development Workflow

### Running Locally
```bash
dotnet watch
```

### Building
```bash
dotnet build
```

### Testing
```bash
dotnet test
```

## Adding New Features

### Adding a New Agent
1. Update `Configs/default.json` with new model
2. Ensure API key is set in user secrets
3. The BaseAgentService will auto-detect OpenAI vs Anthropic

### Adding a New UI App
1. Create new class in `Apps/` inheriting from `ViewBase`
2. Add `[App]` attribute with icon and title
3. Register services via `UseService<T>()`

### Adding a New Service
1. Create interface in `Services/I{ServiceName}.cs`
2. Implement in `Services/{ServiceName}.cs`
3. Register in `Program.cs`

## Error Handling

- All services use try-catch for error handling
- Retry logic with exponential backoff in BaseAgentService
- Logging at appropriate levels (Debug, Info, Warning, Error)
- Graceful fallbacks (mock data, cached data)

## Performance Considerations

- Parallel fetching of stock prices
- Caching of API responses
- Lazy loading of data in UI
- DataTable for large datasets

## Security

- API keys stored in user secrets (never committed)
- Configuration supports `secret:` prefix for user secrets
- Falls back to environment variables if needed

## Troubleshooting

### No Stock Data
- Check Alpha Vantage API key is set
- Verify API rate limits
- Check logs for API errors
- Fallback to mock data should work

### Agent Not Trading
- Verify API key is configured
- Check agent is enabled in config
- Review logs for errors
- Check available stock prices

### Build Errors
- Ensure all NuGet packages restored
- Check Ivy version compatibility
- Verify .NET 9.0 SDK installed

## Future Enhancements

- Real-time UI updates during simulation
- Progress bars for long operations
- Database integration (SQLite/PostgreSQL)
- Advanced charting options
- Risk management features
- Unit tests

