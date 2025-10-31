using OpenTrading.Models;

namespace OpenTrading.Services;

public class TradingService : ITradingService
{
    public decimal CalculateCost(decimal price, int amount)
    {
        return price * amount;
    }

    public bool ValidateTrade(TradingAction action, Position position)
    {
        if (action.Action == ActionType.Buy)
        {
            var cost = CalculateCost(action.Price, action.Amount);
            return position.Cash >= cost && action.Amount > 0;
        }
        else if (action.Action == ActionType.Sell)
        {
            var currentHoldings = position.Holdings.GetValueOrDefault(action.Symbol, 0);
            return currentHoldings >= action.Amount && action.Amount > 0;
        }

        return true; // Hold is always valid
    }

    public Task<TradingResult> ExecuteTradeAsync(TradingAction action, Position currentPosition)
    {
        // Validate action
        if (action.Amount <= 0)
        {
            return Task.FromResult(new TradingResult
            {
                Success = false,
                ErrorMessage = "Trade amount must be greater than zero"
            });
        }

        if (string.IsNullOrEmpty(action.Symbol))
        {
            return Task.FromResult(new TradingResult
            {
                Success = false,
                ErrorMessage = "Stock symbol is required"
            });
        }

        if (!ValidateTrade(action, currentPosition))
        {
            return Task.FromResult(new TradingResult
            {
                Success = false,
                ErrorMessage = action.Action == ActionType.Buy 
                    ? $"Insufficient cash for purchase. Required: ${CalculateCost(action.Price, action.Amount):F2}, Available: ${currentPosition.Cash:F2}"
                    : $"Insufficient holdings for sale. Required: {action.Amount}, Available: {currentPosition.Holdings.GetValueOrDefault(action.Symbol, 0)}"
            });
        }

        var updatedPosition = new Position
        {
            Date = action.Date,
            AgentId = currentPosition.AgentId,
            Holdings = new Dictionary<string, int>(currentPosition.Holdings),
            Cash = currentPosition.Cash
        };

        if (action.Action == ActionType.Buy)
        {
            var cost = CalculateCost(action.Price, action.Amount);
            updatedPosition.Cash -= cost;
            updatedPosition.Holdings[action.Symbol] = 
                updatedPosition.Holdings.GetValueOrDefault(action.Symbol, 0) + action.Amount;
            action.TotalCost = cost;
        }
        else if (action.Action == ActionType.Sell)
        {
            var proceeds = CalculateCost(action.Price, action.Amount);
            updatedPosition.Cash += proceeds;
            var currentHoldings = updatedPosition.Holdings.GetValueOrDefault(action.Symbol, 0);
            updatedPosition.Holdings[action.Symbol] = currentHoldings - action.Amount;
            
            if (updatedPosition.Holdings[action.Symbol] <= 0)
            {
                updatedPosition.Holdings.Remove(action.Symbol);
            }
            
            action.TotalCost = proceeds;
        }

        updatedPosition.LastAction = action;

        return Task.FromResult(new TradingResult
        {
            Success = true,
            UpdatedPosition = updatedPosition,
            Action = action
        });
    }
}

