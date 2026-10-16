namespace TradingDataGenerator.Models;

public record Instrument(
    int Id,
    string Ticker,
    string Name,
    string Sector,
    string Currency,
    double StartPrice,
    double Mu, // Annual drift.
    double Sigma); // Annual volatility.
