using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.Data.SqlClient;
using TradingMcp.Auth;
using TradingMcp.Model;
using TradingMcp.Tools;

namespace TradingMcp.Repository;

/// <summary>
/// The server's entire view of the database. Every method is a single stored-procedure call
/// (schema "mcp"), and the SQL login behind it has EXECUTE on those procedures and nothing else.
/// </summary>
public sealed class TradingRepository(Func<SqlConnection> connect, Func<ClaimsPrincipal?> user) : ITradingRepository
{
    private const int QueryTimeoutSeconds = 15;

    // The stored procedures report arguments only the database can check, such as an unknown ticker or account,
    // with THROW and an error number of 50000 or more. Those messages are written for the model.
    private const int FirstUserDefinedErrorNumber = 50000;

    // Every session carries who's asking: 'caller' is the readable name the audit log uses, and 'caller_key' is the
    // stable key that row-level security (02_security.sql) matches against trading.CallerClient.
    // read_only: nothing later in the session can change them. A pooled connection is reset before it's reused,
    // which clears them for the next caller.
    private const string SetCallerSql = """
        EXEC sys.sp_set_session_context @key = N'caller',     @value = @Caller,    @read_only = 1;
        EXEC sys.sp_set_session_context @key = N'caller_key', @value = @CallerKey, @read_only = 1;
        """;

    internal async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var caller = user();
        var conn = connect();
        try
        {
            await conn.OpenAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(
                SetCallerSql,
                new { Caller = caller.GetCallerId(), CallerKey = caller.GetCallerKey() },
                cancellationToken: ct));
            return conn;
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string procedure, object? args, CancellationToken ct)
    {
        try
        {
            await using var conn = await OpenAsync(ct);
            var rows = await conn.QueryAsync<T>(new CommandDefinition(
                procedure, args,
                commandType: CommandType.StoredProcedure,
                commandTimeout: QueryTimeoutSeconds,
                cancellationToken: ct));
            return rows.AsList();
        }
        catch (SqlException exception) when (exception.Number >= FirstUserDefinedErrorNumber)
        {
            throw new ToolArgumentException(exception.Message);
        }
    }

    // Dapper parameters are plain DateTime, so DateOnly is converted here.
    private static DateTime? ToDate(DateOnly? d) => d?.ToDateTime(TimeOnly.MinValue);

    public Task<IReadOnlyList<DatasetInfo>> GetDatasetInfo(CancellationToken ct) =>
        QueryAsync<DatasetInfo>("mcp.GetDatasetInfo", null, ct);

    public Task<IReadOnlyList<Instrument>> SearchInstruments(string text, CancellationToken ct) =>
        QueryAsync<Instrument>("mcp.SearchInstruments", new { Text = text }, ct);

    public Task<IReadOnlyList<Price>> GetPriceHistory(string ticker, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        QueryAsync<Price>("mcp.GetPriceHistory", new { Ticker = ticker, From = ToDate(from), To = ToDate(to) }, ct);

    public Task<IReadOnlyList<Mover>> GetTopMovers(DateOnly? date, int count, CancellationToken ct) =>
        QueryAsync<Mover>("mcp.GetTopMovers", new { Date = ToDate(date), Count = count }, ct);

    public Task<IReadOnlyList<Account>> FindAccounts(string name, CancellationToken ct) =>
        QueryAsync<Account>("mcp.FindAccounts", new { Name = name }, ct);

    public Task<IReadOnlyList<Trade>> ListTrades(
        Guid accountKey, DateOnly? from, DateOnly? to, string? ticker, int page, CancellationToken ct) =>
        QueryAsync<Trade>("mcp.ListTrades",
            new { AccountKey = accountKey, From = ToDate(from), To = ToDate(to), Ticker = ticker, Page = page }, ct);

    public Task<IReadOnlyList<Position>> GetPositions(Guid accountKey, CancellationToken ct) =>
        QueryAsync<Position>("mcp.GetPositions", new { AccountKey = accountKey }, ct);

    public Task<IReadOnlyList<SectorSummary>> GetPortfolioSummary(Guid accountKey, CancellationToken ct) =>
        QueryAsync<SectorSummary>("mcp.GetPortfolioSummary", new { AccountKey = accountKey }, ct);

    /// <summary>Writes one audit row. Deliberately not cancellable: an audit entry should survive a cancelled request.</summary>
    public async Task WriteAuditAsync(
        string caller, string tool, string parameters, int? rows, int durationMs, bool success, string? error)
    {
        await using var conn = await OpenAsync(CancellationToken.None);
        await conn.ExecuteAsync(new CommandDefinition(
            "mcp.WriteAudit",
            new
            {
                Caller = caller,
                ToolName = tool,
                Parameters = parameters,
                RowsReturned = rows,
                DurationMs = durationMs,
                Success = success,
                Error = error
            },
            commandType: CommandType.StoredProcedure,
            commandTimeout: 5));
    }
}
