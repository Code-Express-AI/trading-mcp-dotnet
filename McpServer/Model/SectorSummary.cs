namespace TradingMcp.Model;

public sealed record SectorSummary(string Sector, string Currency, int Positions, decimal MarketValue, decimal UnrealisedPnL);
