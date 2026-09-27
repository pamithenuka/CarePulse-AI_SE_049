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
        ModelName = configuration["AI:GeminiModel"] ?? "gemini-1.5-flash";
        _apiKey = configuration["AI:GeminiApiKey"] ?? "";
    }

    public async Task<AiPlanCompletionResult> GeneratePlanAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return new AiPlanCompletionResult(false, null, "Gemini API Key is not configured.");

            var requestPayload = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
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

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{ModelName}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestPayload, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Gemini returned {StatusCode}: {Body}", response.StatusCode, body);
                return new AiPlanCompletionResult(false, null, $"Gemini returned HTTP {(int)response.StatusCode}.");
            }

            var responseJson = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var candidates = responseJson.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
                return new AiPlanCompletionResult(false, null, "No candidates returned from Gemini.");

            var contentText = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(contentText))
                return new AiPlanCompletionResult(false, null, "Empty response from Gemini.");

            // Remove markdown code blocks if present
            if (contentText.StartsWith("```json"))
            {
                contentText = contentText.Replace("```json", "").Replace("```", "").Trim();
            }

            return new AiPlanCompletionResult(true, contentText, null);
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
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Gemini returned unparsable JSON.");
            return new AiPlanCompletionResult(false, null, "The AI planning service returned an unreadable response.");
        }
    }
}
