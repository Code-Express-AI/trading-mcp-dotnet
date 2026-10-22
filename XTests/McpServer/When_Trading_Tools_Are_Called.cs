using NSubstitute;
using TradingMcp.Audit;
using TradingMcp.Model;
using TradingMcp.Repository;
using TradingMcp.Tools;

namespace XTests.McpServer;

public class When_Trading_Tools_Are_Called
{
    private readonly ITradingRepository _repository = Substitute.For<ITradingRepository>();
    private readonly RecordingAuditService _audit = new();
    private readonly TradingTools _tools;

    public When_Trading_Tools_Are_Called()
    {
        _tools = new TradingTools(_repository, _audit);
    }

    public static TheoryData<string, Func<TradingTools, Task>, string> InvalidCalls => new()
    {
        { "search_instruments", tools => tools.SearchInstruments(" "), "text must not be empty, for example 'MAR' or 'Pharma'." },
        { "get_price_history", tools => tools.GetPriceHistory(""), "ticker must not be empty. Use search_instruments to find one." },
        { "get_price_history", tools => tools.GetPriceHistory("MARP", new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1)), "from must be on or before to." },
        { "get_top_movers", tools => tools.GetTopMovers(count: 0), "count must be between 1 and 20." },
        { "get_top_movers", tools => tools.GetTopMovers(count: 21), "count must be between 1 and 20." },
        { "find_accounts", tools => tools.FindAccounts("a"), "name must be at least 2 characters, for example 'Smith'." },
        { "find_accounts", tools => tools.FindAccounts(" a "), "name must be at least 2 characters, for example 'Smith'." },
        { "find_accounts", tools => tools.FindAccounts(null!), "name must be at least 2 characters, for example 'Smith'." },
        { "list_trades", tools => tools.ListTrades(0), "accountId must be a positive number. Use find_accounts to look it up." },
        { "list_trades", tools => tools.ListTrades(1, new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1)), "from must be on or before to." },
        { "list_trades", tools => tools.ListTrades(1, page: 0), "page must be between 1 and 1000." },
        { "list_trades", tools => tools.ListTrades(1, page: 1001), "page must be between 1 and 1000." },
        { "get_positions", tools => tools.GetPositions(-1), "accountId must be a positive number. Use find_accounts to look it up." },
        { "get_portfolio_summary", tools => tools.GetPortfolioSummary(0), "accountId must be a positive number. Use find_accounts to look it up." }
    };

    [Theory]
    [MemberData(nameof(InvalidCalls))]
    public async Task it_should_reject_invalid_arguments_with_a_message_for_the_model_without_calling_the_database(
        string toolName, Func<TradingTools, Task> call, string expectedMessage)
    {
        var exception = await Assert.ThrowsAsync<ToolArgumentException>(() => call(_tools));

        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(toolName, _audit.ToolName);
        Assert.Empty(_repository.ReceivedCalls());
    }

    [Fact]
    public async Task it_should_return_the_repository_results_through_the_audit_wrapper()
    {
        IReadOnlyList<DatasetInfo> expected = [new DatasetInfo(DateTime.UnixEpoch, DateTime.UnixEpoch, 1, 2, 3)];
        _repository.GetDatasetInfo(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _tools.GetDatasetInfo();

        Assert.Same(expected, result);
        Assert.Equal("get_dataset_info", _audit.ToolName);
    }

    [Fact]
    public async Task it_should_pass_every_argument_and_the_cancellation_token_to_the_repository()
    {
        using var cancellation = new CancellationTokenSource();
        var from = new DateOnly(2026, 1, 1);
        var to = new DateOnly(2026, 6, 30);

        await _tools.ListTrades(45, from, to, "MARP", 3, cancellation.Token);

        await _repository.Received(1).ListTrades(45, from, to, "MARP", 3, cancellation.Token);
        Assert.Equal("list_trades", _audit.ToolName);
        Assert.Equal(new { accountId = 45, from = (DateOnly?)from, to = (DateOnly?)to, ticker = (string?)"MARP", page = 3 }.ToString(),
            _audit.Arguments?.ToString());
    }

    [Fact]
    public async Task it_should_accept_the_boundary_values()
    {
        await _tools.GetTopMovers(count: 1);
        await _tools.GetTopMovers(count: 20);
        await _tools.FindAccounts("Al");
        await _tools.ListTrades(1, page: 1000);

        Assert.Equal(4, _repository.ReceivedCalls().Count());
    }

    private sealed class RecordingAuditService : IAuditService
    {
        public string? ToolName { get; private set; }
        public object? Arguments { get; private set; }

        public Task<IReadOnlyList<T>> RunAsync<T>(string tool, object args, Func<Task<IReadOnlyList<T>>> action)
        {
            ToolName = tool;
            Arguments = args;
            return action();
        }
    }
}
