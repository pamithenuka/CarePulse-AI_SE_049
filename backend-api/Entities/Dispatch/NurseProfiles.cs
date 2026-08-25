using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class NurseProfiles : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public double CurrentLat { get; set; }
    public double CurrentLng { get; set; }
    public bool IsAvailable { get; set; }
    public string Specialization { get; set; } = string.Empty;
}
