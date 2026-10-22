namespace TradingMcp.Model;

public sealed record Position(
    string Ticker,
    string InstrumentName,
    string Sector,
    string Currency,
    decimal NetQuantity,
    decimal? AvgBuyPrice,
    DateTime PriceAsOf,
    decimal LastPrice,
    decimal MarketValue,
    decimal UnrealisedPnL,
    decimal? UnrealisedPnLPct);
