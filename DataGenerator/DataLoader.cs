using Microsoft.Data.SqlClient;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;

namespace TradingDataGenerator;

internal sealed class DataLoader(Func<string, IDatabaseSeeder> createSeeder, TextWriter output, TextWriter errorOutput)
{
    public async Task<int> RunAsync(string[] args, string? environmentConnectionString, string currentDirectory)
    {
        if (GeneratorOptions.IsHelpRequested(args))
        {
            await output.WriteLineAsync(GeneratorOptions.HelpText);
            return ExitCodes.Success;
        }

        GeneratorOptions options;
        try
        {
            options = GeneratorOptions.Parse(args, environmentConnectionString, DateTime.UtcNow.Date, currentDirectory);
        }
        catch (InvalidOptionsException exception)
        {
            await errorOutput.WriteLineAsync(exception.Message);
            return ExitCodes.InvalidOptions;
        }

        try
        {
            return await LoadAsync(options);
        }
        catch (SqlException exception)
        {
            await errorOutput.WriteLineAsync(DatabaseErrorMessages.Describe(exception, options.ConnectionString));
            return ExitCodes.DatabaseError;
        }
    }

    private async Task<int> LoadAsync(GeneratorOptions options)
    {
        var seeder = createSeeder(options.ConnectionString);

        // Checked before generating, so a bad connection fails in seconds rather than after the data is built.
        var databaseContainsData = await seeder.ContainsDataAsync();
        if (databaseContainsData && !options.Reset)
        {
            await errorOutput.WriteLineAsync(
                "The database already contains data. Run again with --reset to delete it and load new data.");
            return ExitCodes.DatabaseAlreadyContainsData;
        }

        var data = SyntheticData.Generate(options);
        await seeder.SeedAsync(data, clearExistingData: databaseContainsData);
        return ExitCodes.Success;
    }
}
