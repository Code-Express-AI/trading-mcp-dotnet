using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// The security model in one place: mcp_reader can run the granted mcp stored procedures and nothing else.
// Even if the server code (or a prompt-injected model) tried raw SQL, the database refuses it.
[Trait("Category", "Integration")]
public class When_The_Mcp_Reader_Bypasses_The_Procedures
{
    private const int PermissionDenied = 229;

    private static async Task<SqlException> RunAsReaderAsync(string sql) =>
        await Assert.ThrowsAsync<SqlException>(() => IntegrationDatabase.ExecuteAsync(sql, IntegrationDatabase.ReaderConnectionString));

    [IntegrationFact]
    public async Task it_should_refuse_to_read_a_table_directly()
    {
        Assert.Equal(PermissionDenied, (await RunAsReaderAsync("SELECT TOP 1 * FROM trading.Trade")).Number);
    }

    [IntegrationFact]
    public async Task it_should_refuse_to_change_data()
    {
        Assert.Equal(PermissionDenied, (await RunAsReaderAsync("DELETE FROM trading.Trade")).Number);
    }

    [IntegrationFact]
    public async Task it_should_refuse_to_read_the_audit_log()
    {
        Assert.Equal(PermissionDenied, (await RunAsReaderAsync("SELECT TOP 1 * FROM trading.AuditLog")).Number);
    }

    // EXECUTE is granted procedure by procedure, so one added to the mcp schema by mistake can't be run.
    [IntegrationFact]
    public async Task it_should_refuse_a_procedure_that_was_not_granted()
    {
        var admin = IntegrationDatabase.AdminConnectionString();
        await IntegrationDatabase.ExecuteAsync("CREATE OR ALTER PROCEDURE mcp.NotGranted AS SELECT COUNT(*) FROM trading.Trade;", admin);
        try
        {
            Assert.Equal(PermissionDenied, (await RunAsReaderAsync("EXEC mcp.NotGranted")).Number);
        }
        finally
        {
            await IntegrationDatabase.ExecuteAsync("DROP PROCEDURE mcp.NotGranted;", admin);
        }
    }
}
