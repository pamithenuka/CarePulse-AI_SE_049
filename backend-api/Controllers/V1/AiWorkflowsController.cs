using System.Security.Claims;
using CarePulse.Api.DTOs.Ai;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

/// <summary>
/// Agent 1 (Coordinator/Planner) — receives a domain objective for a patient,
/// produces a structured multi-step plan, and lets a Doctor/Admin review it.
/// Web-only for now (Section 8's mobile submission path is a future extension).
/// </summary>
[ApiController]
[Route("api/v1/patients/{patientId:guid}/ai-plan")]
[Authorize]
public class AiWorkflowsController : ControllerBase
{
    private readonly IAgentPlannerService _plannerService;

    public AiWorkflowsController(IAgentPlannerService plannerService)
    {
        _plannerService = plannerService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IList<string> CurrentRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

    // A Patient submits their own reported symptoms (mirrors the future Flutter submission flow);
    // Doctor/Admin may submit on behalf of any patient (e.g. relayed over the phone).
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<AiWorkflowDto>> CreatePlan(Guid patientId, CreateAiPlanDto dto)
    {
        var result = await _plannerService.CreatePlanAsync(patientId, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<List<AiWorkflowDto>>> GetPlans(Guid patientId)
    {
        var result = await _plannerService.GetPlansAsync(patientId, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    // Approve/reject stays Doctor/Admin-only — a Patient cannot review their own submitted plan.
    [HttpPut("{workflowId:guid}/review")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<AiWorkflowDto>> ReviewPlan(Guid patientId, Guid workflowId, ReviewAiPlanDto dto)
    {
        var result = await _plannerService.ReviewPlanAsync(patientId, workflowId, CurrentUserId, dto);
        return ToActionResult(result, value => Ok(value));
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult<T>> onSuccess)
    {
        if (result.Succeeded)
        {
            return onSuccess(result.Value!);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Forbidden => Forbid(),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
