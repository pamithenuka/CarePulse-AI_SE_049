using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class OnSiteVitalsRecords : BaseEntity
{
    public Guid DispatchTicketId { get; set; }
    public int HeartRate { get; set; }
    public string BloodPressure { get; set; } = string.Empty;
    public double BodyTempC { get; set; }
    public int OxygenSaturation { get; set; }
    public string ClinicalNotes { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
}
