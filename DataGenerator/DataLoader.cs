using TradingDataGenerator.Configuration;
using TradingDataGenerator.Constants;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace TradingDataGenerator;

internal sealed class DataLoader : IDataLoader
{
    public async Task<int> LoadAsync(string[] commandLineArgs, string databaseConnectionString)
    {
        GeneratorOptionsParseResult generatorOptionsParseResult;
        GeneratorOptions generatorOptions;
        IDataPreparation dataPreparation;
        ISyntheticDataGenerator syntheticDataGenerator;
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
        syntheticDataGenerator = new SyntheticDataGenerator();
        dataPreparation = new DataPreparation(syntheticDataGenerator);
        generatedData = dataPreparation.PrepareGeneratedData(generatorOptions);
        databaseSeeder = new DatabaseSeeder(databaseConnectionString);
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
