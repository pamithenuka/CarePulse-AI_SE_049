using System.ComponentModel.DataAnnotations;

namespace CarePulse.Api.DTOs.Staff;

/// <summary>Admin-only: creates the login account (Doctor role) and a minimal
/// DoctorProfile in one call, mirroring RegisterPatientDto's pattern.</summary>
public class RegisterDoctorDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required, Phone, MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class DoctorProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Status { get; set; } = "Active";
}

/// <summary>Admin-only: creates the login account (Nurse role) and a minimal
/// NurseProfiles row. LicenseNumber/Specialization are registration-time
/// fields; CurrentLat/CurrentLng/IsAvailable are dispatch runtime state
/// (Student 4's concern) and default to unavailable/unset here.</summary>
public class RegisterNurseDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Specialization { get; set; } = string.Empty;
}

public class NurseProfileDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string Status { get; set; } = "Active";
}
