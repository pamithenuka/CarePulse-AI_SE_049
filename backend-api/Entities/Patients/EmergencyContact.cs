using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Patients;

public class EmergencyContact : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string RelationshipToPatient { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
