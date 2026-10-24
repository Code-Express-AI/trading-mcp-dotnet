using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// The security model in one place: mcp_reader can run the mcp stored procedures and nothing else.
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
}
