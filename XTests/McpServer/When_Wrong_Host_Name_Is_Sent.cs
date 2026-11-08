using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingMcp;

namespace XTests.McpServer;

// Runs the real pipeline on a random port. Host filtering rejects a wrong Host header before any of our
// middleware runs, so only a real request proves the rejection is logged, and that appsettings.json doesn't hide it.
public class When_Wrong_Host_Name_Is_Sent
{
    private const string ConnectionString = "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;";
    private const string Token = "--Mcp:Tokens:inspector=test-token-with-more-than-thirty-two-characters";

    [Fact]
    public async Task it_should_refuse_it_and_log_the_host_name()
    {
        await using var application = McpServerApplication.Build(
            ["--environment=Production", $"--contentRoot={AppContext.BaseDirectory}", "--urls=http://127.0.0.1:0", ConnectionString, Token]);
        var log = new RecordingLoggerProvider();
        application.Services.GetRequiredService<ILoggerFactory>().AddProvider(log);
        await application.StartAsync();

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{application.Urls.First()}/health");
        request.Headers.Host = "evil.example";
        using var response = await client.SendAsync(request);
        await application.StopAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(log.Entries, entry => entry.Message.Contains("'evil.example'"));
    }
}
