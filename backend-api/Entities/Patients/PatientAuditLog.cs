namespace CarePulse.Api.Entities.Patients;

public enum AuditAction
{
    Created,
    Updated,
    Deleted
}

/// <summary>
/// One row per changed patient-module entity (PatientProfile, MedicalHistory,
/// EmergencyContact, MedicalDocument). Written automatically from
/// CarePulseDbContext.SaveChangesAsync — never inserted by hand.
/// </summary>
public class PatientAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientProfileId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }

    /// <summary>JSON array of {"field","oldValue","newValue"} entries.</summary>
    public string ChangesJson { get; set; } = "[]";

    public string ChangedByUserId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
