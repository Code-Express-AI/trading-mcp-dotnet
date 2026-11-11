using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// A snapshot of everything mcp_reader may do. The bypass tests check the mistakes we thought of; this one fails on
// any grant, deny or role membership that 02_security.sql and 03_procedures.sql don't make. Adding a tool means
// adding its GRANT EXECUTE line here too.
[Trait("Category", "Integration")]
public class When_Mcp_Reader_Permissions_Are_Listed
{
    [IntegrationFact]
    public async Task it_should_hold_only_the_permissions_the_scripts_grant()
    {
        string[] expected =
        [
            "mcp_reader DENY ALTER SCHEMA::trading",
            "mcp_reader DENY DELETE SCHEMA::trading",
            "mcp_reader DENY INSERT SCHEMA::trading",
            "mcp_reader DENY SELECT SCHEMA::trading",
            "mcp_reader DENY UPDATE SCHEMA::trading",
            "mcp_reader GRANT CONNECT",
            "mcp_reader GRANT EXECUTE mcp.FindAccounts",
            "mcp_reader GRANT EXECUTE mcp.GetDatasetInfo",
            "mcp_reader GRANT EXECUTE mcp.GetPortfolioSummary",
            "mcp_reader GRANT EXECUTE mcp.GetPositions",
            "mcp_reader GRANT EXECUTE mcp.GetPriceHistory",
            "mcp_reader GRANT EXECUTE mcp.GetTopMovers",
            "mcp_reader GRANT EXECUTE mcp.ListTrades",
            "mcp_reader GRANT EXECUTE mcp.SearchInstruments",
            "mcp_reader GRANT EXECUTE mcp.WriteAudit",
            // Every user is in public, so a grant to public is a grant to mcp_reader. These two are SQL Server's defaults.
            "public GRANT VIEW ANY COLUMN ENCRYPTION KEY DEFINITION",
            "public GRANT VIEW ANY COLUMN MASTER KEY DEFINITION"
        ];

        Assert.Equal(expected, await ListExplicitPermissionsAsync());
    }

    // What mcp_reader can actually do, however it got the right: directly, through a role or through public.
    // A DENY doesn't stop db_owner, so this catches what the list above can't: a role membership's effect.
    [IntegrationFact]
    public async Task it_should_gain_nothing_more_through_roles()
    {
        string[] expected =
        [
            "DATABASE CONNECT",
            "DATABASE VIEW ANY COLUMN ENCRYPTION KEY DEFINITION",
            "DATABASE VIEW ANY COLUMN MASTER KEY DEFINITION"
        ];

        var actual = await ReadRowsAsync(
            """
            SELECT 'DATABASE', permission_name FROM sys.fn_my_permissions(NULL, 'DATABASE')
            UNION ALL SELECT 'SCHEMA::mcp', permission_name FROM sys.fn_my_permissions('mcp', 'SCHEMA')
            UNION ALL SELECT 'SCHEMA::trading', permission_name FROM sys.fn_my_permissions('trading', 'SCHEMA');
            """,
            IntegrationDatabase.ReaderConnectionString,
            row => $"{row.GetString(0)} {row.GetString(1)}");

        Assert.Equal(expected, actual);
    }

    // The columns are formatted in C#: in this contained database, catalog names and string literals have
    // different collations, so joining them in SQL fails.
    private static async Task<string[]> ListExplicitPermissionsAsync()
    {
        var admin = IntegrationDatabase.AdminConnectionString();

        var permissions = await ReadRowsAsync(
            """
            SELECT USER_NAME(grantee_principal_id), state_desc, permission_name, class_desc,
                   SCHEMA_NAME(major_id), OBJECT_SCHEMA_NAME(major_id), OBJECT_NAME(major_id), major_id
            FROM sys.database_permissions
            WHERE grantee_principal_id IN (USER_ID('mcp_reader'), USER_ID('public'))
              -- public's SELECT on the system views is SQL Server's own and differs between versions.
              AND NOT (grantee_principal_id = USER_ID('public') AND class_desc = 'OBJECT_OR_COLUMN'
                       AND OBJECT_SCHEMA_NAME(major_id) = 'sys');
            """,
            admin,
            row => $"{row.GetString(0)} {row.GetString(1)} {row.GetString(2)}" + row.GetString(3) switch
            {
                "DATABASE" => "",
                "SCHEMA" => $" SCHEMA::{row.GetString(4)}",
                "OBJECT_OR_COLUMN" => $" {row.GetString(5)}.{row.GetString(6)}",
                var other => $" {other} {row.GetInt32(7)}"
            });

        var roles = await ReadRowsAsync(
            """
            SELECT USER_NAME(role_principal_id)
            FROM sys.database_role_members
            WHERE member_principal_id = USER_ID('mcp_reader');
            """,
            admin,
            row => $"mcp_reader MEMBER OF {row.GetString(0)}");

        return [.. permissions.Concat(roles).Order(StringComparer.Ordinal)];
    }

    private static async Task<string[]> ReadRowsAsync(string sql, string connectionString, Func<SqlDataReader, string> format)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(format(reader));
        }

        return [.. rows.Order(StringComparer.Ordinal)];
    }
}
