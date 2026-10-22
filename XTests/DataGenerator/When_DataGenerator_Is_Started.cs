using System.Diagnostics;

namespace XTests.DataGenerator;

public class When_DataGenerator_Is_Started
{
    [Fact]
    public async Task it_should_exit_with_invalid_options_when_there_is_no_connection_string_or_env_file()
    {
        using var emptyDirectory = new TempDirectory();
        var assemblyPath = Path.Combine(AppContext.BaseDirectory, "DataGenerator.dll");
        Assert.True(File.Exists(assemblyPath), $"Expected to find {assemblyPath}.");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = emptyDirectory.Path
        };
        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.Environment["TRADING_DB_CONNECTION"] = "";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start DataGenerator.");
        var standardError = await process.StandardError.ReadToEndAsync();
        var standardOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        Assert.Equal(1, process.ExitCode);
        Assert.StartsWith("No connection string, and no .env file found in", standardError);
        Assert.Empty(standardOutput);
    }
}
