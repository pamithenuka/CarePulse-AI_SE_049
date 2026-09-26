using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class NurseProfiles : BaseEntity
{
    // string, not Guid: ASP.NET Core Identity's ApplicationUser.Id is a string
    // (GUID-formatted, but typed as string) - this must match to actually link
    // a nurse profile to a real login account.
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public double CurrentLat { get; set; }
    public double CurrentLng { get; set; }
    public bool IsAvailable { get; set; }
    public string Specialization { get; set; } = string.Empty;
}
