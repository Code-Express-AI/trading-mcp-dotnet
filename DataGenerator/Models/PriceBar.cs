namespace TradingDataGenerator.Models;

internal sealed record PriceBar(
    int InstrumentId,
    DateTime PriceDate,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume);
