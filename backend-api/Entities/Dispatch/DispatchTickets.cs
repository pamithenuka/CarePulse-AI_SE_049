using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class DispatchTickets : BaseEntity
{
    public Guid TriageTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid NurseId { get; set; }
    public string Status { get; set; } = string.Empty; // Assigned, EnRoute, ArrivedOnSite, Completed, Escalated
    public DateTime AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
