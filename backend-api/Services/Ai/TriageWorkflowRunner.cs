using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.DTOs.Ai;
using CarePulse.Api.Entities.Ai;
using CarePulse.Api.Services.Agents;
using CarePulse.Api.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Ai;

public class TriageWorkflowRunner(CarePulseDbContext db, IAgentPlannerService planner, ITriageService triage,
    ISchedulingAgent scheduling, IValidationAgent safety, ILogger<TriageWorkflowRunner> logger)
{
    public async Task<TriageResponseDto> RunAsync(TriageSubmitRequestDto request, ClaimsPrincipal user)
    {
        var clock = Stopwatch.StartNew();
        var planResult = await planner.CreatePlanAsync(request.PatientProfileId, user.UserId(),
            user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList(), new CreateAiPlanDto { Objective = request.Symptoms });
        if (!planResult.Succeeded) throw new ApiProblem(400, planResult.ErrorMessage ?? "Unable to create workflow.");
        var workflow = await db.AiWorkflows.SingleAsync(w => w.Id == planResult.Value!.Id);
        workflow.ExecutionStatus = "Running";
        workflow.ExecutionJson = JsonSerializer.Serialize(new[] { new WorkflowEvent("Planner", DateTime.UtcNow,
            new { workflow.Status, workflow.ErrorMessage }, clock.ElapsedMilliseconds) });
        await db.SaveChangesAsync();
        try
        {
            clock.Restart();
            var ticket = await triage.SubmitTriageAsync(request);
            workflow.TriageTicketId = ticket.Id;
            await WorkflowProgress.RecordAsync(db, ticket.Id, "Running", "DomainAnalysis", new
            { ticket.RiskScore, ticket.RiskLevel, ticket.AssessmentFailed, DurationMs = clock.ElapsedMilliseconds });
            // Persist the link before external calls. A restart can expose unfinished work to staff.
            await db.SaveChangesAsync();
            clock.Restart();
            var slots = await scheduling.RecommendAsync(ticket.RecommendedSpecialty);
            await WorkflowProgress.RecordAsync(db, ticket.Id, "Running", "ActionTool", new
            { Tool = "search_available_slots", Specialty = ticket.RecommendedSpecialty, Slots = slots, DurationMs = clock.ElapsedMilliseconds });
            await db.SaveChangesAsync();
            clock.Restart();
            var verdict = await safety.EvaluateDispatchSafetyAsync(new ValidationAgentRequest
            { TriageId = ticket.Id, SeverityScore = ticket.RiskScore, EtaMinutes = 0 });
            var failed = workflow.Status != AiWorkflowStatus.PlanCreated || ticket.AssessmentFailed;
            workflow.StepsJson = JsonSerializer.Serialize(new[] {
                new AiPlanStepDto { Agent = "DomainAnalysis", Task = "Assess reported symptoms", Status = ticket.AssessmentFailed ? "Failed" : "Completed" },
                new AiPlanStepDto { Agent = "ActionTool", Task = "Find available specialty slots", Status = "Completed" },
                new AiPlanStepDto { Agent = "Validation", Task = "Validate safety and approval gate", Status = "Completed" }
            });
            if (failed)
            {
                var entity = await db.TriageTickets.Include(t => t.ApprovalQueue).SingleAsync(t => t.Id == ticket.Id);
                entity.RequiresDoctorApproval = true;
                entity.Status = TriageConstants.StatusNeedsApproval;
                if (entity.ApprovalQueue == null) db.ApprovalQueues.Add(new Entities.ApprovalQueue { TriageTicketId = entity.Id, ReviewStatus = "PENDING" });
                ticket.RequiresDoctorApproval = true;
                ticket.Status = entity.Status;
            }
            var status = failed ? "ManualReviewRequired" : ticket.RequiresDoctorApproval ? "AwaitingApproval" : "Completed";
            await WorkflowProgress.RecordAsync(db, ticket.Id, status, "Validation", new
            { verdict.RequiresHumanApproval, verdict.FlaggedRules, DurationMs = clock.ElapsedMilliseconds }, terminal: status == "Completed");
            await db.SaveChangesAsync();
            ticket.WorkflowId = workflow.Id;
            ticket.WorkflowStatus = status;
            return ticket;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Workflow {WorkflowId} failed", workflow.Id);
            workflow.ExecutionStatus = "Failed";
            workflow.ErrorMessage = "A workflow step failed. Staff review is required; no dispatch was executed.";
            workflow.FinishedAt = DateTime.UtcNow;
            if (workflow.TriageTicketId is Guid id)
            {
                var entity = await db.TriageTickets.Include(t => t.ApprovalQueue).SingleAsync(t => t.Id == id);
                entity.RequiresDoctorApproval = true;
                entity.Status = TriageConstants.StatusNeedsApproval;
                if (entity.ApprovalQueue == null) db.ApprovalQueues.Add(new Entities.ApprovalQueue { TriageTicketId = id, ReviewStatus = "PENDING" });
                await WorkflowProgress.RecordAsync(db, id, "Failed", "SafeFailure", new { workflow.ErrorMessage }, terminal: true);
            }
            await db.SaveChangesAsync();
            throw new ApiProblem(503, $"Workflow {workflow.Id} could not finish. No dispatch was executed; check your case history before retrying.");
        }
    }
}
