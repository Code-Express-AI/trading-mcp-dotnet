using TradingDataGenerator;
using TradingDataGenerator.Configuration;
using TradingDataGenerator.Constants;
using TradingDataGenerator.Database;
using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_DataLoader_Is_Used
{
    [Fact]
    public async Task it_should_return_invalid_configuration_without_preparing_data_when_options_are_invalid()
    {
        DataLoader dataLoader;
        StringWriter standardError;
        TextWriter originalStandardError;
        int result;

        dataLoader = new DataLoader();
        standardError = new StringWriter();
        originalStandardError = Console.Error;

        try
        {
            Console.SetError(standardError);
            result = await dataLoader.LoadAsync(["--instruments=0"], "connection-string");
        }
        finally
        {
            Console.SetError(originalStandardError);
        }

        Assert.Equal(ExitCodes.InvalidConfiguration, result);
        Assert.Equal("--instruments must be greater than zero.", standardError.ToString().Trim());
    }

    [Fact]
    public async Task it_should_prepare_data_and_seed_without_reset_when_reset_is_not_requested()
    {
        FakeDataPreparation dataPreparation;
        FakeDatabaseSeeder databaseSeeder;
        DataLoader dataLoader;
        int result;

        dataPreparation = new FakeDataPreparation();
        databaseSeeder = new FakeDatabaseSeeder(seedResultCode: 17);
        dataLoader = new DataLoader(dataPreparation, connectionString =>
        {
            Assert.Equal("connection-string", connectionString);
            return databaseSeeder;
        });

        result = await dataLoader.LoadAsync(["--seed=7", "--clients=5"], "connection-string");

        Assert.Equal(17, result);
        Assert.Equal(1, dataPreparation.CallCount);
        Assert.Equal(7, dataPreparation.Options?.Seed);
        Assert.Equal(5, dataPreparation.Options?.ClientCount);
        Assert.False(dataPreparation.Options?.Reset);
        Assert.Same(dataPreparation.GeneratedData, databaseSeeder.SeededData);
        Assert.Equal(1, databaseSeeder.SeedCallCount);
        Assert.Equal(0, databaseSeeder.SeedWithResetCallCount);
    }

    [Fact]
    public async Task it_should_prepare_data_and_seed_with_reset_when_reset_is_requested()
    {
        FakeDataPreparation dataPreparation;
        FakeDatabaseSeeder databaseSeeder;
        DataLoader dataLoader;
        int result;

        dataPreparation = new FakeDataPreparation();
        databaseSeeder = new FakeDatabaseSeeder(seedResultCode: 23);
        dataLoader = new DataLoader(dataPreparation, _ => databaseSeeder);

        result = await dataLoader.LoadAsync(["--reset"], "connection-string");

        Assert.Equal(23, result);
        Assert.Equal(1, dataPreparation.CallCount);
        Assert.True(dataPreparation.Options?.Reset);
        Assert.Same(dataPreparation.GeneratedData, databaseSeeder.SeededData);
        Assert.Equal(0, databaseSeeder.SeedCallCount);
        Assert.Equal(1, databaseSeeder.SeedWithResetCallCount);
    }

    private sealed class FakeDataPreparation : IDataPreparation
    {
        public GeneratedData GeneratedData { get; } = new();
        public IGeneratorOptions? Options { get; private set; }
        public int CallCount { get; private set; }

        public GeneratedData PrepareGeneratedData(IGeneratorOptions generatorOptions)
        {
            CallCount++;
            Options = generatorOptions;
            return GeneratedData;
        }
    }

    private sealed class FakeDatabaseSeeder(int seedResultCode = 0) : IDatabaseSeeder
    {
        public GeneratedData? SeededData { get; private set; }
        public int SeedCallCount { get; private set; }
        public int SeedWithResetCallCount { get; private set; }

        public Task<int> SeedAsync(GeneratedData generatedData)
        {
            SeedCallCount++;
            SeededData = generatedData;
            return Task.FromResult(seedResultCode);
        }

        public Task<int> SeedAsyncWithReset(GeneratedData generatedData)
        {
            SeedWithResetCallCount++;
            SeededData = generatedData;
            return Task.FromResult(seedResultCode);
        }
    }
}
