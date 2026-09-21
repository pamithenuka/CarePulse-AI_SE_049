namespace CarePulse.Api.DTOs;

public static class TriageConstants
{
    public const String RiskLow = "LOW";
    public const String RiskMedium = "MEDIUM";
    public const String RiskHigh = "HIGH";

    public const String ActionSelfCare = "SELF_CARE_MONITORING";
    public const String ActionConsultation = "DOCTOR_CONSULTATION";
    public const String ActionDoctorApproval = "DOCTOR_APPROVAL_REQUIRED";

    public const String StatusCompleted = "COMPLETED";
    public const String StatusConsultationRecommended = "DOCTOR_CONSULTATION_RECOMMENDED";
    public const String StatusNeedsApproval = "NEEDS_DOCTOR_APPROVAL";
    public const String StatusApproved = "APPROVED";
    public const String StatusRejected = "REJECTED";
}

public class TriageSubmitRequestDto
{
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> AdditionalSymptoms { get; set; } = new();
    public bool HasPhotoAttachment { get; set; }
}

public class TriageResponseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool RequiresDoctorApproval { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool FollowUpRecommended { get; set; }
}

public class AiTriageLogDto
{
    public Guid Id { get; set; }
    public Guid TriageTicketId { get; set; }
    public string LogMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ApproveTriageRequestDto
{
    public string Notes { get; set; } = string.Empty;
}
