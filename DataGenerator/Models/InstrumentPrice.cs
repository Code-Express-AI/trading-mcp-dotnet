namespace TradingDataGenerator.Models;

internal sealed record InstrumentPrice(
    int InstrumentId,
    DateTime PriceDate,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);
