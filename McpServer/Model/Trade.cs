namespace TradingMcp.Model;

public sealed record Trade(
    DateTime TradeTimestamp,
    string Ticker,
    string Side,
    decimal Quantity,
    decimal Price,
    decimal Fees,
    string Status);
