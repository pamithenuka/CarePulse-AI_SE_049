using System.ComponentModel.DataAnnotations;
using CarePulse.Api.Entities.Patients;

namespace CarePulse.Api.DTOs.Patients;

public class EmergencyContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string RelationshipToPatient { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class CreateEmergencyContactDto
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string RelationshipToPatient { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public class UpdateEmergencyContactDto
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string RelationshipToPatient { get; set; } = string.Empty;

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public class MedicalHistoryDto
{
    public Guid Id { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly DiagnosedOn { get; set; }
    public bool IsChronic { get; set; }
    public string? CurrentMedications { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AddMedicalHistoryDto
{
    [Required, MaxLength(150)]
    public string ConditionName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateOnly DiagnosedOn { get; set; }

    public bool IsChronic { get; set; }

    [MaxLength(500)]
    public string? CurrentMedications { get; set; }
}

public class UpdateMedicalHistoryDto
{
    [Required, MaxLength(150)]
    public string ConditionName { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [Required]
    public DateOnly DiagnosedOn { get; set; }

    public bool IsChronic { get; set; }

    [MaxLength(500)]
    public string? CurrentMedications { get; set; }

    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
}

public class MedicalDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public MedicalDocumentType DocumentType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateMedicalDocumentDto
{
    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public MedicalDocumentType DocumentType { get; set; }
}

public class CreatePatientProfileDto
{
    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(5)]
    public string? BloodType { get; set; }

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    [Required, MaxLength(30)]
    [RegularExpression(@"^(\d{9}[VvXx]|\d{12})$", ErrorMessage = "National ID must be 9 digits + V/X (old format) or 12 digits (new format).")]
    public string NationalId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Allergies { get; set; }

    public List<CreateEmergencyContactDto> EmergencyContacts { get; set; } = new();
}

/// <summary>
/// Admin-only, used by the web app to onboard a patient without the mobile app:
/// creates the login account (Email/Password) and the profile in one call.
/// </summary>
public class RegisterPatientDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [Required, MaxLength(20)]
    public string Gender { get; set; } = string.Empty;

    [MaxLength(5)]
    public string? BloodType { get; set; }

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Address { get; set; }

    [Required, MaxLength(30)]
    [RegularExpression(@"^(\d{9}[VvXx]|\d{12})$", ErrorMessage = "National ID must be 9 digits + V/X (old format) or 12 digits (new format).")]
    public string NationalId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Allergies { get; set; }

    public List<CreateEmergencyContactDto> EmergencyContacts { get; set; } = new();
}

/// <summary>
/// A patient may only submit PhoneNumber/Address. Any other populated field
/// is ignored unless the requester is an Admin — enforced in PatientService.
/// </summary>
public class UpdatePatientProfileDto
{
    [MaxLength(120)]
    public string? FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(5)]
    public string? BloodType { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    [MaxLength(30)]
    [RegularExpression(@"^(\d{9}[VvXx]|\d{12})$", ErrorMessage = "National ID must be 9 digits + V/X (old format) or 12 digits (new format).")]
    public string? NationalId { get; set; }

    [MaxLength(500)]
    public string? Allergies { get; set; }
}

public class PatientListItemDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string? MainConditions { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
}

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class PatientProfileDetailDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? LastEmergencyBroadcastAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
    public List<MedicalHistoryDto> MedicalHistories { get; set; } = new();
    public List<MedicalDocumentDto> MedicalDocuments { get; set; } = new();
}

public class EmergencyAlertNotificationDto
{
    public string ContactName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public NotificationDeliveryStatus DeliveryStatus { get; set; }
}

public class EmergencyAlertResultDto
{
    public Guid AlertLogId { get; set; }
    public Guid PatientProfileId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string? BloodGroup { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicConditions { get; set; } = new();
    public List<string> ActiveMedications { get; set; } = new();
    public List<EmergencyAlertNotificationDto> NotifiedContacts { get; set; } = new();
}

public class EmergencyAlertLogSummaryDto
{
    public Guid Id { get; set; }
    public Guid PatientProfileId { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string TriggeredByUserId { get; set; } = string.Empty;
    public string TriggeredByName { get; set; } = "Unknown";
    public DateTime TriggeredAt { get; set; }
    public int ContactsNotified { get; set; }
    public int ContactsFailed { get; set; }
}

public class PatientAuditLogDto
{
    public Guid Id { get; set; }
    public Guid PatientProfileId { get; set; }
    public string PatientFullName { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ChangesJson { get; set; } = "[]";
    public string ChangedByUserId { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = "Unknown";
    public DateTime ChangedAt { get; set; }
}

/// <summary>
/// Consumed by Agent 1 (Planner) once Student 2's symptom-submission workflow
/// starts. Call IPatientService.GetPatientContextAsync(patientId) directly —
/// this never needs to travel over HTTP since agents run in-process.
/// </summary>
public class PatientContextDto
{
    public Guid PatientProfileId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public List<string> Allergies { get; set; } = new();
    public List<string> ChronicConditions { get; set; } = new();
    public List<string> ActiveMedications { get; set; } = new();
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
}
