using System.Security.Claims;
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
    public async Task<ActionResult<TriageResponseDto>> SubmitTriage([FromBody] TriageSubmitRequestDto request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Symptoms))
        {
            return BadRequest("Invalid triage data.");
        }

        var result = await _triageService.SubmitTriageAsync(request);
        return Ok(result);
    }

    [HttpGet("pending-approvals")]
    // [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<IEnumerable<TriageResponseDto>>> GetPendingApprovals()
    {
        var result = await _triageService.GetPendingApprovalsAsync();
        return Ok(result);
    }

    [HttpGet("{id}/audit-log")]
    public async Task<ActionResult<IEnumerable<AiTriageLogDto>>> GetAuditLog(Guid id)
    {
        var result = await _triageService.GetAuditLogAsync(id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTriage(Guid id)
    {
        var success = await _triageService.DeleteTriageAsync(id);
        if (!success)
        {
            return NotFound("Triage ticket not found or already deleted.");
        }

        return NoContent();
    }

    [HttpPost("{id}/approve")]
    // [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> ApproveTriage(Guid id, [FromBody] ApproveTriageRequestDto request)
    {
        var success = await _triageService.ApproveTriageAsync(id, request);
        if (!success)
        {
            return BadRequest("Triage ticket not found or does not require approval.");
        }

        return Ok(new { message = "Triage request approved successfully." });
    }

    [HttpPost("{id}/reject")]
    // [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> RejectTriage(Guid id, [FromBody] ApproveTriageRequestDto request)
    {
        var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var success = await _triageService.RejectTriageAsync(id, request, doctorId);
        if (!success)
        {
            return BadRequest("Triage ticket not found or does not require approval.");
        }

        return Ok(new { message = "Triage case rejected successfully." });
    }
}
