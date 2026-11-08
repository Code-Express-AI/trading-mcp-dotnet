using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingMcp;

namespace XTests.McpServer;

// Runs the real pipeline on a random port. Only a real request proves the origin check is in the pipeline,
// and that it runs before authentication: a valid token must not get a foreign web page past it.
public class When_Foreign_Origin_Is_Sent
{
    private const string TokenValue = "test-token-with-more-than-thirty-two-characters";
    private const string ConnectionString = "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;";
    private const string Token = $"--Mcp:Tokens:inspector={TokenValue}";

    [Fact]
    public async Task it_should_refuse_it_even_with_a_valid_token_and_log_the_origin()
    {
        await using var application = McpServerApplication.Build(
            ["--environment=Production", $"--contentRoot={AppContext.BaseDirectory}", "--urls=http://127.0.0.1:0", ConnectionString, Token]);
        var log = new RecordingLoggerProvider();
        application.Services.GetRequiredService<ILoggerFactory>().AddProvider(log);
        await application.StartAsync();

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{application.Urls.First()}/mcp");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenValue);
        using var response = await client.SendAsync(request);
        await application.StopAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(log.Entries, entry => entry.Message.Contains("'https://evil.example'"));
    }
}
