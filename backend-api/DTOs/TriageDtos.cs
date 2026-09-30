using System.ComponentModel.DataAnnotations;
using FluentValidation;

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
    public const String StatusApproved = "APPROVED_BY_DOCTOR";
    public const String StatusRejected = "REJECTED";

    public static readonly string[] AllowedSpecialties = new[]
    {
        "GENERAL_MEDICINE",
        "DERMATOLOGY",
        "CARDIOLOGY",
        "NEUROLOGY",
        "ORTHOPEDICS",
        "PEDIATRICS",
        "ENT",
        "OPHTHALMOLOGY",
        "GYNECOLOGY",
        "PSYCHIATRY"
    };
}

public class TriageSubmitRequestDto
{
    public Guid PatientProfileId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> AdditionalSymptoms { get; set; } = new();
    public bool HasPhotoAttachment { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class TriageResponseDto
{
    public Guid? WorkflowId { get; set; }
    public string? WorkflowStatus { get; set; }
    public string? DispatchStatus { get; set; }
    public bool AssessmentFailed { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public Guid Id { get; set; }
    public Guid PatientProfileId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool RequiresDoctorApproval { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool FollowUpRecommended { get; set; }
    public string RecommendedSpecialty { get; set; } = string.Empty;
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
    [Required, MinLength(3), MaxLength(500)]
    public string Notes { get; set; } = string.Empty;
}

public class TriageSubmitValidator : AbstractValidator<TriageSubmitRequestDto>
{
    public TriageSubmitValidator()
    {
        RuleFor(x => x.PatientProfileId).NotEmpty();
        RuleFor(x => x.Symptoms).NotEmpty().MinimumLength(5).MaximumLength(1000);
        RuleFor(x => x.Duration).MaximumLength(100);
        RuleFor(x => x.Severity).MaximumLength(100);
        RuleFor(x => x.AdditionalSymptoms).NotNull().Must(x => x == null || x.Count <= 20);
        RuleForEach(x => x.AdditionalSymptoms).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x).Must(x => x.Latitude.HasValue == x.Longitude.HasValue).WithMessage("Provide both latitude and longitude.");
        RuleFor(x => x.HasPhotoAttachment).Equal(false).WithMessage("Attach medical photos through the document vault; triage currently accepts text only.");
    }
}
