using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Services.Patients;

namespace CarePulse.Api.Services.Staff;

/// <summary>Admin-only staff onboarding: creates the login account (Identity
/// user + role) and a minimal domain profile in one atomic call, mirroring
/// IPatientService.RegisterPatientAsync's pattern for Patients.</summary>
public interface IStaffRegistrationService
{
    Task<ServiceResult<DoctorProfileDto>> RegisterDoctorAsync(RegisterDoctorDto dto);

    Task<ServiceResult<NurseProfileDto>> RegisterNurseAsync(RegisterNurseDto dto);

    /// <summary>status: "active" (default), "inactive" (deleted only) or "all" - mirrors IPatientService.GetPatientsAsync.</summary>
    Task<List<DoctorProfileDto>> GetDoctorsAsync(string status = "active");

    Task<ServiceResult<DoctorProfileDto>> GetDoctorByIdAsync(Guid id);

    Task<List<NurseProfileDto>> GetNursesAsync(string status = "active");

    Task<ServiceResult<NurseProfileDto>> GetNurseByIdAsync(Guid id);

    /// <summary>Admin-only soft delete: hides the record from the active directory and
    /// blocks login, without erasing it - mirrors IPatientService.DeactivateProfileAsync.</summary>
    Task<ServiceResult<bool>> DeleteDoctorAsync(Guid id);

    Task<ServiceResult<bool>> RestoreDoctorAsync(Guid id);

    Task<ServiceResult<bool>> DeleteNurseAsync(Guid id);

    Task<ServiceResult<bool>> RestoreNurseAsync(Guid id);
}
