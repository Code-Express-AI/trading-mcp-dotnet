using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using NSubstitute;
using TradingMcp.Audit;
using TradingMcp.Repository;
using TradingMcp.Tools;

namespace XTests.McpServer;

public class When_Audit_Service_Is_Run
{
    private readonly ITradingRepository _repository = Substitute.For<ITradingRepository>();

    private AuditService CreateAuditService(bool failClosed = false, bool auditWriteFails = false)
    {
        if (auditWriteFails)
        {
            _repository.WriteAuditAsync(default!, default!, default!, default, default, default, default)
                .ReturnsForAnyArgs(Task.FromException(new InvalidOperationException("Audit table unavailable.")));
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Audit:FailClosed"] = failClosed.ToString() })
            .Build();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        return new AuditService(_repository, httpContextAccessor, configuration, NullLogger<AuditService>.Instance);
    }

    private static Task<IReadOnlyList<string>> Rows(params string[] rows) => Task.FromResult<IReadOnlyList<string>>(rows);

    [Fact]
    public async Task it_should_return_the_rows_and_audit_a_successful_call()
    {
        var auditService = CreateAuditService();

        var rows = await auditService.RunAsync("search_instruments", new { text = "MAR" }, () => Rows("row-1", "row-2"));

        Assert.Equal(["row-1", "row-2"], rows);
        await _repository.Received(1).WriteAuditAsync(
            "anonymous", "search_instruments", """{"text":"MAR"}""", 2, Arg.Any<int>(), true, null);
    }

    [Fact]
    public async Task it_should_pass_argument_errors_to_the_model_unchanged_and_audit_them()
    {
        var auditService = CreateAuditService();

        var exception = await Assert.ThrowsAsync<ToolArgumentException>(() => auditService.RunAsync<string>(
            "get_top_movers", new { count = 50 }, () => throw new ToolArgumentException("count must be between 1 and 20.")));

        Assert.Equal("count must be between 1 and 20.", exception.Message);
        await _repository.Received(1).WriteAuditAsync(
            "anonymous", "get_top_movers", Arg.Any<string>(), null, Arg.Any<int>(), false, "Invalid arguments: count must be between 1 and 20.");
    }

    [Fact]
    public async Task it_should_hide_other_errors_behind_a_generic_message_that_discourages_retrying()
    {
        var auditService = CreateAuditService();

        var exception = await Assert.ThrowsAsync<McpException>(() => auditService.RunAsync<string>(
            "get_dataset_info", new { }, () => throw new InvalidOperationException("database details must not be exposed")));

        Assert.Equal(AuditService.GenericErrorMessage, exception.Message);
        Assert.DoesNotContain("database details", exception.Message);
        await _repository.Received(1).WriteAuditAsync(
            Arg.Any<string>(), "get_dataset_info", Arg.Any<string>(), null, Arg.Any<int>(), false,
            "InvalidOperationException: database details must not be exposed");
    }

    [Fact]
    public async Task it_should_preserve_cancellation()
    {
        var auditService = CreateAuditService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => auditService.RunAsync(
            "search_instruments", new { text = "MAR" }, () => Task.FromCanceled<IReadOnlyList<string>>(new CancellationToken(canceled: true))));
    }

    [Fact]
    public async Task it_should_still_return_the_rows_when_the_audit_write_fails_and_fail_closed_is_off()
    {
        var auditService = CreateAuditService(failClosed: false, auditWriteFails: true);

        var rows = await auditService.RunAsync("search_instruments", new { text = "MAR" }, () => Rows("row-1"));

        Assert.Equal(["row-1"], rows);
    }

    [Fact]
    public async Task it_should_refuse_to_return_rows_when_the_audit_write_fails_and_fail_closed_is_on()
    {
        var auditService = CreateAuditService(failClosed: true, auditWriteFails: true);

        var exception = await Assert.ThrowsAsync<McpException>(() =>
            auditService.RunAsync("search_instruments", new { text = "MAR" }, () => Rows("row-1")));

        Assert.Equal("Audit log unavailable; request refused.", exception.Message);
    }
}
