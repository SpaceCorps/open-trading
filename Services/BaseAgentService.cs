using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenTrading.Models;

namespace OpenTrading.Services;

public class BaseAgentService : IAgentService
{
    private readonly ITradingService _tradingService;
    private readonly IPositionService _positionService;
    private readonly IStockDataService _stockDataService;
    private readonly ILogService _logService;
    private readonly ILogger<BaseAgentService> _logger;

    public BaseAgentService(
        ITradingService tradingService,
        IPositionService positionService,
        IStockDataService stockDataService,
        ILogService logService,
        ILogger<BaseAgentService> logger)
    {
        _tradingService = tradingService;
        _positionService = positionService;
        _stockDataService = stockDataService;
        _logService = logService;
        _logger = logger;
    }

    public async Task<List<TradingAction>> RunTradingDayAsync(DateTime date, string agentId, AgentConfig config)
    {
        var actions = new List<TradingAction>();
        
        // Get current position
        var position = await _positionService.GetCurrentPositionAsync(agentId, date.AddDays(-1))
            ?? new Position
            {
                Date = date,
                AgentId = agentId,
                Cash = config.InitialCash
            };

        // Get available stock prices
        var prices = await _stockDataService.GetPricesForDateAsync(date);
        
        if (!prices.Any())
        {
            _logger.LogWarning("No prices available for {Date}", date);
            return actions;
        }

        var context = new AgentContext
        {
            CurrentPosition = position,
            CurrentDate = date,
            AvailablePrices = prices,
            AgentId = agentId,
            Config = config
        };

        // Run reasoning loop
        for (int step = 0; step < config.MaxSteps; step++)
        {
            var log = new TradingLog
            {
                Date = date,
                AgentId = agentId,
                Step = step,
                Timestamp = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Agent {AgentId} step {Step}/{MaxSteps} on {Date}", 
                    agentId, step + 1, config.MaxSteps, date);
                
                var action = await DecideActionAsync(context);
                
                if (action.Action == ActionType.Hold && step > 0)
                {
                    // Agent decided to hold, stop reasoning
                    log.Message = "Agent decided to hold - ending trading day";
                    log.Reasoning = action.Reasoning;
                    await _logService.SaveLogAsync(log);
                    break;
                }

                if (action.Action == ActionType.Buy || action.Action == ActionType.Sell)
                {
                    // Validate action before executing
                    if (string.IsNullOrEmpty(action.Symbol) || action.Amount <= 0)
                    {
                        log.Message = $"Invalid action: Symbol={action.Symbol}, Amount={action.Amount}";
                        log.Reasoning = action.Reasoning;
                        await _logService.SaveLogAsync(log);
                        continue;
                    }

                    // Execute trade
                    var result = await _tradingService.ExecuteTradeAsync(action, context.CurrentPosition);
                    
                    if (result.Success && result.UpdatedPosition != null)
                    {
                        context.CurrentPosition = result.UpdatedPosition;
                        actions.Add(action);
                        
                        log.Message = $"{action.Action} {action.Amount} shares of {action.Symbol} at ${action.Price:F2} (Total: ${result.Action?.TotalCost ?? 0:F2})";
                        log.Action = action;
                        log.Reasoning = action.Reasoning;
                        
                        _logger.LogInformation("Agent {AgentId} executed {Action} {Amount} shares of {Symbol} at ${Price}",
                            agentId, action.Action, action.Amount, action.Symbol, action.Price);
                        
                        // Save updated position
                        await _positionService.SavePositionAsync(context.CurrentPosition);
                    }
                    else
                    {
                        log.Message = $"Trade failed: {result.ErrorMessage}";
                        log.Reasoning = action.Reasoning;
                        _logger.LogWarning("Trade failed for agent {AgentId}: {Error}",
                            agentId, result.ErrorMessage);
                    }
                }
                else
                {
                    log.Message = "Agent decided to hold";
                    log.Reasoning = action.Reasoning;
                }

                await _logService.SaveLogAsync(log);
                
                // Small delay to avoid rate limiting
                await Task.Delay(TimeSpan.FromSeconds(config.BaseDelay));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in trading step {Step} for agent {AgentId}", step, agentId);
                log.Message = $"Error: {ex.Message}";
                await _logService.SaveLogAsync(log);
                break;
            }
        }

