namespace TradingMcp.Model;

public sealed class DatasetInfo
{
    public DatasetInfo(DateTime firstPriceDate, DateTime lastPriceDate, int instruments, int accounts, int trades)
    {
        FirstPriceDate = firstPriceDate;
        LastPriceDate = lastPriceDate;
        Instruments = instruments;
        Accounts = accounts;
        Trades = trades;
    }

    public DateTime FirstPriceDate { get; }
    public DateTime LastPriceDate { get; }
    public int Instruments { get; }
    public int Accounts { get; }
    public int Trades { get; }
}
