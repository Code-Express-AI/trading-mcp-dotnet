using Microsoft.Data.SqlClient;
using TradingMcp.Repository;

namespace XTests.Integration;

// The repository puts the caller in each session's SESSION_CONTEXT, so the database can tell who's asking:
// 'caller' for the audit log and 'caller_key' for row-level security.
[Trait("Category", "Integration")]
public class When_Caller_Is_Passed_To_The_Database
{
    // Its own connection pool, so the pooling test can't pick up a connection another test left behind.
    private readonly string _connectionString =
        new SqlConnectionStringBuilder(IntegrationDatabase.ReaderConnectionString) { ApplicationName = $"caller-test-{Guid.NewGuid()}" }.ConnectionString;

    private TradingRepository RepositoryFor(string? tokenName) => IntegrationDatabase.RepositoryFor(tokenName, _connectionString);

    private static async Task<(string? Caller, string? CallerKey, short SessionId)> ReadSessionAsync(SqlConnection connection)
    {
        await using var command = new SqlCommand("""
            SELECT CAST(SESSION_CONTEXT(N'caller') AS NVARCHAR(200)),
                   CAST(SESSION_CONTEXT(N'caller_key') AS NVARCHAR(128)),
                   CAST(@@SPID AS SMALLINT);
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.IsDBNull(0) ? null : reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.GetInt16(2));
    }

    [IntegrationFact]
    public async Task it_should_let_the_database_read_the_caller_and_its_key()
    {
        await using var connection = await RepositoryFor("copilot").OpenAsync(default);

        var session = await ReadSessionAsync(connection);

        Assert.Equal("copilot", session.Caller);
        Assert.Equal("copilot", session.CallerKey);
    }

    [IntegrationFact]
    public async Task it_should_give_a_caller_with_no_identity_no_key()
    {
        await using var connection = await RepositoryFor(null).OpenAsync(default);

        var session = await ReadSessionAsync(connection);

        Assert.Equal("anonymous", session.Caller);
        Assert.Null(session.CallerKey);
    }

    [IntegrationFact]
    public async Task it_should_refuse_to_change_the_caller_key_later_in_the_session()
    {
        await using var connection = await RepositoryFor("copilot").OpenAsync(default);
        await using var command = new SqlCommand("EXEC sys.sp_set_session_context @key = N'caller_key', @value = N'local';", connection);

        await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Equal("copilot", (await ReadSessionAsync(connection)).CallerKey);
    }

    // The same physical connection serves both callers, and the second must see only its own key.
    [IntegrationFact]
    public async Task it_should_not_carry_one_caller_over_to_the_next_on_a_pooled_connection()
    {
        (string? Caller, string? CallerKey, short SessionId) first, second;

        await using (var connection = await RepositoryFor("copilot").OpenAsync(default))
        {
            first = await ReadSessionAsync(connection);
        }

        await using (var connection = await RepositoryFor("claude").OpenAsync(default))
        {
            second = await ReadSessionAsync(connection);
        }

        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal("copilot", first.CallerKey);
        Assert.Equal("claude", second.CallerKey);
    }
}
