using TradingMcp.Model;

namespace TradingMcp.Repository;

public interface ITradingRepository
{
    Task<IReadOnlyList<DatasetInfo>> GetDatasetInfo(CancellationToken ct);
    Task<IReadOnlyList<Instrument>> SearchInstruments(string text, CancellationToken ct);
    Task<IReadOnlyList<Price>> GetPriceHistory(string ticker, DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<IReadOnlyList<Mover>> GetTopMovers(DateOnly? date, int count, CancellationToken ct);
    Task<IReadOnlyList<Account>> FindAccounts(string name, CancellationToken ct);
    Task<IReadOnlyList<Trade>> ListTrades(
        int accountId, DateOnly? from, DateOnly? to, string? ticker, int page, CancellationToken ct);
    Task<IReadOnlyList<Position>> GetPositions(int accountId, CancellationToken ct);
    Task<IReadOnlyList<SectorSummary>> GetPortfolioSummary(int accountId, CancellationToken ct);
    Task WriteAuditAsync(
        string caller, string tool, string parameters, int? rows, int durationMs, bool success, string? error);
}
