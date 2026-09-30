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
/// One durable record per planner invocation. Execution state advances with business
/// commands; execution summaries are appended. No soft delete: workflow history is retained.
/// </summary>
public class AiWorkflow
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public uint RowVersion { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TriageTicketId { get; set; }
    public string ExecutionStatus { get; set; } = "PlanOnly";
    public string ExecutionJson { get; set; } = "[]";
    public DateTime? FinishedAt { get; set; }
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
