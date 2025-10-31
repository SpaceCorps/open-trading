# AI-Trader Port to C# with Ivy Framework - Detailed Plan

## Executive Summary

This document outlines a comprehensive plan to port the [AI-Trader repository](https://github.com/HKUDS/AI-Trader) from Python to C# using the Ivy Framework. The original project is an AI-powered trading system where multiple AI models compete in a simulated trading environment using NASDAQ 100 stocks.

## Architecture Overview

### Original Python Architecture

- **Agent System**: BaseAgent class with LangChain integration
- **MCP Services**: Model Context Protocol tools for trading operations
- **Data Layer**: JSONL files for positions and logs
- **Stock Data**: Alpha Vantage API integration
- **AI Models**: OpenAI, Anthropic Claude integration via LangChain
- **Web Interface**: Static HTML/JavaScript visualization

### Target C#/Ivy Architecture

- **Ivy Apps**: Multiple ViewBase apps for different views
- **Services**: C# services for agent logic, data fetching, trading operations
- **Data Layer**: File-based storage (JSON) or database option
- **Stock Data**: HTTP client for API calls
- **AI Models**: Direct API integration (OpenAI SDK, Anthropic SDK for .NET)
- **Real-time UI**: Ivy's reactive components with charts and tables

## Phase 1: Project Structure & Foundation

### 1.1 Directory Structure

```
OpenTrading/
├── Apps/
│   ├── TradingArenaApp.cs          # Main trading dashboard
│   ├── AgentPerformanceApp.cs      # Agent comparison view
│   └── StockDataApp.cs              # Stock data viewer
├── Services/
│   ├── IAgentService.cs             # Agent interface
│   ├── BaseAgentService.cs          # Base agent implementation
│   ├── TradingService.cs            # Trading logic
│   ├── StockDataService.cs          # Stock data fetching
│   ├── PositionService.cs           # Position management
│   └── ConfigService.cs             # Configuration management
├── Models/
│   ├── AgentConfig.cs               # Agent configuration
│   ├── Position.cs                  # Position data
│   ├── TradingAction.cs             # Buy/Sell actions
│   ├── StockPrice.cs                 # Stock price data
│   └── TradingLog.cs                 # Trading log entry
├── Data/
│   ├── Agents/                      # Agent-specific folders
│   └── Configs/                     # Configuration files
├── Configs/
│   ├── default.json                 # Default configuration
│   └── custom.json                  # Custom configurations
└── Tools/                           # MCP tools (if needed)
    └── TradingTools.cs
```

### 1.2 NuGet Packages Required

```xml
<ItemGroup>
  <!-- Ivy Framework (already included) -->
  <PackageReference Include="Ivy" Version="1.*" />
  
  <!-- AI/ML SDKs -->
  <PackageReference Include="OpenAI" Version="1.0.0" />
  <PackageReference Include="Anthropic.SDK" Version="1.0.0" />
  
  <!-- HTTP & JSON -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  <PackageReference Include="System.Text.Json" Version="8.0.0" />
  
  <!-- Logging -->
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
  <PackageReference Include="Serilog.Extensions.Logging" Version="8.0.0" />
  
  <!-- Configuration -->
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
</ItemGroup>
```

## Phase 2: Core Data Models

### 2.1 Models to Implement

#### AgentConfig

- Properties: Name, BaseModel, Signature, Enabled, MaxSteps, MaxRetries, InitialCash
- Purpose: Configuration for each AI agent

#### TradingAction

- Properties: Action (Buy/Sell), Symbol, Amount, Date, Price
- Purpose: Represents a trading decision

#### Position

- Properties: Date, AgentId, Positions (Dictionary<string, decimal>), Cash
- Purpose: Current holdings and cash balance

#### StockPrice

- Properties: Symbol, Date, Open, High, Low, Close, Volume
- Purpose: Stock price data from API

#### TradingLog

- Properties: Date, AgentId, Step, Message, Reasoning
- Purpose: Detailed logging of agent decisions

## Phase 3: Services Implementation

### 3.1 StockDataService

**Responsibilities:**

- Fetch daily stock prices from Alpha Vantage or alternative API
- Cache data locally to avoid API limits
- Parse and normalize price data
- Provide data for date ranges

**Key Methods:**

- `Task<List<StockPrice>> GetDailyPricesAsync(string symbol, DateTime startDate, DateTime endDate)`
- `Task<StockPrice> GetPriceAsync(string symbol, DateTime date)`
- `Task<List<string>> GetSymbolsAsync()` // NASDAQ 100 list

**Implementation Notes:**

- Use HttpClient with retry logic
- Cache responses in memory or file
- Handle API rate limiting

### 3.2 TradingService

**Responsibilities:**

- Execute buy/sell orders
- Calculate transaction costs
- Validate trades (cash availability, position limits)
- Update positions after trades

**Key Methods:**

- `Task<TradingResult> ExecuteTradeAsync(TradingAction action, Position currentPosition)`
- `decimal CalculateCost(decimal price, int amount)`
- `bool ValidateTrade(TradingAction action, Position position)`

### 3.3 PositionService

**Responsibilities:**

- Track agent positions over time
- Load/save position history (JSONL format)
- Calculate portfolio values
- Generate position reports

**Key Methods:**

- `Task<Position> GetCurrentPositionAsync(string agentId, DateTime date)`
- `Task SavePositionAsync(Position position)`
- `Task<Dictionary<string, decimal>> CalculatePortfolioValueAsync(string agentId, DateTime date, List<StockPrice> prices)`

### 3.4 BaseAgentService

**Responsibilities:**

- Core agent logic and reasoning loop
- Integration with AI models (OpenAI, Anthropic)
- Tool calling for trading operations
- Step-by-step reasoning tracking

**Key Methods:**

- `Task<TradingAction> DecideActionAsync(AgentContext context)`
- `Task<string> GetReasoningAsync(string prompt, AgentContext context)`
- `Task<List<TradingAction>> RunTradingDayAsync(DateTime date, string agentId)`

**Implementation Strategy:**
Since the original uses LangChain with MCP (Model Context Protocol), we have options:

1. **Direct API Integration**: Use OpenAI and Anthropic SDKs directly
2. **Custom Tool System**: Implement a simple tool-calling mechanism
3. **MCP Client**: If a .NET MCP client exists, use it

**Recommended Approach**: Direct API integration with structured prompts and tool calling.

### 3.5 ConfigService

**Responsibilities:**

- Load configuration from JSON files
- Validate configuration
- Provide default configurations
- Manage date ranges and agent settings

**Key Methods:**

- `Task<AppConfig> LoadConfigAsync(string configPath)`
- `Task SaveConfigAsync(AppConfig config, string configPath)`

## Phase 4: AI Agent Implementation

### 4.1 Prompt Engineering

**Trading Prompt Template:**

- Current positions
- Available cash
- Stock prices for the day
- Market context
- Trading rules

### 4.2 Tool Calling Structure

Instead of MCP tools, implement structured function calling:

```csharp
public class TradingTool
{
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    
    // Available tools:
    // - get_stock_price(symbol, date)
    // - get_current_positions()
    // - buy_stock(symbol, amount)
    // - sell_stock(symbol, amount)
    // - get_portfolio_value()
}
```

### 4.3 Agent Reasoning Loop

1. Load current position
2. Get available stock data
3. Build context prompt
4. Call AI model with tools
5. Parse tool calls
6. Execute trading actions
7. Log reasoning
8. Update positions
9. Repeat for max steps

## Phase 5: Ivy UI Implementation

### 5.1 TradingArenaApp (Main Dashboard)

**Components:**

- **Header**: Date selector, configuration selector
- **Agent Cards**: Show each agent's performance metrics
  - Total value
  - Daily change
  - Best performer badge
- **Portfolio Chart**: Line chart showing value over time
- **Position Table**: DataTable showing current holdings
- **Trading Log**: Scrollable log of recent decisions

**Ivy Widgets Used:**

- `DataTable` for positions
- `LineChart` for portfolio value over time
- `BarChart` for comparing agents
- `Card` for agent summaries
- State management with `UseState`

### 5.2 AgentPerformanceApp

**Features:**

- Compare multiple agents side-by-side
- Performance metrics table
- Win/loss statistics
- Best/worst trades

### 5.3 StockDataApp

**Features:**

- Browse NASDAQ 100 stocks
- View price charts
- Filter and search
- Export data

## Phase 6: Data Management

### 6.1 Data Storage Strategy

**Option A: JSONL Files (Original Approach)**

- Store positions and logs in JSONL format
- One file per agent per date
- Easy to parse and append

**Option B: SQLite Database**

- More structured queries
- Better performance for large datasets
- Use Entity Framework Core if needed

**Recommended**: Start with JSONL files, migrate to database if needed.

### 6.2 File Structure

```
Data/
├── Prices/
│   └── merged.jsonl                  # All stock prices
├── Agents/
│   ├── claude-3-7-sonnet/
│   │   ├── positions/
│   │   │   └── position.jsonl
│   │   └── logs/
│   │       └── 2025-01-20.jsonl
│   └── gpt-4o/
│       └── ...
└── Configs/
    └── config.json
```

## Phase 7: Configuration System

### 7.1 Configuration File Format

```json
{
  "agentType": "BaseAgent",
  "dateRange": {
    "initDate": "2025-01-01",
    "endDate": "2025-01-31"
  },
  "models": [
    {
      "name": "claude-3-7-sonnet",
      "baseModel": "anthropic/claude-3-7-sonnet",
      "signature": "claude-3-7-sonnet",
      "enabled": true,
      "apiKey": "env:ANTHROPIC_API_KEY"
    }
  ],
  "agentConfig": {
    "maxSteps": 30,
    "maxRetries": 3,
    "baseDelay": 1.0,
    "initialCash": 10000.0
  },
  "logConfig": {
    "logPath": "./Data/Agents"
  }
}
```

### 7.2 Environment Variables

- `OPENAI_API_KEY`
- `ANTHROPIC_API_KEY`
- `ALPHA_VANTAGE_API_KEY` (if using Alpha Vantage)
- `TRADING_INITIAL_CASH` (optional override)

## Phase 8: Real-time Features

### 8.1 Live Updates

- Use Ivy's reactive state management
- Auto-refresh data at intervals
- Real-time position updates

### 8.2 Progress Tracking

- Show simulation progress
- ETA for completion
- Current date being processed

## Phase 9: Advanced Features

### 9.1 Multi-Agent Competition

- Run multiple agents simultaneously
- Compare performance in real-time
- Leaderboard view

### 9.2 Backtesting Engine

- Replay historical dates
- Step through trading decisions
- Analyze strategy effectiveness

### 9.3 Risk Management

- Position limits
- Stop-loss mechanisms
- Portfolio rebalancing rules

## Phase 10: Testing & Validation

### 10.1 Unit Tests

- Service layer tests
- Model validation tests
- Trading logic tests

### 10.2 Integration Tests

- End-to-end trading simulation
- API integration tests
- Data persistence tests

### 10.3 Performance Tests

- Large dataset handling
- Concurrent agent execution
- UI responsiveness

## Implementation Priority

### Priority 1 (MVP)

1. ✅ Project structure setup
2. ✅ Basic models (Position, TradingAction, StockPrice)
3. ✅ StockDataService (mock data initially)
4. ✅ PositionService (save/load JSONL)
5. ✅ Basic BaseAgentService (simple AI integration)
6. ✅ TradingArenaApp (basic UI)
7. ✅ Single agent trading simulation

### Priority 2 (Core Features)

1. Real stock data API integration
2. Multi-agent support
3. Configuration system
4. Enhanced UI with charts
5. Logging system
6. Performance metrics

### Priority 3 (Enhanced Features)

1. Advanced visualizations
2. Comparison views
3. Export functionality
4. Risk management
5. Strategy customization

## Technical Challenges & Solutions

### Challenge 1: MCP Protocol

**Problem**: Original uses MCP (Model Context Protocol) which is Python-focused.
**Solution**:

- Use direct OpenAI/Anthropic function calling
- Implement custom tool system
- Or find/create .NET MCP client

### Challenge 2: LangChain Replacement

**Problem**: LangChain provides abstractions for AI workflows.
**Solution**:

- Use direct SDK calls
- Create service abstractions
- Implement prompt templates

### Challenge 3: Async Trading Simulation

**Problem**: Need to process multiple agents efficiently.
**Solution**:

- Use async/await for I/O
- Parallel agent execution with Task.WhenAll
- Rate limiting for API calls

### Challenge 4: Large Data Visualization

**Problem**: Rendering many data points in charts.
**Solution**:

- Use Ivy's DataTable with pagination
- Chart aggregation (daily/weekly summaries)
- Lazy loading for historical data

## Dependencies Mapping

| Python Package | C# Alternative |
|----------------|----------------|
| langchain | Direct OpenAI/Anthropic SDKs |
| langchain-openai | OpenAI NuGet package |
| langchain-anthropic | Anthropic.SDK NuGet |
| langchain-mcp-adapters | Custom tool system |
| fastmcp | Custom MCP client (if needed) |
| python-dotenv | Microsoft.Extensions.Configuration |
| requests | HttpClient (built-in) |
| pandas | LINQ + custom extensions |
| numpy | System.Numerics (if needed) |

## Success Criteria

1. ✅ Can configure and run multiple AI agents
2. ✅ Agents can fetch stock data and make trading decisions
3. ✅ Positions are tracked and persisted correctly
4. ✅ UI shows real-time performance metrics
5. ✅ Can run backtest simulations
6. ✅ Performance matches or exceeds Python version
7. ✅ Code is maintainable and extensible

## Next Steps

After approval of this plan:

1. Set up detailed project structure
2. Implement models and services
3. Create basic UI
4. Integrate AI models
5. Test with single agent
6. Scale to multiple agents
7. Add visualizations
8. Polish and optimize

## Notes

- Consider using Entity Framework Core for future database integration
- May need to create custom JSONL reader/writer for .NET
- Ivy's reactive UI will provide better UX than original static HTML
- Consider using BackgroundService for long-running simulations
- Implement proper error handling and retry logic for API calls
