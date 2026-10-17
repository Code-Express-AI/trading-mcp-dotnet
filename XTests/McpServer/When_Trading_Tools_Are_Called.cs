using NSubstitute;
using TradingMcp.Audit;
using TradingMcp.Model;
using TradingMcp.Repository;
using TradingMcp.Tools;

namespace XTests.McpServer;

public class When_Trading_Tools_Are_Called
{
    [Fact]
    public async Task get_dataset_info_should_audit_and_return_repository_results()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        IReadOnlyList<DatasetInfo> expected = [new DatasetInfo(DateTime.UnixEpoch, DateTime.UnixEpoch, 1, 2, 3)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.GetDatasetInfo(cancellationSource.Token), expected);

        var result = await tools.GetDatasetInfo(cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).GetDatasetInfo(cancellationSource.Token);
        Assert.Equal("get_dataset_info", audit.ToolName);
    }

    [Fact]
    public async Task search_instruments_should_forward_search_text_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        IReadOnlyList<Instrument> expected = [new Instrument("EXMP", "Example", "Technology", "USD")];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.SearchInstruments("EX", cancellationSource.Token), expected);

        var result = await tools.SearchInstruments("EX", cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).SearchInstruments("EX", cancellationSource.Token);
        Assert.Equal("search_instruments", audit.ToolName);
    }

    [Fact]
    public async Task get_price_history_should_forward_ticker_dates_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 31);
        IReadOnlyList<Price> expected = [new Price(from.ToDateTime(TimeOnly.MinValue), 1, 2, 0, 1, 10)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.GetPriceHistory("EXMP", from, to, cancellationSource.Token), expected);

        var result = await tools.GetPriceHistory("EXMP", from, to, cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).GetPriceHistory("EXMP", from, to, cancellationSource.Token);
        Assert.Equal("get_price_history", audit.ToolName);
    }

    [Fact]
    public async Task get_top_movers_should_forward_date_count_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        var date = new DateOnly(2025, 1, 31);
        IReadOnlyList<Mover> expected = [new Mover("EXMP", "Example", date.ToDateTime(TimeOnly.MinValue), 2, 1, 100)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.GetTopMovers(date, 10, cancellationSource.Token), expected);

        var result = await tools.GetTopMovers(date, 10, cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).GetTopMovers(date, 10, cancellationSource.Token);
        Assert.Equal("get_top_movers", audit.ToolName);
    }

    [Fact]
    public async Task find_accounts_should_forward_name_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        IReadOnlyList<Account> expected =
            [new Account(1, "****1234", "Avery Example", "GB", "Balanced", "Brokerage", "GBP", DateTime.UnixEpoch)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.FindAccounts("Avery", cancellationSource.Token), expected);

        var result = await tools.FindAccounts("Avery", cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).FindAccounts("Avery", cancellationSource.Token);
        Assert.Equal("find_accounts", audit.ToolName);
    }

    [Fact]
    public async Task list_trades_should_forward_all_filters_paging_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 1, 31);
        IReadOnlyList<Trade> expected = [new Trade(12, DateTime.UnixEpoch, "EXMP", "BUY", 2, 10, 1, "SETTLED")];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(
            () => repository.ListTrades(45, from, to, "EXMP", 3, cancellationSource.Token),
            expected);

        var result = await tools.ListTrades(45, from, to, "EXMP", 3, cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).ListTrades(45, from, to, "EXMP", 3, cancellationSource.Token);
        Assert.Equal("list_trades", audit.ToolName);
    }

    [Fact]
    public async Task get_positions_should_forward_account_id_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        IReadOnlyList<Position> expected =
            [new Position("EXMP", "Example", "Technology", "USD", 1, 10, DateTime.UnixEpoch, 12, 12, 2, 20)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.GetPositions(45, cancellationSource.Token), expected);

        var result = await tools.GetPositions(45, cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).GetPositions(45, cancellationSource.Token);
        Assert.Equal("get_positions", audit.ToolName);
    }

    [Fact]
    public async Task get_portfolio_summary_should_forward_account_id_and_cancellation()
    {
        var repository = Substitute.For<ITradingRepository>();
        var audit = CreateTransparentAudit();
        var tools = new TradingTools(repository, audit);
        IReadOnlyList<SectorSummary> expected = [new SectorSummary("Technology", "USD", 1, 12, 2)];
        using var cancellationSource = new CancellationTokenSource();
        ConfigureRepositoryCall(() => repository.GetPortfolioSummary(45, cancellationSource.Token), expected);

        var result = await tools.GetPortfolioSummary(45, cancellationSource.Token);

        Assert.Same(expected, result);
        await repository.Received(1).GetPortfolioSummary(45, cancellationSource.Token);
        Assert.Equal("get_portfolio_summary", audit.ToolName);
    }

    private static RecordingAuditService CreateTransparentAudit()
    {
        return new RecordingAuditService();
    }

    private static void ConfigureRepositoryCall<T>(
        Func<Task<IReadOnlyList<T>>> repositoryCall,
        IReadOnlyList<T> result)
    {
        repositoryCall().Returns(Task.FromResult(result));
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public string? ToolName { get; private set; }
        public object? Arguments { get; private set; }

        public Task<IReadOnlyList<T>> RunAsync<T>(
            string tool,
            object args,
            Func<Task<IReadOnlyList<T>>> action)
        {
            ToolName = tool;
            Arguments = args;
            return action();
        }
    }
}
