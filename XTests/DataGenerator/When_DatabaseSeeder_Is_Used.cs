using TradingDataGenerator.Constants;
using TradingDataGenerator.Database;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_DatabaseSeeder_Is_Used
{
    [Fact]
    public async Task it_should_not_seed_when_the_database_already_contains_instruments()
    {
        FakeDatabaseSeederSessionFactory sessionFactory;
        DatabaseSeeder seeder;
        int result;

        sessionFactory = new FakeDatabaseSeederSessionFactory(existingInstrumentCount: 1);
        seeder = new DatabaseSeeder(sessionFactory);

        result = await seeder.SeedAsync(new GeneratedData());

        Assert.Equal(ExitCodes.DatabaseAlreadyContainsData, result);
        Assert.Equal(0, sessionFactory.Session.InsertCount);
        Assert.Equal(0, sessionFactory.Session.ClearCount);
        Assert.True(sessionFactory.Session.Disposed);
    }

    [Fact]
    public async Task it_should_insert_and_commit_when_the_database_is_empty()
    {
        FakeDatabaseSeederSessionFactory sessionFactory;
        DatabaseSeeder seeder;
        GeneratedData generatedData;
        int result;

        sessionFactory = new FakeDatabaseSeederSessionFactory(existingInstrumentCount: 0);
        seeder = new DatabaseSeeder(sessionFactory);
        generatedData = new GeneratedData();

        result = await seeder.SeedAsync(generatedData);

        Assert.Equal(ExitCodes.Success, result);
        Assert.Same(generatedData, sessionFactory.Session.InsertedData);
        Assert.Equal(1, sessionFactory.Session.InsertCount);
        Assert.Equal(0, sessionFactory.Session.ClearCount);
        Assert.True(sessionFactory.Session.Disposed);
    }

    [Fact]
    public async Task it_should_clear_existing_data_before_seeding_when_reset_is_requested()
    {
        FakeDatabaseSeederSessionFactory sessionFactory;
        DatabaseSeeder seeder;
        int result;

        sessionFactory = new FakeDatabaseSeederSessionFactory(existingInstrumentCount: 1);
        seeder = new DatabaseSeeder(sessionFactory);

        result = await seeder.SeedAsyncWithReset(new GeneratedData());

        Assert.Equal(ExitCodes.Success, result);
        Assert.Equal(1, sessionFactory.Session.ClearCount);
        Assert.Equal(1, sessionFactory.Session.InsertCount);
    }

    private sealed class FakeDatabaseSeederSessionFactory : IDatabaseSeederSessionFactory
    {
        public FakeDatabaseSeederSessionFactory(int existingInstrumentCount)
        {
            Session = new FakeDatabaseSeederSession(existingInstrumentCount);
        }

        public FakeDatabaseSeederSession Session { get; }

        public Task<IDatabaseSeederSession> BeginTransactionAsync()
        {
            return Task.FromResult<IDatabaseSeederSession>(Session);
        }
    }

    private sealed class FakeDatabaseSeederSession(int existingInstrumentCount) : IDatabaseSeederSession
    {
        public int ClearCount { get; private set; }
        public int InsertCount { get; private set; }
        public bool Disposed { get; private set; }
        public GeneratedData? InsertedData { get; private set; }

        public Task<int> GetExistingInstrumentCountAsync()
        {
            return Task.FromResult(existingInstrumentCount);
        }

        public Task ClearDataAsync()
        {
            ClearCount++;
            return Task.CompletedTask;
        }

        public Task<int> InsertAndCommitAsync(GeneratedData generatedData)
        {
            InsertCount++;
            InsertedData = generatedData;
            return Task.FromResult(0);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
