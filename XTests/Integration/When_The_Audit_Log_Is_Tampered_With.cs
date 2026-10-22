using System.Data;
using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// The audit log is an append-only ledger table: even sa, the most powerful login, can't rewrite history.
[Trait("Category", "Integration")]
public class When_The_Audit_Log_Is_Tampered_With
{
    private const int AppendOnlyLedgerTable = 37359;

    private static async Task<SqlException> RunAsAdminAsync(string sql) =>
        await Assert.ThrowsAsync<SqlException>(() => IntegrationDatabase.ExecuteAsync(sql, IntegrationDatabase.AdminConnectionString()));

    [IntegrationFact]
    public async Task it_should_refuse_to_change_a_row_even_for_sa()
    {
        var exception = await RunAsAdminAsync("UPDATE trading.AuditLog SET Success = 1");

        Assert.Equal(AppendOnlyLedgerTable, exception.Number);
    }

    [IntegrationFact]
    public async Task it_should_refuse_to_delete_a_row_even_for_sa()
    {
        var exception = await RunAsAdminAsync("DELETE FROM trading.AuditLog");

        Assert.Equal(AppendOnlyLedgerTable, exception.Number);
    }

    [IntegrationFact]
    public async Task it_should_verify_against_the_latest_ledger_digest()
    {
        await using var connection = new SqlConnection(IntegrationDatabase.AdminConnectionString());
        await connection.OpenAsync();
        await using var generate = new SqlCommand("sys.sp_generate_database_ledger_digest", connection) { CommandType = CommandType.StoredProcedure };
        var digest = (string)(await generate.ExecuteScalarAsync())!;

        // Throws if any ledger row no longer matches the hash chain.
        await using var verify = new SqlCommand("sys.sp_verify_database_ledger", connection) { CommandType = CommandType.StoredProcedure };
        verify.Parameters.AddWithValue("@digests", $"[{digest}]");
        await verify.ExecuteNonQueryAsync();
    }
}
