namespace CarePulse.Api.Entities.Ai;

public enum AiWorkflowStatus
{
    PlanCreated,
    ValidationFailed,
    LlmError
}

public enum AiPlanReviewStatus
{
    NotReviewed,
    Approved,
    Rejected
}

/// <summary>
/// One record per Agent 1 (Planner/Coordinator) invocation. Immutable audit-style
/// record — intentionally does NOT inherit BaseEntity (no soft delete), matching
/// EmergencyAlertLog: a workflow run must never be hidden or edited, only reviewed.
/// </summary>
public class AiWorkflow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientProfileId { get; set; }

    public string Objective { get; set; } = string.Empty;

    /// <summary>Snapshot of the PatientContextDto passed to the LLM, for auditability.</summary>
    public string ContextSnapshotJson { get; set; } = "{}";

    /// <summary>Records the one allow-listed tool call Agent 1 made (name, timing, result summary).</summary>
    public string ToolCallSummary { get; set; } = string.Empty;

    public string? PlanSummary { get; set; }

    /// <summary>JSON array of {"agent","task","status"} — the delegated steps.</summary>
    public string StepsJson { get; set; } = "[]";

    public AiWorkflowStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string ModelUsed { get; set; } = string.Empty;

    public string CreatedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AiPlanReviewStatus ReviewStatus { get; set; } = AiPlanReviewStatus.NotReviewed;
    public string? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }
}
