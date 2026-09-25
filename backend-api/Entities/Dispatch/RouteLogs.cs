using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class RouteLogs : BaseEntity
{
    public Guid DispatchTicketId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double SpeedKmh { get; set; }
    public double Heading { get; set; }
    public DateTime RecordedAt { get; set; }
}
