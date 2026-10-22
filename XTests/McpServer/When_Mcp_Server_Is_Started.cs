using System.Diagnostics;

namespace XTests.McpServer;

// Starts the real McpServer executable, so this is an end-to-end test rather than a unit test.
// Production mode stops it reading this machine's user-secrets, so only the settings given here apply.
[Trait("Category", "Integration")]
public class When_Mcp_Server_Is_Started
{
    private const string ConnectionString = "Server=localhost,1433;Database=TradingDemo;User ID=mcp_reader;Password=x";

    private static async Task<(int ExitCode, string StandardError, string StandardOutput)> StartAsync(
        params (string Name, string Value)[] settings)
    {
        using var workingDirectory = new XTests.DataGenerator.TempDirectory();
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "McpServer.dll");
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory.Path
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        foreach (var (name, value) in settings)
        {
            startInfo.Environment[name] = value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start McpServer.");
        var standardError = await process.StandardError.ReadToEndAsync();
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, standardError.Trim(), standardOutput.Trim());
    }

    [Fact]
    public async Task it_should_print_one_line_and_exit_when_no_bearer_tokens_are_set()
    {
        var (exitCode, standardError, standardOutput) = await StartAsync(("ConnectionStrings__Trading", ConnectionString));

        Assert.Equal(1, exitCode);
        Assert.StartsWith("No bearer tokens are set.", standardError);
        Assert.DoesNotContain(Environment.NewLine, standardError);
        Assert.Empty(standardOutput);
    }

    [Fact]
    public async Task it_should_print_one_line_and_exit_when_the_connection_string_is_missing()
    {
        var (exitCode, standardError, _) = await StartAsync(("Mcp__Tokens__inspector", "test-token-with-more-than-thirty-two-characters"));

        Assert.Equal(1, exitCode);
        Assert.Equal(
            "ConnectionStrings:Trading isn't set. See docs/02-run-server-and-inspector.md for the user-secrets commands.",
            standardError);
    }
}
