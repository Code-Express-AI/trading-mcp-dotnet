namespace TradingMcp.Model;

public sealed class Instrument
{
    public Instrument(string ticker, string name, string sector, string currency)
    {
        Ticker = ticker;
        Name = name;
        Sector = sector;
        Currency = currency;
    }

    public string Ticker { get; }
    public string Name { get; }
    public string Sector { get; }
    public string Currency { get; }
}
