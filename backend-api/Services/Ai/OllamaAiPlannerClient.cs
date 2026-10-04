using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarePulse.Api.Services.Ai;

/// <summary>
/// Calls a local Ollama server (see appsettings "Ollama" section). Uses Ollama's
/// "format": "json" mode, which grammar-constrains the model's output to valid
/// JSON — the first line of defence against prompt injection in the objective
/// text, independent of the deterministic schema validation applied afterwards
/// in AgentPlannerService.
/// </summary>
public class OllamaAiPlannerClient : IAiPlannerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaAiPlannerClient> _logger;

    public string ModelName { get; }

    public OllamaAiPlannerClient(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaAiPlannerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        ModelName = configuration["Ollama:Model"] ?? "llama3.2";
    }

    public async Task<AiPlanCompletionResult> GeneratePlanAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaGenerateRequest
            {
                Model = ModelName,
                System = systemPrompt,
                Prompt = userPrompt,
                Format = "json",
                Stream = false
            };

            var response = await _httpClient.PostAsJsonAsync("api/generate", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Ollama returned {StatusCode}: {Body}", response.StatusCode, body);
                return new AiPlanCompletionResult(false, null, $"Ollama returned HTTP {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
            if (payload?.Response is null)
            {
                return new AiPlanCompletionResult(false, null, "Ollama response was empty.");
            }

            return new AiPlanCompletionResult(true, payload.Response, null);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Ollama request timed out.");
            return new AiPlanCompletionResult(false, null, "The AI planning service timed out.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not reach Ollama.");
            return new AiPlanCompletionResult(false, null, "The AI planning service is unavailable. Is Ollama running?");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Ollama returned unparsable JSON.");
            return new AiPlanCompletionResult(false, null, "The AI planning service returned an unreadable response.");
        }
    }

    private class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("system")]
        public string System { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("format")]
        public string Format { get; set; } = "json";

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
