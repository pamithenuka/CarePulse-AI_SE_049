using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Ai;

// The bounded synchronous workflow takes less than ten minutes. Never replay a
// side effect after a crash: expose unfinished cases for an authorized review.
public class WorkflowRecoveryService(IServiceScopeFactory scopes, ILogger<WorkflowRecoveryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                await RecoverAsync(scope.ServiceProvider.GetRequiredService<CarePulseDbContext>(), DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception ex) { logger.LogWarning(ex, "Workflow recovery deferred; will retry next minute."); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public static async Task RecoverAsync(CarePulseDbContext db, DateTime now, CancellationToken token = default)
    {
        var cutoff = now.AddMinutes(-10);
        var interrupted = await db.AiWorkflows.Where(w => w.ExecutionStatus == "Running" && w.CreatedAt < cutoff).ToListAsync(token);
        foreach (var workflow in interrupted)
        {
            workflow.ExecutionStatus = "ManualReviewRequired";
            workflow.ErrorMessage = "Processing was interrupted. Review the saved case before taking further action.";
            workflow.FinishedAt = now;
            if (workflow.TriageTicketId is Guid id)
            {
                var ticket = await db.TriageTickets.Include(t => t.ApprovalQueue).SingleOrDefaultAsync(t => t.Id == id, token);
                if (ticket != null && !await db.DispatchTickets.AnyAsync(d => d.TriageTicketId == id, token))
                {
                    ticket.RequiresDoctorApproval = true;
                    ticket.Status = TriageConstants.StatusNeedsApproval;
                    if (ticket.ApprovalQueue == null) db.ApprovalQueues.Add(new ApprovalQueue { TriageTicketId = id, ReviewStatus = "PENDING" });
                    await WorkflowProgress.RecordAsync(db, id, "ManualReviewRequired", "InterruptedWorkflowRecovered", new { workflow.ErrorMessage }, terminal: true);
                }
            }
        }
        // xmin prevents another worker or a doctor's action from being overwritten.
        await db.SaveChangesAsync(token);
    }
}
