namespace TradingDataGenerator.Models;

internal sealed record Trade(
    int AccountId,
    int InstrumentId,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Fees,
    DateTime Timestamp,
    string Status);
