using System.ComponentModel.DataAnnotations;
using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

/// <summary>
/// Minimal doctor identity record, scoped to admin-driven registration only.
/// Student 3 (Doctor Rostering, Clinic Scheduling & Consultations) owns the
/// rest of this entity's eventual shape (ClinicRosters, AppointmentSlots) on
/// feature/student3-doctor-scheduling - expect to reconcile this file when
/// that branch merges, keeping UserId and adding their scheduling collections back.
/// </summary>
public class DoctorProfile : BaseEntity
{
    // string, not Guid: must match ApplicationUser.Id (ASP.NET Core Identity)
    // so a doctor profile is actually linked to a real login account.
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }
}
