using TradingDataGenerator.Configuration;
using TradingDataGenerator.Constants;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace TradingDataGenerator;

internal sealed class DataLoader : IDataLoader
{
    private readonly IDataPreparation _dataPreparation;
    private readonly Func<string, IDatabaseSeeder> _databaseSeederFactory;

    internal DataLoader()
        : this(
            new DataPreparation(new SyntheticDataGenerator()),
            databaseConnectionString => new DatabaseSeeder(databaseConnectionString))
    {
    }

    internal DataLoader(
        IDataPreparation dataPreparation,
        Func<string, IDatabaseSeeder> databaseSeederFactory)
    {
        _dataPreparation = dataPreparation;
        _databaseSeederFactory = databaseSeederFactory;
    }

    public async Task<int> LoadAsync(string[] commandLineArgs, string databaseConnectionString)
    {
        GeneratorOptionsParseResult generatorOptionsParseResult;
        GeneratorOptions generatorOptions;
        GeneratedData generatedData;
        IDatabaseSeeder databaseSeeder;
        int seedResultCode;

        generatorOptionsParseResult = GeneratorOptions.Parse(commandLineArgs);
        if (generatorOptionsParseResult.Options is not { } parsedGeneratorOptions)
        {
            Console.Error.WriteLine(generatorOptionsParseResult.ErrorMessage);
            return ExitCodes.InvalidConfiguration;
        }

        generatorOptions = parsedGeneratorOptions;
        generatedData = _dataPreparation.PrepareGeneratedData(generatorOptions);
        databaseSeeder = _databaseSeederFactory(databaseConnectionString);
        if (generatorOptions.Reset)
        {
            seedResultCode = await databaseSeeder.SeedAsyncWithReset(generatedData);
        }
        else
        {
            seedResultCode = await databaseSeeder.SeedAsync(generatedData);
        }
        return seedResultCode;
    }
}
