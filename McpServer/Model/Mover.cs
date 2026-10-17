namespace TradingMcp.Model;

public sealed class Mover
{
    public Mover(string ticker, string name, DateTime priceDate, decimal closePrice, decimal prevClose, decimal changePct)
    {
        Ticker = ticker;
        Name = name;
        PriceDate = priceDate;
        ClosePrice = closePrice;
        PrevClose = prevClose;
        ChangePct = changePct;
    }

    public string Ticker { get; }
    public string Name { get; }
    public DateTime PriceDate { get; }
    public decimal ClosePrice { get; }
    public decimal PrevClose { get; }
    public decimal ChangePct { get; }
}
