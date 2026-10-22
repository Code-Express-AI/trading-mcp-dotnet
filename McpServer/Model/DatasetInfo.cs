namespace TradingMcp.Model;

public sealed record DatasetInfo(DateTime FirstPriceDate, DateTime LastPriceDate, int Instruments, int Accounts, int Trades);
