using OpenTrading.Models;

namespace OpenTrading.Services;

public interface ITradingService
{
    Task<TradingResult> ExecuteTradeAsync(TradingAction action, Position currentPosition);
    decimal CalculateCost(decimal price, int amount);
    bool ValidateTrade(TradingAction action, Position position);
}

