using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Patients;

public class PatientProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string NationalId { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public string? Allergies { get; set; }
    public DateTime? LastEmergencyBroadcastAt { get; set; }

    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<MedicalHistory> MedicalHistories { get; set; } = new List<MedicalHistory>();
    public ICollection<MedicalDocument> MedicalDocuments { get; set; } = new List<MedicalDocument>();
}
