namespace TradingDataGenerator.Models;

internal sealed record Instrument(
    int Id,
    string Ticker,
    string Name,
    string Sector,
    string Currency,
    double StartPrice,
    double AnnualDrift,
    double AnnualVolatility);
