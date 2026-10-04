using System.Text.Json;
using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Ai;

public record WorkflowEvent(string Step, DateTime At, object Result, long? DurationMs = null);

public static class WorkflowProgress
{
    // Stage state and event together; the caller saves them atomically with its business command.
    public static async Task RecordAsync(CarePulseDbContext db, Guid triageId, string status, string step, object result, bool terminal = false)
    {
        var workflow = db.AiWorkflows.Local.SingleOrDefault(w => w.TriageTicketId == triageId)
            ?? await db.AiWorkflows.SingleOrDefaultAsync(w => w.TriageTicketId == triageId);
        if (workflow == null) return;
        var events = JsonSerializer.Deserialize<List<WorkflowEvent>>(workflow.ExecutionJson) ?? [];
        events.Add(new WorkflowEvent(step, DateTime.UtcNow, result));
        workflow.ExecutionJson = JsonSerializer.Serialize(events);
        workflow.ExecutionStatus = status;
        if (terminal) workflow.FinishedAt = DateTime.UtcNow;
    }
}
