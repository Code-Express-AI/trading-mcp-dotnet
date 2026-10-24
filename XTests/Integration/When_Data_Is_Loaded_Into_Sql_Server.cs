using Microsoft.Data.SqlClient;
using TradingDataGenerator;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;

namespace XTests.Integration;

// Loads generated data into a throwaway database with the real schema, so the demo database is never touched.
[Trait("Category", "Integration")]
public class When_Data_Is_Loaded_Into_Sql_Server : IAsyncLifetime
{
    private readonly string _database = $"TradingDemoTest_{Guid.NewGuid():N}";
    private bool _created;

    private string ConnectionString => IntegrationDatabase.AdminConnectionString(_database);

    public async ValueTask InitializeAsync()
    {
        if (IntegrationDatabase.SkipReason is not null)
        {
            return;
        }

        await IntegrationDatabase.ExecuteAsync($"CREATE DATABASE [{_database}]", IntegrationDatabase.AdminConnectionString("master"));
        _created = true;
        await IntegrationDatabase.RunScriptAsync("01_schema.sql", ConnectionString);
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

    private static GeneratorOptions Options(int seed) => GeneratorOptions.Parse(
        [$"--seed={seed}", "--years=1", "--instruments=5", "--clients=10", "--trades-per-account=5", "--end-date=2025-12-31"],
        "Server=unused;Database=unused",
        new DateTime(2026, 3, 15),
        Path.GetTempPath());

    private Task<int> CountAsync(string table) => IntegrationDatabase.QueryScalarAsync<int>($"SELECT COUNT(*) FROM {table}", _database);

    [IntegrationFact]
    public async Task it_should_load_every_table_and_then_report_that_it_contains_data()
    {
        var seeder = new DatabaseSeeder(ConnectionString);
        var data = SyntheticData.Generate(Options(seed: 42));

        Assert.False(await seeder.ContainsDataAsync());
        await seeder.SeedAsync(data, clearExistingData: false);

        Assert.True(await seeder.ContainsDataAsync());
        Assert.Equal(data.Instruments.Count, await CountAsync("trading.Instrument"));
        Assert.Equal(data.InstrumentPrices.Count, await CountAsync("trading.InstrumentPrice"));
        Assert.Equal(data.Clients.Count, await CountAsync("trading.Client"));
        Assert.Equal(data.Accounts.Count, await CountAsync("trading.Account"));
        Assert.Equal(data.Trades.Count, await CountAsync("trading.Trade"));
    }

    [IntegrationFact]
    public async Task it_should_satisfy_the_schema_s_rules_and_never_hold_a_negative_position()
    {
        await new DatabaseSeeder(ConnectionString).SeedAsync(SyntheticData.Generate(Options(seed: 42)), clearExistingData: false);

        Assert.Equal(0, await IntegrationDatabase.QueryScalarAsync<int>(
            "SELECT COUNT(*) FROM trading.v_Position WHERE NetQuantity < 0", _database));
        Assert.Equal(0, await IntegrationDatabase.QueryScalarAsync<int>("""
            SELECT COUNT(*) FROM trading.Trade t
            JOIN trading.InstrumentPrice p ON p.InstrumentId = t.InstrumentId AND p.PriceDate = CAST(t.TradeTimestamp AS date)
            WHERE t.Price < p.LowPrice OR t.Price > p.HighPrice
            """, _database));
    }

    [IntegrationFact]
    public async Task it_should_replace_the_existing_data_and_restart_trade_ids_when_clearing()
    {
        var seeder = new DatabaseSeeder(ConnectionString);
        await seeder.SeedAsync(SyntheticData.Generate(Options(seed: 1)), clearExistingData: false);
        var replacement = SyntheticData.Generate(Options(seed: 2));

        await seeder.SeedAsync(replacement, clearExistingData: true);

        Assert.Equal(replacement.Trades.Count, await CountAsync("trading.Trade"));
        Assert.Equal(1L, await IntegrationDatabase.QueryScalarAsync<long>("SELECT MIN(TradeId) FROM trading.Trade", _database));
    }
}
