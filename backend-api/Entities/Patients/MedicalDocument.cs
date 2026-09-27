using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Patients;

public enum MedicalDocumentType
{
    Prescription,
    LabReport,
    ImagingScan,
    Other
}

public class MedicalDocument : BaseEntity
{
    public Guid PatientProfileId { get; set; }
    public PatientProfile? PatientProfile { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public MedicalDocumentType DocumentType { get; set; }
}
