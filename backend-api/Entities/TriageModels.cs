using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

public class TriageTicket : BaseEntity
{
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool RequiresDoctorApproval { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool FollowUpRecommended { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
    
    // Navigation properties
    public ICollection<AiTriageLog> AiTriageLogs { get; set; } = new List<AiTriageLog>();
    public RiskAssessment? RiskAssessment { get; set; }
    public ApprovalQueue? ApprovalQueue { get; set; }
}

public class AiTriageLog : BaseEntity
{
    public Guid TriageTicketId { get; set; }
    public string LogMessage { get; set; } = string.Empty;

    public TriageTicket? TriageTicket { get; set; }
}

public class RiskAssessment : BaseEntity
{
    public Guid TriageTicketId { get; set; }
    public int Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;

    public TriageTicket? TriageTicket { get; set; }
}

public class ApprovalQueue : BaseEntity
{
    public Guid TriageTicketId { get; set; }
    public string ReviewStatus { get; set; } = string.Empty;
    public string? ReviewedByDoctorId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public TriageTicket? TriageTicket { get; set; }
}
