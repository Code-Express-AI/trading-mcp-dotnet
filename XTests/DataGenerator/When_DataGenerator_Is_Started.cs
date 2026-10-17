using System.Diagnostics;

namespace XTests.DataGenerator;

public class When_DataGenerator_Is_Started
{
    [Theory]
    [InlineData(null, "")]
    [InlineData(" ", "Server=example;Database=example;")]
    public async Task it_should_exit_with_invalid_configuration_when_no_usable_connection_string_is_available(
        string? connectionStringArgument,
        string environmentConnectionString)
    {
        string dataGeneratorAssemblyPath;
        ProcessStartInfo processStartInfo;
        Process process;
        string standardError;
        string standardOutput;
        int exitCode;

        dataGeneratorAssemblyPath = Path.Combine(AppContext.BaseDirectory, "DataGenerator.dll");
        Assert.True(File.Exists(dataGeneratorAssemblyPath), $"Expected to find {dataGeneratorAssemblyPath}.");

        processStartInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        processStartInfo.ArgumentList.Add(dataGeneratorAssemblyPath);
        if (connectionStringArgument is not null)
        {
            processStartInfo.ArgumentList.Add(connectionStringArgument);
        }

        processStartInfo.Environment["TRADING_DB_CONNECTION"] = environmentConnectionString;

        process = Process.Start(processStartInfo)
            ?? throw new InvalidOperationException("Failed to start the DataGenerator process.");
        using (process)
        {
            standardError = await process.StandardError.ReadToEndAsync();
            standardOutput = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            exitCode = process.ExitCode;
        }

        Assert.Equal(1, exitCode);
        Assert.Equal(
            "No connection string. Pass it as the first argument or set TRADING_DB_CONNECTION.",
            standardError.Trim());
        Assert.Empty(standardOutput);
    }
}
