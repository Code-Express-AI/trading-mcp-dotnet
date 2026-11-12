using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// Each read procedure writes its own row to trading.ProcedureCallLog before it returns any data, so a call is
// recorded whoever makes it, and the record can't be rolled back.
[Trait("Category", "Integration")]
public class When_Procedures_Record_Their_Own_Calls
{
    private const int RefusedInsideATransaction = 50009;

    // A name no client has, so each test can find its own rows.
    private readonly string _marker = $"Zz{Guid.NewGuid():N}"[..20];

    private Task<int> CountCallsAsync(string where = "1 = 1") => IntegrationDatabase.QueryScalarAsync<int>(
        $"SELECT COUNT(*) FROM trading.ProcedureCallLog WHERE Arguments = N'{{\"name\":\"{_marker}\"}}' AND {where}");

    private Task RunAsReaderAsync(string sql) => IntegrationDatabase.ExecuteAsync(sql, IntegrationDatabase.ReaderConnectionString);

    [IntegrationFact]
    public async Task it_should_record_who_called_which_procedure_with_which_arguments()
    {
        await IntegrationDatabase.RepositoryFor("copilot").FindAccounts(_marker, default);

        Assert.Equal(1, await CountCallsAsync(
            "ProcedureName = N'mcp.FindAccounts' AND DatabaseUser = N'mcp_reader' AND Caller = N'copilot' AND CallerKey = N'copilot'"));
    }

    // Someone with the mcp_reader password and their own code still leaves a row, with the database user it saw.
    [IntegrationFact]
    public async Task it_should_record_a_call_that_bypasses_the_server()
    {
        await RunAsReaderAsync($"EXEC mcp.FindAccounts @Name = N'{_marker}';");

        Assert.Equal(1, await CountCallsAsync("DatabaseUser = N'mcp_reader' AND Caller IS NULL AND CallerKey IS NULL"));
    }

    // Otherwise BEGIN TRAN; EXEC ...; ROLLBACK would return the data and then remove the record.
    [IntegrationFact]
    public async Task it_should_refuse_to_run_inside_a_transaction()
    {
        var exception = await Assert.ThrowsAsync<SqlException>(() =>
            RunAsReaderAsync($"BEGIN TRAN; EXEC mcp.FindAccounts @Name = N'{_marker}'; ROLLBACK;"));

        Assert.Equal(RefusedInsideATransaction, exception.Number);
        Assert.Equal(0, await CountCallsAsync());
    }

    // With implicit transactions on, the procedure's own SELECT opens a transaction. Rolling it back must not
    // remove the record, which was already committed.
    [IntegrationFact]
    public async Task it_should_keep_the_record_when_an_implicit_transaction_is_rolled_back()
    {
        await RunAsReaderAsync($"SET IMPLICIT_TRANSACTIONS ON; EXEC mcp.FindAccounts @Name = N'{_marker}'; IF @@TRANCOUNT > 0 ROLLBACK;");

        Assert.Equal(1, await CountCallsAsync());
    }
}
