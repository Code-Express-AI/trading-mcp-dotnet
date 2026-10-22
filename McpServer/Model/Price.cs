namespace TradingMcp.Model;

public sealed record Price(DateTime PriceDate, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
