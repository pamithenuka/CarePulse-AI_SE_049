using CarePulse.Api.DTOs.Ai;
using CarePulse.Api.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

/// <summary>Cross-patient AI Care Plan inbox for Doctors/Admins - built for the
/// mobile Doctor review flow, which needs to see plans awaiting review without
/// already knowing which patient they belong to. AiWorkflowsController stays
/// per-patient (matches the web app's per-patient AI Plan tab).</summary>
[ApiController]
[Route("api/v1/ai-plans")]
[Authorize(Roles = "Doctor,Admin")]
public class AiPlanReviewController : ControllerBase
{
    private readonly IAgentPlannerService _plannerService;

    public AiPlanReviewController(IAgentPlannerService plannerService)
    {
        _plannerService = plannerService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<List<AiWorkflowDto>>> GetPending()
    {
        var result = await _plannerService.GetPendingReviewPlansAsync();
        return Ok(result.Value);
    }
}
