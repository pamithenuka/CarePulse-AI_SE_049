using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

public class ConsultationRecord : BaseEntity
{
    public Guid SlotId { get; set; }

    [ForeignKey(nameof(SlotId))]
    public AppointmentSlot? Slot { get; set; }

    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }

    [Required]
    public string Notes { get; set; } = string.Empty;

    public string? Prescription { get; set; }
}