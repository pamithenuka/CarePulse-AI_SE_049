using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Patients;

public class MedicalHistory : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public string ConditionName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateOnly DiagnosedOn { get; set; }
    public bool IsChronic { get; set; }
    public string? CurrentMedications { get; set; }
    public bool IsResolved { get; set; }
    public DateOnly? ResolvedOn { get; set; }
    public string? RecordedByUserId { get; set; }
}