        return actions;
    }

    public async Task<TradingAction> DecideActionAsync(AgentContext context)
    {
        var prompt = BuildTradingPrompt(context);
        var reasoning = await GetReasoningAsync(prompt, context);

        // Parse AI response to extract trading action
        return ParseTradingAction(reasoning, context);
    }

    public async Task<string> GetReasoningAsync(string prompt, AgentContext context)
    {
        if (string.IsNullOrEmpty(context.Config.ApiKey))
        {
            throw new InvalidOperationException($"API key not configured for agent {context.AgentId}");
        }

        // Retry logic
        var maxRetries = context.Config.MaxRetries;
        var retryDelay = TimeSpan.FromSeconds(context.Config.BaseDelay);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Check if it's OpenAI or Anthropic based on model name or base model
                var isOpenAI = context.Config.BaseModel.Contains("gpt") || 
                               context.Config.BaseModel.Contains("openai") ||
                               context.Config.BaseModel.StartsWith("o1");

                if (isOpenAI)
                {
                    return await GetOpenAIReasoningAsync(prompt, context);
                }
                else
                {
                    return await GetAnthropicReasoningAsync(prompt, context);
                }
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Attempt {Attempt} failed for agent {AgentId}, retrying...", attempt + 1, context.AgentId);
                await Task.Delay(retryDelay * (attempt + 1)); // Exponential backoff
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "All retry attempts failed for agent {AgentId}", context.AgentId);
                throw;
            }
        }

        throw new InvalidOperationException($"Failed to get reasoning after {maxRetries + 1} attempts");
    }

    private async Task<string> GetAnthropicReasoningAsync(string prompt, AgentContext context)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("x-api-key", context.Config.ApiKey);
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var model = context.Config.BaseModel.Contains("claude") 
            ? context.Config.BaseModel 
            : "claude-3-5-sonnet-20241022";

        var requestBody = new
        {
            model = model,
            max_tokens = 4096,
            system = BuildSystemPrompt(context),
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(responseBody);
        var root = responseDoc.RootElement;

        var reasoning = root.TryGetProperty("content", out var contentEl)
            && contentEl.ValueKind == JsonValueKind.Array
            && contentEl[0].TryGetProperty("text", out var textEl)
            ? textEl.GetString() ?? ""
            : "";

        return reasoning;
    }

    private async Task<string> GetOpenAIReasoningAsync(string prompt, AgentContext context)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {context.Config.ApiKey}");

        var model = context.Config.BaseModel.Contains("gpt") 
            ? context.Config.BaseModel 
            : "gpt-4o";

        var requestBody = new
        {
            model = model,
            max_tokens = 4096,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = BuildSystemPrompt(context)
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            response_format = new { type = "json_object" } // Encourage JSON response
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseDoc = JsonDocument.Parse(responseBody);
        var root = responseDoc.RootElement;

        var reasoning = root.TryGetProperty("choices", out var choicesEl)
            && choicesEl.ValueKind == JsonValueKind.Array
            && choicesEl.GetArrayLength() > 0
            && choicesEl[0].TryGetProperty("message", out var messageEl)
            && messageEl.TryGetProperty("content", out var messageContentEl)
            ? messageContentEl.GetString() ?? ""
            : "";

        return reasoning;
    }

    private string BuildSystemPrompt(AgentContext context)
    {
        return @"You are an AI trading agent analyzing stock market data to make buy/sell decisions.

Available tools:
- get_stock_price(symbol): Get current price for a stock
- buy_stock(symbol, amount): Buy shares of a stock
- sell_stock(symbol, amount): Sell shares of a stock
- get_current_positions(): Get your current holdings and cash

Respond in JSON format:
{
  ""action"": ""buy"" | ""sell"" | ""hold"",
  ""symbol"": ""AAPL"",
  ""amount"": 10,
  ""reasoning"": ""Explain your decision""
}";
    }

    private string BuildTradingPrompt(AgentContext context)
    {
        var position = context.CurrentPosition;
        var prices = context.AvailablePrices;
        
        var prompt = $@"Current Date: {context.CurrentDate:yyyy-MM-dd}
Current Cash: ${position.Cash:F2}

Current Holdings:
";
        
        foreach (var holding in position.Holdings)
        {
            var price = prices.GetValueOrDefault(holding.Key);
            var currentValue = price != null ? holding.Value * price.Close : 0;
            prompt += $"- {holding.Key}: {holding.Value} shares (Value: ${currentValue:F2})\n";
        }

        prompt += "\nAvailable Stock Prices:\n";
        var topStocks = prices.Take(20);
        foreach (var kvp in topStocks)
        {
            prompt += $"- {kvp.Key}: Open=${kvp.Value.Open:F2}, High=${kvp.Value.High:F2}, Low=${kvp.Value.Low:F2}, Close=${kvp.Value.Close:F2}, Volume={kvp.Value.Volume:N0}\n";
        }

        prompt += "\nAnalyze the market and decide on a trading action. Consider:\n";
        prompt += "- Current portfolio composition\n";
        prompt += "- Stock price trends\n";
        prompt += "- Available cash\n";
        prompt += "- Risk management\n\n";
        prompt += "Respond with a JSON object containing your decision.";

        return prompt;
    }

    private TradingAction ParseTradingAction(string reasoning, AgentContext context)
    {
        // Try to parse JSON from reasoning
        try
        {
            var jsonStart = reasoning.IndexOf('{');
            var jsonEnd = reasoning.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = reasoning.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                var action = root.GetProperty("action").GetString()?.ToLower() ?? "hold";
                var symbol = root.TryGetProperty("symbol", out var symEl) ? symEl.GetString() : "";
                var amount = root.TryGetProperty("amount", out var amtEl) ? amtEl.GetInt32() : 0;
                var reasoningText = root.TryGetProperty("reasoning", out var reasonEl) ? reasonEl.GetString() : reasoning;

                var actionType = action switch
                {
                    "buy" => ActionType.Buy,
                    "sell" => ActionType.Sell,
                    _ => ActionType.Hold
                };

                var price = context.AvailablePrices.GetValueOrDefault(symbol ?? "")?.BuyPrice ?? 0;

                if (actionType == ActionType.Buy && price == 0)
                {
                    price = context.AvailablePrices.GetValueOrDefault(symbol ?? "")?.Open ?? 0;
                }

                return new TradingAction
                {
                    Action = actionType,
                    Symbol = symbol ?? "",
                    Amount = amount,
                    Date = context.CurrentDate,
                    Price = price,
                    AgentId = context.AgentId,
                    Reasoning = reasoningText
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON from reasoning: {Reasoning}", reasoning.Substring(0, Math.Min(200, reasoning.Length)));
        }

        // Fallback: try to extract action from text
        var lowerReasoning = reasoning.ToLower();
        if (lowerReasoning.Contains("buy") && context.AvailablePrices.Any())
        {
            // Simple extraction - find first stock mentioned
            var symbol = context.AvailablePrices.Keys.FirstOrDefault(s => lowerReasoning.Contains(s.ToLower()));
            if (symbol != null)
            {
                return new TradingAction
                {
                    Action = ActionType.Buy,
                    Symbol = symbol,
                    Amount = 10, // Default amount
                    Date = context.CurrentDate,
                    Price = context.AvailablePrices[symbol].BuyPrice,
                    AgentId = context.AgentId,
                    Reasoning = reasoning
                };
            }
        }

        // Default: Hold
        return new TradingAction
        {
            Action = ActionType.Hold,
            Symbol = "",
            Amount = 0,
            Date = context.CurrentDate,
            Price = 0,
            AgentId = context.AgentId,
            Reasoning = reasoning
        };
    }
}

