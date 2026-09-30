using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities.Dispatch;

public class DispatchTickets : BaseEntity
{
    [System.ComponentModel.DataAnnotations.Timestamp]
    public uint RowVersion { get; set; }
    public Guid TriageTicketId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid NurseId { get; set; }
    public string Status { get; set; } = string.Empty; // Assigned, EnRoute, ArrivedOnSite, Completed, Escalated
    public DateTime AssignedAt { get; set; }
    public double? DestinationLat { get; set; }
    public double? DestinationLng { get; set; }
    public bool IsEscalated { get; set; }
    public string? EscalationNotes { get; set; }
    public string SafetySummary { get; set; } = string.Empty;
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
