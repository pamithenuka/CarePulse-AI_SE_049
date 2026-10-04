using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace CarePulse.Api.Services.Ai;

public sealed class GeminiProviderException(HttpStatusCode statusCode) : InvalidOperationException(statusCode switch
{
    HttpStatusCode.NotFound => "The configured AI model is unavailable. Update AI:GeminiModel to a model available to your project.",
    HttpStatusCode.TooManyRequests => "Gemini quota or rate limit reached. Check the project's quota in Google AI Studio before retrying.",
    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "Gemini access was denied. Check the locally configured API key and project permissions.",
    HttpStatusCode.BadRequest => "Gemini rejected the request. Check the API key, model configuration and request format.",
    _ => $"AI provider returned HTTP {(int)statusCode}. Please retry later or request manual review."
})
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}

public static class GeminiTransport
{
    // Conservative minimization, not a guarantee of anonymization of arbitrary prose.
    public static string Minimize(string text)
    {
        text = Regex.Replace(text, @"[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}", "[email]", RegexOptions.None, TimeSpan.FromSeconds(1));
        return Regex.Replace(text, @"(?<!\d)(?:\+?\d[\d ()-]{8,}\d)(?!\d)", "[identifier]", RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    public static async Task<string> SendAsync(HttpClient client, string model, string key, string json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Contains("YOUR_")) throw new InvalidOperationException("Configure AI__GeminiApiKey locally.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        for (var attempt = 0; attempt < 2; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent");
            request.Headers.Add("x-goog-api-key", key);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, timeout.Token);
            if (attempt == 0 && (response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500))
            {
                var wait = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                await Task.Delay(wait > TimeSpan.FromSeconds(3) ? TimeSpan.FromSeconds(3) : wait, timeout.Token);
                continue;
            }
            if (!response.IsSuccessStatusCode) throw new GeminiProviderException(response.StatusCode);
            return await response.Content.ReadAsStringAsync(timeout.Token);
        }
        throw new InvalidOperationException("AI service unavailable.");
    }
}
