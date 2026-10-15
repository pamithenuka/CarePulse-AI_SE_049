using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarePulse.Api.Services.Ai;

public class GeminiAiPlannerClient : IAiPlannerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiAiPlannerClient> _logger;
    private readonly string _apiKey;

    public string ModelName { get; }

    public GeminiAiPlannerClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAiPlannerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        ModelName = configuration["AI:GeminiModel"] ?? "gemini-3.5-flash-lite";
        _apiKey = configuration["AI:GeminiApiKey"] ?? "";
    }

    public async Task<AiPlanCompletionResult> GeneratePlanAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Contains("YOUR_"))
                return new AiPlanCompletionResult(false, null, "Gemini API Key is not configured.");

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = GeminiTransport.Minimize(userPrompt) } }
                    }
                },
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                generation_config = new
                {
                    response_mime_type = "application/json"
                }
            };

            var body = await GeminiTransport.SendAsync(_httpClient, ModelName, _apiKey, JsonSerializer.Serialize(requestPayload), cancellationToken);
            var responseJson = JsonSerializer.Deserialize<JsonElement>(body);
            var candidates = responseJson.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
                return new AiPlanCompletionResult(false, null, "No candidates returned from Gemini.");

            var contentText = string.Concat(candidates[0]
                .GetProperty("content")
                .GetProperty("parts").EnumerateArray()
                .Where(part => !(part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True))
                .Where(part => part.TryGetProperty("text", out _))
                .Select(part => part.GetProperty("text").GetString()));

            if (string.IsNullOrWhiteSpace(contentText))
                return new AiPlanCompletionResult(false, null, "Empty response from Gemini.");

            // Remove markdown code blocks if present
            if (contentText.StartsWith("```json"))
            {
                contentText = contentText.Replace("```json", "").Replace("```", "").Trim();
            }

            return new AiPlanCompletionResult(true, contentText, null);
        }
        catch (GeminiProviderException ex)
        {
            _logger.LogWarning("Gemini rejected planning request with HTTP {StatusCode}.", (int)ex.StatusCode);
            return new AiPlanCompletionResult(false, null, ex.Message);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Gemini request timed out.");
            return new AiPlanCompletionResult(false, null, "The AI planning service timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach Gemini.");
            return new AiPlanCompletionResult(false, null, "The AI planning service is unavailable.");
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            _logger.LogWarning(ex, "Gemini returned unparsable JSON.");
            return new AiPlanCompletionResult(false, null, "The AI planning service returned an unreadable response.");
        }
    }
}
