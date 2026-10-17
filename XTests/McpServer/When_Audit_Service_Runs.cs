using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using NSubstitute;
using TradingMcp.Audit;
using TradingMcp.Repository;

namespace XTests.McpServer;

public class When_Audit_Service_Runs
{
    [Fact]
    public async Task it_should_return_results_when_audit_writing_fails_and_fail_closed_is_disabled()
    {
        IReadOnlyList<string> rows;
        AuditService auditService;

        auditService = CreateAuditService(failClosed: false);
        rows = await auditService.RunAsync(
            "search_instruments",
            new { text = "example" },
            () => Task.FromResult<IReadOnlyList<string>>(["row-1", "row-2"]));

        Assert.Equal(["row-1", "row-2"], rows);
    }

    [Fact]
    public async Task it_should_reject_results_when_audit_writing_fails_and_fail_closed_is_enabled()
    {
        AuditService auditService;
        McpException exception;

        auditService = CreateAuditService(failClosed: true);

        exception = await Assert.ThrowsAsync<McpException>(() => auditService.RunAsync(
            "search_instruments",
            new { text = "example" },
            () => Task.FromResult<IReadOnlyList<string>>(["row-1"])));

        Assert.Equal("Audit log unavailable; request refused.", exception.Message);
    }

    [Fact]
    public async Task it_should_return_a_generic_mcp_error_when_a_tool_action_fails()
    {
        AuditService auditService;
        McpException exception;

        auditService = CreateAuditService(failClosed: false);

        exception = await Assert.ThrowsAsync<McpException>(() => auditService.RunAsync(
            "search_instruments",
            new { text = "example" },
            () => Task.FromException<IReadOnlyList<string>>(
                new InvalidOperationException("database details must not be exposed"))));

        Assert.Equal("The request could not be completed. Check the arguments and try again.", exception.Message);
    }

    [Fact]
    public async Task it_should_preserve_cancellation_from_a_tool_action()
    {
        AuditService auditService;

        auditService = CreateAuditService(failClosed: false);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => auditService.RunAsync(
            "search_instruments",
            new { text = "example" },
            () => Task.FromCanceled<IReadOnlyList<string>>(new CancellationToken(canceled: true))));
    }

    private static AuditService CreateAuditService(bool failClosed)
    {
        ITradingRepository repository;
        IHttpContextAccessor httpContextAccessor;
        IConfiguration configuration;

        repository = Substitute.For<ITradingRepository>();
        repository.WriteAuditAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<string?>())
            .Returns(_ => throw new InvalidOperationException("Audit repository intentionally unavailable in this unit test."));
        httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Audit:FailClosed"] = failClosed.ToString()
            })
            .Build();

        return new AuditService(
            repository,
            httpContextAccessor,
            configuration,
            NullLogger<AuditService>.Instance);
    }
}
