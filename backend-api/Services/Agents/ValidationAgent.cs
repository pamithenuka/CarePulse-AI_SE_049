using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.Configuration;

namespace CarePulse.Api.Services.Agents;

public class ValidationAgentRequest
{
    public Guid TriageId { get; set; }
    public double SeverityScore { get; set; }
    public Guid? RecommendedNurseId { get; set; }
    public int EtaMinutes { get; set; }
}

public class ValidationAgentResponse
{
    public bool RequiresHumanApproval { get; set; } = true;
    public List<string> FlaggedRules { get; set; } = new();
    public string VerdictReason { get; set; } = string.Empty;
}

public interface IValidationAgent
{
    Task<ValidationAgentResponse> EvaluateDispatchSafetyAsync(ValidationAgentRequest request);
}

public class SafetyThresholdsPlugin
{
    private readonly CarePulseDbContext _context;

    public SafetyThresholdsPlugin(CarePulseDbContext context)
    {
        // Using the context in a read-only manner (Requirement)
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    [KernelFunction("Tool_GetSystemSafetyThresholds")]
    [Description("Retrieves the system safety thresholds for clinical severity bounds and maximum ETA.")]
    public async Task<string> GetSystemSafetyThresholdsAsync()
    {
        // Verifying read-only DB access capability
        var canConnect = await _context.Database.CanConnectAsync();

        var thresholds = new
        {
            MaxAllowedSeverityScore = 8.5,
            MaxAllowedEtaMinutes = 30,
            MandatoryDoctorApprovalThreshold = 5.0,
            DbConnected = canConnect
        };

        return JsonSerializer.Serialize(thresholds);
    }
}

public class ValidationAgent : IValidationAgent
{
    private readonly Kernel _kernel;
    private readonly ILogger<ValidationAgent> _logger;
    private readonly IChatCompletionService? _chatService;
    private readonly bool _hasAiConfigured;

    public ValidationAgent(CarePulseDbContext context, IConfiguration config, ILogger<ValidationAgent> logger)
    {
        _logger = logger;
        var builder = Kernel.CreateBuilder();
        builder.Plugins.AddFromObject(new SafetyThresholdsPlugin(context), "SafetyThresholds");

        var apiKey = config["OpenAI:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            builder.AddOpenAIChatCompletion("gpt-4o-mini", apiKey);
            _hasAiConfigured = true;
        }

        _kernel = builder.Build();
        if (_hasAiConfigured)
        {
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();
        }
    }

    public async Task<ValidationAgentResponse> EvaluateDispatchSafetyAsync(ValidationAgentRequest request)
    {
        _logger.LogInformation("Agent 4 evaluating safety for Triage {TriageId}", request.TriageId);

        if (_hasAiConfigured && _chatService != null)
        {
            try
            {
                return await EvaluateWithAiAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI Evaluation failed. Falling back to rule-based recovery.");
                return await EvaluateWithRulesFallbackAsync(request);
            }
        }
        else
        {
            _logger.LogInformation("No AI key configured. Using rule-based fallback recovery.");
            return await EvaluateWithRulesFallbackAsync(request);
        }
    }

    private async Task<ValidationAgentResponse> EvaluateWithAiAsync(ValidationAgentRequest request)
    {
        var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
        var chatHistory = new ChatHistory();
        
        chatHistory.AddSystemMessage(@"
You are Agent 4 (Validation & Safety Agent) for CarePulse.
Your primary role is to evaluate emergency dispatch assignments for safety.
You MUST use the Tool_GetSystemSafetyThresholds tool to fetch safety bounds.
Evaluate the severity score and ETA against these bounds.
Output your response as JSON matching this schema exactly:
{
  ""RequiresHumanApproval"": true,
  ""FlaggedRules"": [""string""],
  ""VerdictReason"": ""string""
}
CRITICAL SAFETY POLICY: Every emergency nurse dispatch MUST require an authorized Doctor approval. RequiresHumanApproval must ALWAYS be true.
");
        chatHistory.AddUserMessage($"Evaluate dispatch: TriageId={request.TriageId}, Severity={request.SeverityScore}, ETA={request.EtaMinutes} mins.");

        var result = await _chatService!.GetChatMessageContentAsync(chatHistory, settings, _kernel);
        var content = result.Content ?? string.Empty;

        // Clean up markdown code blocks if present
        if (content.StartsWith("```json"))
        {
            content = content.Replace("```json", "").Replace("```", "").Trim();
        }

        try
        {
            var response = JsonSerializer.Deserialize<ValidationAgentResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (response != null)
            {
                response.RequiresHumanApproval = true; // Hard-enforce policy
                return response;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse AI JSON response. Falling back.");
        }

        return await EvaluateWithRulesFallbackAsync(request);
    }

    private async Task<ValidationAgentResponse> EvaluateWithRulesFallbackAsync(ValidationAgentRequest request)
    {
        var response = new ValidationAgentResponse
        {
            RequiresHumanApproval = true, // ALWAYS TRUE
            FlaggedRules = new List<string>()
        };

        // Manual tool invocation for fallback
        var thresholdsResult = await _kernel.InvokeAsync("SafetyThresholds", "Tool_GetSystemSafetyThresholds");
        var thresholdsJson = thresholdsResult.GetValue<string>();
        var thresholds = JsonSerializer.Deserialize<JsonElement>(thresholdsJson!);

        double maxSeverity = thresholds.GetProperty("MaxAllowedSeverityScore").GetDouble();
        int maxEta = thresholds.GetProperty("MaxAllowedEtaMinutes").GetInt32();

        if (request.SeverityScore > maxSeverity)
            response.FlaggedRules.Add($"Severity Score ({request.SeverityScore}) exceeds maximum allowed ({maxSeverity}).");
        
        if (request.EtaMinutes > maxEta)
            response.FlaggedRules.Add($"ETA ({request.EtaMinutes} mins) exceeds maximum allowed ({maxEta} mins).");

        if (response.FlaggedRules.Count > 0)
            response.VerdictReason = "Fallback Execution: Safety thresholds exceeded. Mandatory doctor approval gate enforced.";
        else
            response.VerdictReason = "Fallback Execution: Within safety thresholds, but mandatory doctor approval gate is strictly enforced.";

        return response;
    }
}
