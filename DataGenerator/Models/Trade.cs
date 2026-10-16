namespace TradingDataGenerator.Models;

public record Trade(
    int AccountId,
    int InstrumentId,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Fees,
    DateTime Timestamp,
    string Status);
