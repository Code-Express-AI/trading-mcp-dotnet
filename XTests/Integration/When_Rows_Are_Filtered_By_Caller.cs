using Microsoft.Data.SqlClient;
using TradingDataGenerator;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;
using TradingMcp.Repository;
using TradingMcp.Tools;

namespace XTests.Integration;

// Row-level security, end to end: the real scripts and data loader in a throwaway database, queried as mcp_reader
// through the repository, exactly as the server does. With 10 clients, "copilot" sees clients 1, 5 and 9 plus the
// shared 4 and 8, "claude" sees 2, 6 and 10 plus the shared ones, and "local" sees all of them.
[Trait("Category", "Integration")]
public class When_Rows_Are_Filtered_By_Caller : IAsyncLifetime
{
    private readonly string _database = $"TradingDemoTest_{Guid.NewGuid():N}";
    private bool _created;

    public async ValueTask InitializeAsync()
    {
        if (IntegrationDatabase.SkipReason is not null)
        {
            return;
        }

        await IntegrationDatabase.ExecuteAsync(
            $"CREATE DATABASE [{_database}]; ALTER DATABASE [{_database}] SET CONTAINMENT = PARTIAL;",
            IntegrationDatabase.AdminConnectionString("master"));
        _created = true;

        var admin = IntegrationDatabase.AdminConnectionString(_database);
        await IntegrationDatabase.RunScriptAsync("01_schema.sql", admin);
        await IntegrationDatabase.RunScriptAsync("02_security.sql", admin,
            script => script.Replace("<<replace-with-a-long-random-password>>", IntegrationDatabase.ReaderPassword));
        await IntegrationDatabase.RunScriptAsync("03_procedures.sql", admin);

        var options = GeneratorOptions.Parse(
            ["--seed=42", "--years=1", "--instruments=5", "--clients=10", "--trades-per-account=5", "--end-date=2025-12-31"],
            "Server=unused;Database=unused", new DateTime(2026, 3, 15), Path.GetTempPath());
        await new DatabaseSeeder(admin).SeedAsync(SyntheticData.Generate(options), clearExistingData: false);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_created)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        await IntegrationDatabase.ExecuteAsync(
            $"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_database}];",
            IntegrationDatabase.AdminConnectionString("master"));
    }

    private TradingRepository RepositoryFor(string? tokenName) =>
        IntegrationDatabase.RepositoryFor(tokenName, IntegrationDatabase.ReaderConnectionStringFor(_database));

    // Looked up as sa (db_owner), which row-level security doesn't filter.
    private Task<T> AsAdminAsync<T>(string sql) => IntegrationDatabase.QueryScalarAsync<T>(sql, _database);

    private Task<int> CountAsAdminAsync(string table, string where = "1 = 1") =>
        AsAdminAsync<int>($"SELECT COUNT(*) FROM {table} t WHERE {where}");

    // The same rule as CallerClientGenerator: copilot's quarter, the shared quarter, and every Thompson.
    private const string CopilotsClientIds =
        "SELECT ClientId FROM trading.Client WHERE ClientId % 4 IN (0, 1) OR FullName LIKE N'% Thompson'";
    private const string CopilotsAccounts = $"t.ClientId IN ({CopilotsClientIds})";
    private const string CopilotsTrades = $"t.AccountId IN (SELECT AccountId FROM trading.Account WHERE ClientId IN ({CopilotsClientIds}))";

    [IntegrationFact]
    public async Task it_should_show_the_local_token_every_account_and_trade()
    {
        var info = (await RepositoryFor("local").GetDatasetInfo(default)).Single();

        Assert.Equal(await CountAsAdminAsync("trading.Account"), info.Accounts);
        Assert.Equal(await CountAsAdminAsync("trading.Trade"), info.Trades);
    }

    [IntegrationFact]
    public async Task it_should_show_a_named_token_only_its_own_clients_accounts_and_trades()
    {
        var info = (await RepositoryFor("copilot").GetDatasetInfo(default)).Single();

        Assert.Equal(await CountAsAdminAsync("trading.Account", CopilotsAccounts), info.Accounts);
        Assert.Equal(await CountAsAdminAsync("trading.Trade", CopilotsTrades), info.Trades);
        Assert.True(info.Accounts < await CountAsAdminAsync("trading.Account"));
    }

    [IntegrationFact]
    public async Task it_should_give_two_tokens_different_answers_to_the_same_question()
    {
        var copilotsClient = await AsAdminAsync<string>("SELECT FullName FROM trading.Client WHERE ClientId = 1");

        Assert.NotEmpty(await RepositoryFor("copilot").FindAccounts(copilotsClient, default));
        Assert.Empty(await RepositoryFor("claude").FindAccounts(copilotsClient, default));
    }

    // The same message as for a key that doesn't exist, so the error doesn't confirm that the account is there.
    [IntegrationFact]
    public async Task it_should_report_another_callers_account_as_unknown()
    {
        var claudesAccount = await AsAdminAsync<Guid>("SELECT TOP 1 AccountKey FROM trading.Account WHERE ClientId = 2");
        var copilot = RepositoryFor("copilot");

        var hidden = await Assert.ThrowsAsync<ToolArgumentException>(() => copilot.GetPositions(claudesAccount, default));
        var missing = await Assert.ThrowsAsync<ToolArgumentException>(() => copilot.GetPositions(Guid.NewGuid(), default));

        Assert.Equal(missing.Message, hidden.Message);
    }

    [IntegrationFact]
    public async Task it_should_show_nothing_to_an_unmapped_caller_or_one_with_no_identity()
    {
        foreach (var tokenName in new[] { "nobody", null })
        {
            var info = (await RepositoryFor(tokenName).GetDatasetInfo(default)).Single();

            Assert.Equal(0, info.Accounts);
            Assert.Equal(0, info.Trades);
        }
    }

    [IntegrationFact]
    public async Task it_should_still_show_the_admin_every_client()
    {
        Assert.Equal(10, await CountAsAdminAsync("trading.Client"));
    }

    // --reset deletes as the admin. If row-level security filtered the admin, the deletes would miss rows.
    [IntegrationFact]
    public async Task it_should_let_the_admin_replace_the_data()
    {
        var options = GeneratorOptions.Parse(
            ["--seed=7", "--years=1", "--instruments=5", "--clients=10", "--trades-per-account=5", "--end-date=2025-12-31"],
            "Server=unused;Database=unused", new DateTime(2026, 3, 15), Path.GetTempPath());

        var data = SyntheticData.Generate(options);

        await new DatabaseSeeder(IntegrationDatabase.AdminConnectionString(_database)).SeedAsync(data, clearExistingData: true);

        Assert.Equal(10, await CountAsAdminAsync("trading.Client"));
        Assert.Equal(data.CallerClients.Count, await CountAsAdminAsync("trading.CallerClient"));
    }
}
