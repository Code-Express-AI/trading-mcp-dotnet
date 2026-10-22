using Microsoft.Data.SqlClient;
using TradingDataGenerator;
using TradingDataGenerator.Database;

namespace XTests.Integration;

// Real SQL Server errors, to prove the DataGenerator recognises them and explains the fix.
[Trait("Category", "Integration")]
public class When_Sql_Server_Rejects_The_Data_Generator
{
    private readonly StringWriter _errorOutput = new();

    private async Task<string> RunAsync(string connectionString)
    {
        var loader = new DataLoader(cs => new DatabaseSeeder(cs), TextWriter.Null, _errorOutput);

        var exitCode = await loader.RunAsync([], connectionString, Path.GetTempPath());

        Assert.Equal(ExitCodes.DatabaseError, exitCode);
        return _errorOutput.ToString();
    }

    private static string AdminPassword => new SqlConnectionStringBuilder(IntegrationDatabase.AdminConnectionString()).Password;

    [IntegrationFact]
    public async Task it_should_explain_a_wrong_password()
    {
        var error = await RunAsync(IntegrationDatabase.Build("sa", "wrong-password", IntegrationDatabase.DatabaseName));

        Assert.StartsWith("SQL Server at localhost,1433 rejected the login for user 'sa'.", error);
    }

    [IntegrationFact]
    public async Task it_should_explain_a_missing_database()
    {
        var error = await RunAsync(IntegrationDatabase.Build("sa", AdminPassword, "NoSuchDatabase"));

        Assert.StartsWith("The database 'NoSuchDatabase' doesn't exist on localhost,1433.", error);
    }

    [IntegrationFact]
    public async Task it_should_explain_that_the_mcp_reader_cannot_load_data()
    {
        var error = await RunAsync(IntegrationDatabase.ReaderConnectionString);

        Assert.StartsWith("User 'mcp_reader' doesn't have permission to load data.", error);
    }

    [IntegrationFact]
    public async Task it_should_explain_an_untrusted_certificate()
    {
        var error = await RunAsync(IntegrationDatabase.Build("sa", AdminPassword, IntegrationDatabase.DatabaseName, trustServerCertificate: false));

        Assert.StartsWith("SQL Server's certificate isn't trusted.", error);
    }
}
