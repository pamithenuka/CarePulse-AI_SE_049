using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePulse.Api.Models;

// Doctor's notes + prescription after a booked appointment took place.
public class ConsultationRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SlotId { get; set; }

    [ForeignKey(nameof(SlotId))]
    public AppointmentSlot? Slot { get; set; }

    [Required]
    public Guid DoctorId { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    [Required]
    public string Notes { get; set; } = string.Empty;

    public string? Prescription { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
