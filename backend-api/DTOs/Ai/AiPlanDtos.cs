using System.ComponentModel.DataAnnotations;

namespace CarePulse.Api.DTOs.Ai;

public class CreateAiPlanDto
{
    [Required, MinLength(5), MaxLength(1000)]
    public string Objective { get; set; } = string.Empty;
}

public class AiPlanStepDto
{
    public string Agent { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public class AiWorkflowDto
{
    public Guid Id { get; set; }
    public Guid PatientProfileId { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public List<AiPlanStepDto> Steps { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public string ToolCallSummary { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = string.Empty;
    public string? ReviewedByName { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReviewAiPlanDto
{
    [Required]
    public bool Approved { get; set; }

    [MaxLength(500)]
    public string? ReviewNotes { get; set; }
}

/// <summary>
/// Internal shape the LLM is asked to return — validated before it's trusted.
/// Deliberately has no Summary field: the model is never asked to restate patient
/// facts, so it can't misreport one (see AgentPlannerService.BuildDeterministicSummary).
/// </summary>
public class RawAiPlan
{
    public List<RawAiPlanStep>? Steps { get; set; }
}

public class RawAiPlanStep
{
    public string? Agent { get; set; }
    public string? Task { get; set; }
}
