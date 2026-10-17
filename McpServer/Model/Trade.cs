namespace TradingMcp.Model;

public sealed class Trade
{
    public Trade(
        long tradeId,
        DateTime tradeTimestamp,
        string ticker,
        string side,
        decimal quantity,
        decimal price,
        decimal fees,
        string status)
    {
        TradeId = tradeId;
        TradeTimestamp = tradeTimestamp;
        Ticker = ticker;
        Side = side;
        Quantity = quantity;
        Price = price;
        Fees = fees;
        Status = status;
    }

    public long TradeId { get; }
    public DateTime TradeTimestamp { get; }
    public string Ticker { get; }
    public string Side { get; }
    public decimal Quantity { get; }
    public decimal Price { get; }
    public decimal Fees { get; }
    public string Status { get; }
}
