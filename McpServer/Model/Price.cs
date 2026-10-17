namespace TradingMcp.Model;

public sealed class Price
{
    public Price(DateTime priceDate, decimal open, decimal high, decimal low, decimal close, long volume)
    {
        PriceDate = priceDate;
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }

    public DateTime PriceDate { get; }
    public decimal Open { get; }
    public decimal High { get; }
    public decimal Low { get; }
    public decimal Close { get; }
    public long Volume { get; }
}
