using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.DTOs.Dispatch;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Services.Agents;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Common;
using CarePulse.Api.Services.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/v1/dispatch")]
[Authorize(Roles = "Nurse,Doctor,Admin")]
public class DispatchController(CarePulseDbContext db, IGoogleMapsService routes, IValidationAgent safety) : ControllerBase
{
    private static readonly string[] ActiveStatuses = ["Assigned", "EnRoute", "ArrivedOnSite"];

    [HttpPut("nurses/{id:guid}/availability")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetAvailability(Guid id, NurseAvailabilityDto request)
    {
        var nurse = await db.NurseProfiles.SingleOrDefaultAsync(n => n.Id == id);
        if (nurse == null) return NotFound(new { message = "Active nurse not found." });
        if (await db.DispatchTickets.AnyAsync(d => d.NurseId == id && ActiveStatuses.Contains(d.Status)))
            return Conflict(new { message = "This nurse has an active dispatch. Complete the visit before changing availability." });
        nurse.IsAvailable = request.IsAvailable!.Value;
        await db.SaveChangesAsync();
        return Ok(new { nurse.Id, nurse.IsAvailable });
    }

    [HttpGet("waiting")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Waiting()
    {
        var tickets = await (from t in db.TriageTickets
            join p in db.PatientProfiles on t.PatientProfileId equals p.Id
            where t.Status == TriageConstants.StatusApproved && !db.DispatchTickets.Any(d => d.TriageTicketId == t.Id)
            orderby t.RiskScore descending, t.CreatedAt
            select new { t.Id, t.PatientProfileId, PatientName = p.FullName, t.RiskScore, t.Reason, t.Latitude, t.Longitude, t.CreatedAt }).ToListAsync();
        return Ok(tickets);
    }

    [HttpGet("nurses")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Nurses() => Ok(await db.NurseProfiles.AsNoTracking()
        .OrderBy(n => n.FullName).Select(n => new
        {
            n.Id, n.FullName, n.CurrentLat, n.CurrentLng, n.LocationRecordedAt,
            IsAvailable = n.IsAvailable && !db.DispatchTickets.Any(d => d.NurseId == n.Id && ActiveStatuses.Contains(d.Status))
        }).ToListAsync());

    [HttpPost("assign")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> AssignDispatch(AssignDispatchDto request)
    {
        var triage = await db.TriageTickets.Include(t => t.ApprovalQueue).SingleOrDefaultAsync(t => t.Id == request.TriageTicketId);
        if (triage == null) return NotFound(new { message = "Triage ticket not found." });
        if (triage.ApprovalQueue?.ReviewStatus != "APPROVED" ||
            string.IsNullOrEmpty(triage.ApprovalQueue.ReviewedByDoctorId))
            return Conflict(new { message = "An authorized doctor must approve this triage before dispatch." });
        var doctor = await db.DoctorProfiles.SingleOrDefaultAsync(d => d.UserId == triage.ApprovalQueue.ReviewedByDoctorId);
        if (doctor == null) return Conflict(new { message = "The approving doctor is no longer active; review is required." });
        await db.RequireDoctorAsync(User, doctor.Id);
        var existing = await db.DispatchTickets.AsNoTracking().SingleOrDefaultAsync(d => d.TriageTicketId == triage.Id);
        if (existing != null) return existing.NurseId == request.NurseId ? Ok(existing) : Conflict(new { message = "This case already has a dispatch." });
        if (triage.Status != TriageConstants.StatusApproved)
            return Conflict(new { message = "Case is no longer awaiting assignment." });
        if (!triage.Latitude.HasValue || !triage.Longitude.HasValue)
            return Conflict(new { message = "Record the patient's confirmed destination before assigning a nurse." });
        var nurse = await db.NurseProfiles.SingleOrDefaultAsync(n => n.Id == request.NurseId);
        if (nurse == null || !nurse.IsAvailable || await db.DispatchTickets.AnyAsync(d => d.NurseId == request.NurseId && ActiveStatuses.Contains(d.Status)))
            return Conflict(new { message = "Nurse is unavailable. The approved case remains in the waiting queue." });
        // Location/ETA are only estimates; never substitute a fabricated location.
        var eta = nurse.LocationRecordedAt >= DateTime.UtcNow.AddMinutes(-5)
            ? (int)Math.Ceiling((await routes.GetRouteMetricsAsync(nurse.CurrentLat, nurse.CurrentLng, triage.Latitude.Value, triage.Longitude.Value)).EtaMinutes)
            : 0;
        var verdict = await safety.EvaluateDispatchSafetyAsync(new ValidationAgentRequest
        { TriageId = triage.Id, SeverityScore = triage.RiskScore, RecommendedNurseId = nurse.Id, EtaMinutes = eta });
        if (eta == 0) verdict.FlaggedRules.Add("Current nurse location is unavailable; ETA is unknown.");
        if (verdict.FlaggedRules.Count > 0 && !request.AcknowledgeSafetyFlags)
            return Conflict(new { message = "Review the safety warnings and explicitly acknowledge before assigning.", flaggedRules = verdict.FlaggedRules });
        // Recheck decision and availability after the external safety call. xmin and unique indexes
        // protect the subsequent atomic SaveChanges against simultaneous requests.
        await db.Entry(triage).ReloadAsync();
        await db.Entry(nurse).ReloadAsync();
        if (triage.IsDeleted || triage.Status != TriageConstants.StatusApproved || nurse.IsDeleted || !nurse.IsAvailable)
            return Conflict(new { message = "The case or nurse changed. Refresh and try again." });
        var dispatch = new DispatchTickets
        {
            TriageTicketId = triage.Id, DoctorId = doctor.Id, NurseId = nurse.Id,
            Status = "Assigned", AssignedAt = DateTime.UtcNow,
            DestinationLat = triage.Latitude.Value, DestinationLng = triage.Longitude.Value,
            SafetySummary = string.Join("; ", verdict.FlaggedRules.Prepend(verdict.VerdictReason))
        };
        nurse.IsAvailable = false;
        triage.Status = "DISPATCH_ASSIGNED";
        db.DispatchTickets.Add(dispatch);
        await WorkflowProgress.RecordAsync(db, triage.Id, "DispatchAssigned", "AuthorizedAssignment", new { DispatchId = dispatch.Id, NurseId = nurse.Id, Actor = User.UserId(), verdict.FlaggedRules });
        await db.SaveChangesAsync();
        return Ok(dispatch);
    }

    [HttpPut("{id:guid}/location")]
    [Authorize(Roles = "Nurse")]
    public async Task<IActionResult> UpdateLocation(Guid id, NurseLocationUpdateDto request)
    {
        var ticket = await OwnTicket(id);
        if (!ActiveStatuses.Contains(ticket.Status)) return Conflict(new { message = "Dispatch is no longer active." });
        var nurse = await db.NurseProfiles.SingleAsync(n => n.Id == ticket.NurseId);
        if (ticket.Status == "Assigned") ticket.Status = "EnRoute";
        var log = new RouteLogs { DispatchTicketId = id, Latitude = request.Latitude, Longitude = request.Longitude,
            SpeedKmh = request.SpeedKmh, Heading = request.Heading, RecordedAt = DateTime.UtcNow };
        db.RouteLogs.Add(log);
        nurse.CurrentLat = request.Latitude;
        nurse.CurrentLng = request.Longitude;
        nurse.LocationRecordedAt = log.RecordedAt;
        await db.SaveChangesAsync();
        return Ok(new { ticket.Status, log.RecordedAt });
    }

    [HttpPut("{id:guid}/arrive")]
    [Authorize(Roles = "Nurse")]
    public async Task<IActionResult> Arrive(Guid id)
    {
        var ticket = await OwnTicket(id);
        if (ticket.Status == "ArrivedOnSite") return Ok(new { ticket.Status });
        if (ticket.Status != "EnRoute") return Conflict(new { message = "Start the route before recording arrival." });
        ticket.Status = "ArrivedOnSite";
        ticket.ArrivedAt = DateTime.UtcNow;
        await WorkflowProgress.RecordAsync(db, ticket.TriageTicketId, "ArrivedOnSite", "NurseArrival", new { Actor = User.UserId() });
        await db.SaveChangesAsync();
        return Ok(new { ticket.Status });
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveDispatches(int page = 1, int pageSize = 50)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100) return BadRequest(new { message = "Use page >= 1 and pageSize 1–100." });
        var userId = User.UserId();
        var query = db.DispatchTickets.AsNoTracking().Where(d => ActiveStatuses.Contains(d.Status));
        if (User.IsInRole("Nurse")) query = query.Where(d => db.NurseProfiles.Any(n => n.Id == d.NurseId && n.UserId == userId));
        var totalItems = await query.CountAsync();
        var tickets = await (from d in query
            join n in db.NurseProfiles on d.NurseId equals n.Id
            orderby d.AssignedAt descending
            select new { d.Id, d.NurseId, NurseName = n.FullName, d.TriageTicketId, d.Status, d.AssignedAt,
                d.IsEscalated, d.EscalationNotes, d.DestinationLat, d.DestinationLng, n.CurrentLat, n.CurrentLng, n.LocationRecordedAt, d.SafetySummary })
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new { totalItems, page, pageSize, tickets });
    }

    [HttpGet("{id:guid}/vitals")]
    public async Task<IActionResult> GetVitals(Guid id)
    {
        if (User.IsInRole("Nurse")) await OwnTicket(id);
        else if (!await db.DispatchTickets.AnyAsync(d => d.Id == id)) return NotFound();
        return Ok(await db.OnSiteVitalsRecords.AsNoTracking().Where(v => v.DispatchTicketId == id).ToListAsync());
    }

    [HttpPost("{id:guid}/escalate")]
    [Authorize(Roles = "Nurse")]
    public async Task<IActionResult> Escalate(Guid id, ApproveTriageRequestDto request)
    {
        var ticket = await OwnTicket(id);
        if (!ActiveStatuses.Contains(ticket.Status)) return Conflict(new { message = "Dispatch is not active." });
        ticket.IsEscalated = true;
        ticket.EscalationNotes = request.Notes;
        await WorkflowProgress.RecordAsync(db, ticket.TriageTicketId, "Escalated", "NurseEscalation", new { Actor = User.UserId(), request.Notes });
        await db.SaveChangesAsync();
        return Ok(new { message = "Escalation recorded on the staff dashboard. Contact the supervising doctor directly for an immediate response." });
    }

    [HttpPost("{id:guid}/complete-onsite")]
    [Authorize(Roles = "Nurse")]
    public async Task<IActionResult> CompleteOnsite(Guid id, CompleteOnsiteDto request)
    {
        var ticket = await OwnTicket(id);
        if (ticket.Status == "Completed")
            return Ok(new { message = "Visit already completed.", vitals = await db.OnSiteVitalsRecords.SingleAsync(v => v.DispatchTicketId == id) });
        if (ticket.Status != "ArrivedOnSite") return Conflict(new { message = "Record arrival before completing this visit." });
        var vitals = new OnSiteVitalsRecords { DispatchTicketId = id, HeartRate = request.HeartRate,
            BloodPressure = request.BloodPressure, BodyTempC = request.BodyTempC,
            OxygenSaturation = request.OxygenSaturation, ClinicalNotes = request.ClinicalNotes.Trim(), RecordedAt = DateTime.UtcNow };
        db.OnSiteVitalsRecords.Add(vitals);
        ticket.Status = "Completed";
        ticket.CompletedAt = DateTime.UtcNow;
        var nurse = await db.NurseProfiles.SingleAsync(n => n.Id == ticket.NurseId);
        nurse.IsAvailable = true;
        var triage = await db.TriageTickets.SingleAsync(t => t.Id == ticket.TriageTicketId);
        triage.Status = "VISIT_COMPLETED";
        await WorkflowProgress.RecordAsync(db, triage.Id, "Completed", "OnsiteCompleted", new { ticket.Id, Actor = User.UserId() }, terminal: true);
        await db.SaveChangesAsync();
        return Ok(new { message = "On-site visit completed.", vitals });
    }

    private async Task<DispatchTickets> OwnTicket(Guid id)
    {
        var ticket = await db.DispatchTickets.SingleOrDefaultAsync(d => d.Id == id) ?? throw new ApiProblem(404, "Dispatch not found.");
        var userId = User.UserId();
        if (!await db.NurseProfiles.AnyAsync(n => n.Id == ticket.NurseId && n.UserId == userId))
            throw new ApiProblem(403, "This dispatch is assigned to another nurse.");
        return ticket;
    }
}

public class NurseAvailabilityDto
{
    [System.ComponentModel.DataAnnotations.Required]
    public bool? IsAvailable { get; set; }
}
