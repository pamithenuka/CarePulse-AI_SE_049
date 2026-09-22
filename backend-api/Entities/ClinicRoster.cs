using System.ComponentModel.DataAnnotations.Schema;
using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

public class ClinicRoster : BaseEntity
{
    public Guid DoctorId { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public DoctorProfile? Doctor { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}