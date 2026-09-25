using CarePulse.Api.Services.Ai;

namespace CarePulse.Api.Tests.Fakes;

/// <summary>
/// Stands in for OllamaAiPlannerClient in tests so AgentPlannerService's
/// validation/persistence logic can be tested deterministically, without a
/// real Ollama server or non-deterministic model output.
/// </summary>
public class FakeAiPlannerClient : IAiPlannerClient
{
    public string ModelName { get; set; } = "fake-model";
    public string? NextRawJson { get; set; }
    public string? NextError { get; set; }
    public List<string> ReceivedUserPrompts { get; } = new();

    public Task<AiPlanCompletionResult> GeneratePlanAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
    {
        ReceivedUserPrompts.Add(userPrompt);

        if (NextError is not null)
        {
            return Task.FromResult(new AiPlanCompletionResult(false, null, NextError));
        }

        return Task.FromResult(new AiPlanCompletionResult(true, NextRawJson, null));
    }
}
