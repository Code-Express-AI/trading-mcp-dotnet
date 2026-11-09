using System.Net;
using TradingMcp;

namespace XTests.McpServer;

// Runs the real pipeline, because only a real request proves the rate limiter skips the endpoint.
public class When_Health_Check_Is_Flooded
{
    private const string ConnectionString = "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;";
    private const string Token = "--Mcp:Tokens:inspector=test-token-with-more-than-thirty-two-characters";

    [Fact]
    public async Task it_should_keep_answering_past_the_anonymous_rate_limit()
    {
        await using var application = McpServerApplication.Build(
            ["--environment=Production", $"--contentRoot={AppContext.BaseDirectory}", "--urls=http://127.0.0.1:0", ConnectionString, Token]);
        await application.StartAsync();

        using var client = new HttpClient();
        var statusCodes = new List<HttpStatusCode>();
        for (var request = 0; request < 125; request++)
        {
            using var response = await client.GetAsync($"{application.Urls.First()}/health");
            statusCodes.Add(response.StatusCode);
        }

        await application.StopAsync();

        Assert.All(statusCodes, statusCode => Assert.Equal(HttpStatusCode.OK, statusCode));
    }
}
