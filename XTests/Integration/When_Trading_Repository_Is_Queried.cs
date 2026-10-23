using Microsoft.Data.SqlClient;
using TradingMcp.Repository;
using TradingMcp.Tools;

namespace XTests.Integration;

// Calls every stored procedure as mcp_reader, exactly as the MCP server does, against the demo database.
[Trait("Category", "Integration")]
public class When_Trading_Repository_Is_Queried
{
    private readonly TradingRepository _repository = new(() => new SqlConnection(IntegrationDatabase.ReaderConnectionString));

    // Fixtures are looked up as sa, so the tests work with whatever data the generator produced.
    private static Task<string> AnyTickerAsync() =>
        IntegrationDatabase.QueryScalarAsync<string>("SELECT TOP 1 Ticker FROM trading.Instruments ORDER BY Ticker");

    private static Task<int> AnAccountWithPositionsAsync() =>
        IntegrationDatabase.QueryScalarAsync<int>("SELECT TOP 1 AccountId FROM trading.v_Positions ORDER BY AccountId");

    [IntegrationFact]
    public async Task it_should_map_the_results_of_every_stored_procedure()
    {
        var ticker = await AnyTickerAsync();
        var accountId = await AnAccountWithPositionsAsync();
        var clientName = await IntegrationDatabase.QueryScalarAsync<string>(
            $"SELECT c.FullName FROM trading.Accounts a JOIN trading.Clients c ON c.ClientId = a.ClientId WHERE a.AccountId = {accountId}");

        Assert.NotEmpty(await _repository.GetDatasetInfo(default));
        Assert.NotEmpty(await _repository.SearchInstruments(ticker[..3], default));
        Assert.NotEmpty(await _repository.GetPriceHistory(ticker, null, null, default));
        Assert.NotEmpty(await _repository.GetTopMovers(null, 5, default));
        Assert.Contains(await _repository.FindAccounts(clientName, default), account => account.AccountId == accountId);
        Assert.NotEmpty(await _repository.ListTrades(accountId, null, null, null, 1, default));
        Assert.NotEmpty(await _repository.GetPositions(accountId, default));
        Assert.Contains(await _repository.GetPortfolioSummary(accountId, default), summary => summary.Sector == "TOTAL");
    }

    [IntegrationFact]
    public async Task it_should_mask_account_numbers_for_the_mcp_reader()
    {
        var accountId = await AnAccountWithPositionsAsync();
        var clientName = await IntegrationDatabase.QueryScalarAsync<string>(
            $"SELECT c.FullName FROM trading.Accounts a JOIN trading.Clients c ON c.ClientId = a.ClientId WHERE a.AccountId = {accountId}");

        var accounts = await _repository.FindAccounts(clientName, default);

        Assert.All(accounts, account => Assert.Contains("XXXXX", account.AccountNumber));
    }

    [IntegrationFact]
    public async Task it_should_treat_like_wildcards_in_search_text_as_literal_characters()
    {
        Assert.Empty(await _repository.FindAccounts("%%", default));
        Assert.Empty(await _repository.FindAccounts("__", default));
        Assert.Empty(await _repository.SearchInstruments("%", default));
        Assert.Empty(await _repository.SearchInstruments("_", default));
    }

    [IntegrationFact]
    public async Task it_should_report_an_unknown_ticker_as_an_argument_problem_for_the_model()
    {
        var exception = await Assert.ThrowsAsync<ToolArgumentException>(() => _repository.GetPriceHistory("NOPE", null, null, default));

        Assert.Equal("Unknown ticker. Use search_instruments to find the right one.", exception.Message);
    }

    [IntegrationFact]
    public async Task it_should_report_a_date_before_the_data_as_an_argument_problem_for_the_model()
    {
        var exception = await Assert.ThrowsAsync<ToolArgumentException>(() => _repository.GetTopMovers(new DateOnly(1900, 1, 1), 5, default));

        Assert.Equal("No prices on or before this date. Call get_dataset_info for the available range.", exception.Message);
    }

    [IntegrationFact]
    public async Task it_should_report_an_unknown_account_as_an_argument_problem_for_the_model()
    {
        var exception = await Assert.ThrowsAsync<ToolArgumentException>(() => _repository.GetPositions(int.MaxValue, default));

        Assert.Equal("Unknown accountId. Use find_accounts to look it up.", exception.Message);
    }

    [IntegrationFact]
    public async Task it_should_write_an_audit_row()
    {
        const string countSql = "SELECT COUNT(*) FROM trading.AuditLog WHERE Caller = 'integration-test'";
        var before = await IntegrationDatabase.QueryScalarAsync<int>(countSql);

        await _repository.WriteAuditAsync("integration-test", "get_dataset_info", "{}", 1, 5, success: true, error: null);

        Assert.Equal(before + 1, await IntegrationDatabase.QueryScalarAsync<int>(countSql));
    }
}
