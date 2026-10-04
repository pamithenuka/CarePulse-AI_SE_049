using System.Net;
using CarePulse.Api.Services.Ai;

namespace CarePulse.Api.Tests;

public class GeminiTransportTests
{
    private sealed class Handler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public int Attempts { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.DoesNotContain("synthetic-secret", request.RequestUri!.ToString());
            Assert.Equal("synthetic-secret", request.Headers.GetValues("x-goog-api-key").Single());
            var response = new HttpResponseMessage(statuses[Math.Min(Attempts++, statuses.Length - 1)])
            { Content = new StringContent("synthetic provider body") };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.Zero);
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task RateLimit_RetriesOnce_ThenReturnsResult()
    {
        var handler = new Handler(HttpStatusCode.TooManyRequests, HttpStatusCode.OK);
        using var client = new HttpClient(handler);
        Assert.Equal("synthetic provider body", await GeminiTransport.SendAsync(client, "gemini-2.5-pro", "synthetic-secret", "{}"));
        Assert.Equal(2, handler.Attempts);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, 1)]
    [InlineData(HttpStatusCode.NotFound, 1)]
    [InlineData(HttpStatusCode.TooManyRequests, 2)]
    [InlineData(HttpStatusCode.ServiceUnavailable, 2)]
    public async Task Failure_IsBounded_AndDoesNotExposeProviderBody(HttpStatusCode status, int attempts)
    {
        var handler = new Handler(status);
        using var client = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<GeminiProviderException>(() => GeminiTransport.SendAsync(client, "gemini-2.5-pro", "synthetic-secret", "{}"));
        Assert.DoesNotContain("synthetic provider body", error.Message);
        Assert.Equal(status, error.StatusCode);
        Assert.Equal(attempts, handler.Attempts);
    }

    [Fact]
    public void OutboundMinimization_RemovesEmailAndLongIdentifiers_PreservesSymptomMeasurements()
    {
        var result = GeminiTransport.Minimize("Email sample@example.test phone +94 77 123 4567; temperature 38.2 and pulse 120.");
        Assert.DoesNotContain("sample@example.test", result);
        Assert.DoesNotContain("123 4567", result);
        Assert.Contains("38.2", result);
        Assert.Contains("120", result);
    }
}
