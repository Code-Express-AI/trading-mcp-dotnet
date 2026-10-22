namespace TradingMcp.Model;

public sealed record Mover(string Ticker, string Name, DateTime PriceDate, decimal ClosePrice, decimal PrevClose, decimal ChangePct);
