using System.Net;
using CarePulse.Api.Services.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarePulse.Api.Tests;

public class GeminiAiPlannerClientTests
{
    private sealed class Handler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(body) };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return Task.FromResult(response);
        }
    }

    private static GeminiAiPlannerClient Create(HttpClient http) => new(http,
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:GeminiApiKey"] = "synthetic-secret",
            ["AI:GeminiModel"] = "test-model"
        }).Build(), NullLogger<GeminiAiPlannerClient>.Instance);

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "model is unavailable")]
    [InlineData(HttpStatusCode.TooManyRequests, "quota or rate limit")]
    [InlineData(HttpStatusCode.Forbidden, "access was denied")]
    public async Task ProviderFailure_IsExplainedWithoutExposingProviderBody(HttpStatusCode status, string expected)
    {
        using var http = new HttpClient(new Handler(status, "private provider details"));
        var result = await Create(http).GeneratePlanAsync("system", "synthetic objective");
        Assert.False(result.Succeeded);
        Assert.Contains(expected, result.Error);
        Assert.DoesNotContain("private provider details", result.Error);
        Assert.DoesNotContain("unreadable", result.Error);
    }

    [Fact]
    public async Task MalformedEnvelope_IsReportedAsUnreadable()
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.OK, "not json"));
        var result = await Create(http).GeneratePlanAsync("system", "objective");
        Assert.False(result.Succeeded);
        Assert.Contains("unreadable", result.Error);
    }

    [Fact]
    public async Task TextParts_AreCombinedWithoutThoughtContent()
    {
        using var http = new HttpClient(new Handler(HttpStatusCode.OK,
            """{"candidates":[{"content":{"parts":[{"thought":true,"text":"internal reasoning"},{"text":"{\"steps\":"},{"text":"[]}"}]}}]}"""));
        var result = await Create(http).GeneratePlanAsync("system", "objective");
        Assert.True(result.Succeeded);
        Assert.Equal("""{"steps":[]}""", result.RawJson);
    }
}
