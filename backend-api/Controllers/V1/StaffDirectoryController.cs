using System.Security.Claims;
using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Services.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

/// <summary>Read-only directory of Admin-registered Doctors/Nurses, for the
/// web-admin "Doctors"/"Nurses" list + detail pages - mirrors the Patient
/// Master Registry pattern. Deliberately rooted at api/v1/staff/* (not
/// api/v1/doctors) to avoid colliding with Student 3's DoctorsController
/// (api/v1/doctors, scheduling-focused) once that branch merges.</summary>
[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = "Doctor,Admin")]
public class StaffDirectoryController : ControllerBase
{
    private readonly IStaffRegistrationService _staffService;

    public StaffDirectoryController(IStaffRegistrationService staffService)
    {
        _staffService = staffService;
    }

    private bool IsAdmin => User.FindAll(ClaimTypes.Role).Any(c => c.Value == "Admin");

    [HttpGet("doctors")]
    public async Task<ActionResult<List<DoctorProfileDto>>> GetDoctors([FromQuery] string status = "active")
    {
        // Only Admins may look at deleted doctors, mirroring PatientsController.GetPatients.
        var effectiveStatus = IsAdmin ? status : "active";
        return Ok(await _staffService.GetDoctorsAsync(effectiveStatus));
    }

    [HttpGet("doctors/{id:guid}")]
    public async Task<ActionResult<DoctorProfileDto>> GetDoctor(Guid id)
    {
        var result = await _staffService.GetDoctorByIdAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        if (result.Value!.Status == "Inactive" && !IsAdmin)
        {
            return NotFound(new { message = "Doctor not found." });
        }
        return Ok(result.Value);
    }

    [HttpGet("nurses")]
    public async Task<ActionResult<List<NurseProfileDto>>> GetNurses([FromQuery] string status = "active")
    {
        var effectiveStatus = IsAdmin ? status : "active";
        return Ok(await _staffService.GetNursesAsync(effectiveStatus));
    }

    [HttpGet("nurses/{id:guid}")]
    public async Task<ActionResult<NurseProfileDto>> GetNurse(Guid id)
    {
        var result = await _staffService.GetNurseByIdAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        if (result.Value!.Status == "Inactive" && !IsAdmin)
        {
            return NotFound(new { message = "Nurse not found." });
        }
        return Ok(result.Value);
    }
}
