using Microsoft.Data.SqlClient;
using TradingMcp.Repository;

namespace XTests.Integration;

// The repository puts the caller in each session's SESSION_CONTEXT, so the database can tell who's asking.
[Trait("Category", "Integration")]
public class When_Caller_Is_Passed_To_The_Database
{
    // Its own connection pool, so the pooling test can't pick up a connection another test left behind.
    private readonly string _connectionString =
        new SqlConnectionStringBuilder(IntegrationDatabase.ReaderConnectionString) { ApplicationName = $"caller-test-{Guid.NewGuid()}" }.ConnectionString;

    private TradingRepository RepositoryFor(string caller) => new(() => new SqlConnection(_connectionString), () => caller);

    private static async Task<(string? Caller, short SessionId)> ReadSessionAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("SELECT CAST(SESSION_CONTEXT(N'caller') AS NVARCHAR(200)), CAST(@@SPID AS SMALLINT);", connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.IsDBNull(0) ? null : reader.GetString(0), reader.GetInt16(1));
    }

    [IntegrationFact]
    public async Task it_should_let_the_database_read_the_caller()
    {
        await using var connection = await RepositoryFor("copilot").OpenAsync(default);

        Assert.Equal("copilot", (await ReadSessionAsync(connection)).Caller);
    }

    [IntegrationFact]
    public async Task it_should_refuse_to_change_the_caller_later_in_the_session()
    {
        await using var connection = await RepositoryFor("copilot").OpenAsync(default);
        await using var command = new SqlCommand("EXEC sys.sp_set_session_context @key = N'caller', @value = N'someone-else';", connection);

        await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("copilot", (await ReadSessionAsync(connection)).Caller);
    }

    // The same physical connection serves both callers, and the second must see only its own name.
    [IntegrationFact]
    public async Task it_should_not_carry_one_caller_over_to_the_next_on_a_pooled_connection()
    {
        (string? Caller, short SessionId) first, second;

        await using (var connection = await RepositoryFor("copilot").OpenAsync(default))
        {
            first = await ReadSessionAsync(connection);
        }

        await using (var connection = await RepositoryFor("claude").OpenAsync(default))
        {
            second = await ReadSessionAsync(connection);
        }

        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal("copilot", first.Caller);
        Assert.Equal("claude", second.Caller);
    }
}
