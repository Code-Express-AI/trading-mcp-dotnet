namespace TradingMcp.Model;

public sealed class SectorSummary
{
    public SectorSummary(string sector, string currency, int positions, decimal marketValue, decimal unrealisedPnL)
    {
        Sector = sector;
        Currency = currency;
        Positions = positions;
        MarketValue = marketValue;
        UnrealisedPnL = unrealisedPnL;
    }

    public string Sector { get; }
    public string Currency { get; }
    public int Positions { get; }
    public decimal MarketValue { get; }
    public decimal UnrealisedPnL { get; }
}
