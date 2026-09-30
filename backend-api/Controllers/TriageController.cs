using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Services;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/triage")]
public class TriageController(ITriageService service, CarePulseDbContext db, TriageWorkflowRunner runner) : ControllerBase
{
    [HttpPost("submit")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<ActionResult<TriageResponseDto>> SubmitTriage(TriageSubmitRequestDto request)
    {
        await db.RequirePatientAsync(User, request.PatientProfileId);
        return Ok(await runner.RunAsync(request, User));
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Mine()
    {
        var id = User.UserId();
        return Ok(await db.TriageTickets.AsNoTracking().Where(t => db.PatientProfiles.Any(p => p.Id == t.PatientProfileId && p.UserId == id))
            .OrderByDescending(t => t.CreatedAt).Take(100).Select(t => new { t.Id, t.Status, t.RiskLevel, t.RiskScore, t.RequiresDoctorApproval,
                t.Reason, t.RecommendedAction, t.CreatedAt, t.PatientProfileId, t.RecommendedSpecialty }).ToListAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var ticket = await GetAccessible(id);
        var workflow = await db.AiWorkflows.AsNoTracking().SingleOrDefaultAsync(w => w.TriageTicketId == id);
        var dispatch = await db.DispatchTickets.AsNoTracking().SingleOrDefaultAsync(d => d.TriageTicketId == id);
        return Ok(new { ticket.Id, ticket.PatientProfileId, ticket.Status, ticket.Symptoms, ticket.RiskScore, ticket.RiskLevel,
            ticket.RequiresDoctorApproval, ticket.Reason, ticket.RecommendedAction, ticket.FollowUpRecommended, ticket.RecommendedSpecialty,
            ticket.Latitude, ticket.Longitude, ticket.AssessmentFailed, WorkflowId = workflow?.Id,
            WorkflowStatus = workflow?.ExecutionStatus, DispatchStatus = dispatch?.Status,
            Execution = workflow == null ? null : System.Text.Json.JsonSerializer.Deserialize<object>(workflow.ExecutionJson) });
    }

    [HttpGet("pending-approvals")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetPendingApprovals() => Ok(await service.GetPendingApprovalsAsync());

    [HttpGet("{id:guid}/audit-log")]
    public async Task<IActionResult> GetAuditLog(Guid id)
    {
        await GetAccessible(id);
        return Ok(await service.GetAuditLogAsync(id));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTriage(Guid id)
    {
        await GetAccessible(id);
        if (await db.DispatchTickets.AnyAsync(d => d.TriageTicketId == id)) return Conflict(new { message = "Dispatched cases must retain their clinical history." });
        return await service.DeleteTriageAsync(id) ? NoContent() : NotFound();
    }

    [HttpPut("{id:guid}/destination")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Destination(Guid id, DestinationDto request)
    {
        var ticket = await GetAccessible(id);
        if (await db.DispatchTickets.AnyAsync(d => d.TriageTicketId == id)) return Conflict(new { message = "An assigned destination cannot be changed." });
        if (ticket.Status != TriageConstants.StatusApproved) return Conflict(new { message = "Approve this case before confirming its dispatch destination." });
        ticket.Latitude = request.Latitude;
        ticket.Longitude = request.Longitude;
        await WorkflowProgress.RecordAsync(db, id, "AwaitingDispatch", "DestinationConfirmed", new { Actor = User.UserId() });
        await db.SaveChangesAsync();
        return Ok(new { ticket.Latitude, ticket.Longitude });
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> ApproveTriage(Guid id, ApproveTriageRequestDto request)
    {
        var userId = User.UserId();
        if (!await db.DoctorProfiles.AnyAsync(d => d.UserId == userId)) return Forbid();
        return await service.ApproveTriageAsync(id, request, userId)
            ? Ok(new { message = "Approved. The case is waiting for nurse assignment." })
            : Conflict(new { message = "Case is no longer awaiting approval." });
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> RejectTriage(Guid id, ApproveTriageRequestDto request)
    {
        var userId = User.UserId();
        if (!await db.DoctorProfiles.AnyAsync(d => d.UserId == userId)) return Forbid();
        return await service.RejectTriageAsync(id, request, userId)
            ? Ok(new { message = "Case rejected." }) : Conflict(new { message = "Case is no longer awaiting approval." });
    }

    private async Task<Entities.TriageTicket> GetAccessible(Guid id)
    {
        var ticket = await db.TriageTickets.SingleOrDefaultAsync(t => t.Id == id) ?? throw new ApiProblem(404, "Triage not found.");
        await db.RequirePatientAsync(User, ticket.PatientProfileId);
        return ticket;
    }
}

public class DestinationDto
{
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Range(-90,90)] public double? Latitude { get; set; }
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.Range(-180,180)] public double? Longitude { get; set; }
}
