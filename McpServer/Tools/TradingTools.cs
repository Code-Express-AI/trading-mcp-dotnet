using System.ComponentModel;
using ModelContextProtocol.Server;
using TradingMcp.Audit;
using TradingMcp.Model;
using TradingMcp.Repository;

namespace TradingMcp.Tools;

/// <summary>
/// Read-only tools over the synthetic trading database. All data is fictitious.
/// Each tool checks its arguments, then calls one stored procedure, all inside the audit wrapper,
/// so rejected calls are audited too.
/// The descriptions matter: they are what the model reads to decide which tool to call.
/// </summary>
[McpServerToolType]
public sealed class TradingTools(ITradingRepository repo, IAuditService audit)
{
    [McpServerTool(Name = "get_dataset_info", ReadOnly = true)]
    [Description("Describes the dataset: first and last price date, and how many instruments, accounts and trades exist. " +
                 "Call this first to learn which dates are available before asking about 'last month' or 'latest'.")]
    public Task<IReadOnlyList<DatasetInfo>> GetDatasetInfo(CancellationToken ct = default) =>
        audit.RunAsync("get_dataset_info", new { }, () => repo.GetDatasetInfo(ct));

    [McpServerTool(Name = "search_instruments", ReadOnly = true)]
    [Description("Finds fictitious listed companies by ticker prefix or part of the company name. Returns at most 25.")]
    public Task<IReadOnlyList<Instrument>> SearchInstruments(
        [Description("Ticker prefix or part of a company name, e.g. 'MAR' or 'Pharma'.")] string text,
        CancellationToken ct = default) =>
        audit.RunAsync("search_instruments", new { text }, () =>
        {
            ToolArguments.Require(!string.IsNullOrWhiteSpace(text), "text must not be empty, for example 'MAR' or 'Pharma'.");
            ToolArguments.Require(text.Length <= 100, "text must be at most 100 characters.");
            return repo.SearchInstruments(text, ct);
        });

    [McpServerTool(Name = "get_price_history", ReadOnly = true)]
    [Description("Daily open/high/low/close/volume for one ticker, oldest first. " +
                 "Returns at most the 250 most recent trading days inside the requested range.")]
    public Task<IReadOnlyList<Price>> GetPriceHistory(
        [Description("Exact ticker, e.g. 'BRIP'. Use search_instruments if unsure.")] string ticker,
        [Description("Start date, yyyy-MM-dd. Optional.")] string from = "",
        [Description("End date, yyyy-MM-dd. Optional.")] string to = "",
        CancellationToken ct = default) =>
        audit.RunAsync("get_price_history", new { ticker, from, to }, () =>
        {
            ToolArguments.Require(!string.IsNullOrWhiteSpace(ticker), "ticker must not be empty. Use search_instruments to find one.");
            ToolArguments.Require(ticker.Length <= 10, "ticker must be at most 10 characters. Use search_instruments to find one.");
            var (fromDate, toDate) = ToolArguments.ParseDateRange(from, to);
            return repo.GetPriceHistory(ticker, fromDate, toDate, ct);
        });

    [McpServerTool(Name = "get_top_movers", ReadOnly = true)]
    [Description("The instruments with the largest absolute daily % price change on a given trading day. " +
                 "A weekend or holiday date snaps back to the previous trading day.")]
    public Task<IReadOnlyList<Mover>> GetTopMovers(
        [Description("Trading day, yyyy-MM-dd. Defaults to the latest day available.")] string date = "",
        [Description("How many to return, 1-20. Default 5.")] int count = 5,
        CancellationToken ct = default) =>
        audit.RunAsync("get_top_movers", new { date, count }, () =>
        {
            var tradingDay = ToolArguments.ParseDate(date, nameof(date));
            ToolArguments.Require(count is >= 1 and <= 20, "count must be between 1 and 20.");
            return repo.GetTopMovers(tradingDay, count, ct);
        });

    [McpServerTool(Name = "find_accounts", ReadOnly = true)]
    [Description("Finds client accounts by (part of) the client's name. Returns at most 25. " +
                 "Account numbers are masked for privacy, so use the returned accountKey in the other tools.")]
    public Task<IReadOnlyList<Account>> FindAccounts(
        [Description("At least 2 characters of the client's name, e.g. 'Thompson'.")] string name,
        CancellationToken ct = default) =>
        audit.RunAsync("find_accounts", new { name }, () =>
        {
            ToolArguments.Require(name?.Trim().Length >= 2, "name must be at least 2 characters, for example 'Thompson'.");
            ToolArguments.Require(name!.Length <= 100, "name must be at most 100 characters.");
            return repo.FindAccounts(name, ct);
        });

    [McpServerTool(Name = "list_trades", ReadOnly = true)]
    [Description("An account's trades, newest first, 50 per page. Optionally filter by date range or ticker.")]
    public Task<IReadOnlyList<Trade>> ListTrades(
        [Description("The accountKey returned by find_accounts.")] string accountKey,
        [Description("Start date, yyyy-MM-dd. Optional.")] string from = "",
        [Description("End date, yyyy-MM-dd (inclusive). Optional.")] string to = "",
        [Description("Only trades in this exact ticker. Optional.")] string ticker = "",
        [Description("Page number, 1-1000. Default 1.")] int page = 1,
        CancellationToken ct = default) =>
        audit.RunAsync("list_trades", new { accountKey, from, to, ticker, page }, () =>
        {
            var key = ToolArguments.ParseAccountKey(accountKey);
            var (fromDate, toDate) = ToolArguments.ParseDateRange(from, to);
            ToolArguments.Require(page is >= 1 and <= 1000, "page must be between 1 and 1000.");
            ToolArguments.Require(ticker is null || ticker.Length <= 10, "ticker must be at most 10 characters. Use search_instruments to find one.");
            return repo.ListTrades(key, fromDate, toDate, string.IsNullOrWhiteSpace(ticker) ? null : ticker, page, ct);
        });

    [McpServerTool(Name = "get_positions", ReadOnly = true)]
    [Description("Current holdings for an account, valued at the latest close: quantity, average buy price, " +
                 "market value and unrealised profit/loss (amount and %), largest first.")]
    public Task<IReadOnlyList<Position>> GetPositions(
        [Description("The accountKey returned by find_accounts.")] string accountKey,
        CancellationToken ct = default) =>
        audit.RunAsync("get_positions", new { accountKey }, () =>
        {
            var key = ToolArguments.ParseAccountKey(accountKey);
            return repo.GetPositions(key, ct);
        });

    [McpServerTool(Name = "get_portfolio_summary", ReadOnly = true)]
    [Description("An account's market value and unrealised P&L grouped by sector, with a TOTAL row.")]
    public Task<IReadOnlyList<SectorSummary>> GetPortfolioSummary(
        [Description("The accountKey returned by find_accounts.")] string accountKey,
        CancellationToken ct = default) =>
        audit.RunAsync("get_portfolio_summary", new { accountKey }, () =>
        {
            var key = ToolArguments.ParseAccountKey(accountKey);
            return repo.GetPortfolioSummary(key, ct);
        });
}
