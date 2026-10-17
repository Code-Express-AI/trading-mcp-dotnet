namespace TradingMcp.Model;

public sealed class Position
{
    public Position(
        string ticker,
        string instrumentName,
        string sector,
        string currency,
        decimal netQuantity,
        decimal? avgBuyPrice,
        DateTime priceAsOf,
        decimal lastPrice,
        decimal marketValue,
        decimal unrealisedPnL,
        decimal? unrealisedPnLPct)
    {
        Ticker = ticker;
        InstrumentName = instrumentName;
        Sector = sector;
        Currency = currency;
        NetQuantity = netQuantity;
        AvgBuyPrice = avgBuyPrice;
        PriceAsOf = priceAsOf;
        LastPrice = lastPrice;
        MarketValue = marketValue;
        UnrealisedPnL = unrealisedPnL;
        UnrealisedPnLPct = unrealisedPnLPct;
    }

    public string Ticker { get; }
    public string InstrumentName { get; }
    public string Sector { get; }
    public string Currency { get; }
    public decimal NetQuantity { get; }
    public decimal? AvgBuyPrice { get; }
    public DateTime PriceAsOf { get; }
    public decimal LastPrice { get; }
    public decimal MarketValue { get; }
    public decimal UnrealisedPnL { get; }
    public decimal? UnrealisedPnLPct { get; }
}
