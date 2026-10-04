using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Services.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

/// <summary>Admin-only nurse onboarding (login account + minimal profile).
/// Dispatch-runtime fields (CurrentLat/Lng, IsAvailable) stay owned by
/// Student 4's DispatchController - this only creates the base identity.</summary>
[ApiController]
[Route("api/v1/nurses")]
[Authorize(Roles = "Admin")]
public class NurseRegistrationController : ControllerBase
{
    private readonly IStaffRegistrationService _staffService;

    public NurseRegistrationController(IStaffRegistrationService staffService)
    {
        _staffService = staffService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<NurseProfileDto>> Register(RegisterNurseDto dto)
    {
        var result = await _staffService.RegisterNurseAsync(dto);
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

    /// <summary>Soft delete: hides this nurse from the active directory and blocks
    /// their login, without erasing the record - mirrors PatientsController.DeactivateProfile.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var result = await _staffService.DeleteNurseAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        return NoContent();
    }

    [HttpPut("{id:guid}/restore")]
    public async Task<ActionResult> Restore(Guid id)
    {
        var result = await _staffService.RestoreNurseAsync(id);
        if (!result.Succeeded)
        {
            return NotFound(new { message = result.ErrorMessage });
        }
        return NoContent();
    }
}
