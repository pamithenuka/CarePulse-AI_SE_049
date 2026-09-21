using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePulse.Api.Models;

// A doctor's recurring weekly availability window (e.g. "Mondays 09:00-13:00").
// Used to generate AppointmentSlots.
public class ClinicRoster
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DoctorId { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public DoctorProfile? Doctor { get; set; }

    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    // Length of each bookable slot, in minutes (e.g. 30).
    public int SlotDurationMinutes { get; set; } = 30;

    public bool IsActive { get; set; } = true;
}
