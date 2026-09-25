using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Services.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

/// <summary>Admin-only doctor onboarding (login account + minimal profile).
/// Deliberately a separate route/file from Student 3's DoctorsController
/// (POST /api/v1/doctors, scheduling-focused) to avoid a file/route collision
/// when feature/student3-doctor-scheduling merges - reconcile then.</summary>
[ApiController]
[Route("api/v1/doctors")]
[Authorize(Roles = "Admin")]
public class DoctorRegistrationController : ControllerBase
{
    private readonly IStaffRegistrationService _staffService;

    public DoctorRegistrationController(IStaffRegistrationService staffService)
    {
        _staffService = staffService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<DoctorProfileDto>> Register(RegisterDoctorDto dto)
    {
        var result = await _staffService.RegisterDoctorAsync(dto);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    /// <summary>Soft delete: hides this doctor from the active directory and blocks
    /// their login, without erasing the record - mirrors PatientsController.DeactivateProfile.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _staffService.DeleteDoctorAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpPut("{id:guid}/restore")]
    public async Task<ActionResult> Restore(Guid id)
    {
        var result = await _staffService.RestoreDoctorAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        return NoContent();
    }
}
