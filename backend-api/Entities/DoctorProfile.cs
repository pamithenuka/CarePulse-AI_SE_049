using System.ComponentModel.DataAnnotations;
using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

public class DoctorProfile : BaseEntity
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ClinicRoster> ClinicRosters { get; set; } = new List<ClinicRoster>();
    public ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();
}