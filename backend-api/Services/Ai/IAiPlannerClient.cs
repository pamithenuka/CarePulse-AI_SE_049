namespace CarePulse.Api.Services.Ai;

public record AiPlanCompletionResult(bool Succeeded, string? RawJson, string? Error);

/// <summary>
/// Abstraction over the LLM used by Agent 1 (Planner/Coordinator). Swap
/// OllamaAiPlannerClient for a different provider in Program.cs — no caller
/// code changes needed. Kept separate from INotificationService's pattern
/// because the contract (prompt in, structured-plan-JSON out) is different.
/// </summary>
public interface IAiPlannerClient
{
    string ModelName { get; }

    Task<AiPlanCompletionResult> GeneratePlanAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);
}
