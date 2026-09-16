using CarePulse.Api.DTOs;
using CarePulse.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class TriageController : ControllerBase
{
    private readonly ITriageService _triageService;

    public TriageController(ITriageService triageService)
    {
        _triageService = triageService;
    }

    [HttpPost("submit")]
    public ActionResult<TriageResponseDto> SubmitTriage([FromBody] TriageSubmitRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Symptoms))
        {
            return BadRequest("Invalid triage data.");
        }

        var result = _triageService.SubmitTriage(request);
        return Ok(result);
    }

    [HttpGet("pending-approvals")]
    // [Authorize(Roles = "Doctor,Admin")] // Commented out for easier testing in memory mode without auth setup yet
    public ActionResult<IEnumerable<TriageResponseDto>> GetPendingApprovals()
    {
        var result = _triageService.GetPendingApprovals();
        return Ok(result);
    }

    [HttpGet("{id}/audit-log")]
    public ActionResult<IEnumerable<AiTriageLogDto>> GetAuditLog(Guid id)
    {
        var result = _triageService.GetAuditLog(id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteTriage(Guid id)
    {
        var success = _triageService.DeleteTriage(id);
        if (!success)
        {
            return NotFound("Triage ticket not found or already deleted.");
        }

        return NoContent();
    }

    [HttpPost("{id}/approve")]
    // [Authorize(Roles = "Doctor")] // Commented out for easier testing in memory mode
    public IActionResult ApproveTriage(Guid id, [FromBody] ApproveTriageRequestDto request)
    {
        var success = _triageService.ApproveTriage(id, request);
        if (!success)
        {
            return BadRequest("Triage ticket not found or does not require approval.");
        }

        return Ok(new { message = "Triage request approved successfully." });
    }
}
