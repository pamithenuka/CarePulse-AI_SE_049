using CarePulse.Api.Services.Ai;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services;

public interface ITriageService
{
    Task<TriageResponseDto> SubmitTriageAsync(TriageSubmitRequestDto request);
    Task<IEnumerable<TriageResponseDto>> GetPendingApprovalsAsync();
    Task<IEnumerable<AiTriageLogDto>> GetAuditLogAsync(Guid triageId);
    Task<bool> DeleteTriageAsync(Guid triageId);
    Task<bool> ApproveTriageAsync(Guid triageId, ApproveTriageRequestDto request, string reviewedByDoctorId);
    Task<bool> RejectTriageAsync(Guid triageId, ApproveTriageRequestDto request, string? reviewedByDoctorId);
}

public class TriageService : ITriageService
{
    private readonly CarePulseDbContext _context;
    private readonly ITriageAiAgent _aiAgent;

    public TriageService(CarePulseDbContext context, ITriageAiAgent aiAgent)
    {
        _context = context;
        _aiAgent = aiAgent;
    }

    public async Task<TriageResponseDto> SubmitTriageAsync(TriageSubmitRequestDto request)
    {
        var ticket = new TriageTicket
        {
            PatientProfileId = request.PatientProfileId,
            Symptoms = request.Symptoms.Trim(),
            Latitude = request.Latitude, Longitude = request.Longitude
        };

        // AI Risk Assessment
        var aiResult = await _aiAgent.AnalyzeSymptomsAsync(request);

        // A self-reported severe case cannot be downgraded by a model response.
        if (string.Equals(request.Severity, "Severe", StringComparison.OrdinalIgnoreCase))
        {
            aiResult.RiskScore = Math.Max(aiResult.RiskScore, 7);
            aiResult.RiskLevel = TriageConstants.RiskHigh;
            aiResult.RecommendedAction = TriageConstants.ActionDoctorApproval;
        }
        ticket.AssessmentFailed = aiResult.AssessmentFailed;
        ticket.RiskScore = aiResult.RiskScore;
        ticket.RiskLevel = aiResult.RiskLevel;
        ticket.RecommendedAction = aiResult.RecommendedAction;
        ticket.Reason = aiResult.Reason;
        ticket.FollowUpRecommended = aiResult.FollowUpRecommended;
        ticket.RecommendedSpecialty = aiResult.RecommendedSpecialty;

        // Apply Backend Business Rules (Doctor Approval override for HIGH risk)
        ticket.RequiresDoctorApproval = ticket.RiskLevel == TriageConstants.RiskHigh;
        ticket.Status = ticket.RequiresDoctorApproval ? TriageConstants.StatusNeedsApproval : TriageConstants.StatusCompleted;
        if (ticket.RiskLevel == TriageConstants.RiskMedium)
        {
            ticket.Status = TriageConstants.StatusConsultationRecommended;
        }

        await _context.TriageTickets.AddAsync(ticket);

        // Attach Risk Assessment
        var riskAssessment = new RiskAssessment
        {
            TriageTicket = ticket,
            Score = ticket.RiskScore,
            Level = ticket.RiskLevel,
            RecommendedAction = ticket.RecommendedAction,
            Reason = aiResult.Reason
        };
        await _context.RiskAssessments.AddAsync(riskAssessment);

        // Log generation based on risk
        string logMessage = ticket.RiskLevel switch
        {
            TriageConstants.RiskHigh => $"Symptoms analyzed by AI\nRisk assessed: HIGH ({ticket.RiskScore}/10)\nReason: {aiResult.Reason}\nDoctor approval required\nAdded to approval queue",
            TriageConstants.RiskMedium => $"Symptoms analyzed by AI\nRisk assessed: MEDIUM ({ticket.RiskScore}/10)\nReason: {aiResult.Reason}\nDoctor consultation recommended\nDoctor approval not required",
            _ => $"Symptoms analyzed by AI\nRisk assessed: LOW ({ticket.RiskScore}/10)\nReason: {aiResult.Reason}\nSelf-care monitoring recommended\nDoctor approval not required"
        };

        var initialLog = new AiTriageLog
        {
            TriageTicket = ticket,
            LogMessage = logMessage
        };
        await _context.AiTriageLogs.AddAsync(initialLog);

        // Create ApprovalQueue record ONLY for HIGH cases
        if (ticket.RequiresDoctorApproval)
        {
            var approvalQueue = new ApprovalQueue
            {
                TriageTicket = ticket,
                ReviewStatus = "PENDING"
            };
            await _context.ApprovalQueues.AddAsync(approvalQueue);
        }

        await _context.SaveChangesAsync();

        return MapToDto(ticket);
    }

    public async Task<IEnumerable<TriageResponseDto>> GetPendingApprovalsAsync()
    {
        var tickets = await _context.TriageTickets
            .Where(t => t.RequiresDoctorApproval && t.Status == TriageConstants.StatusNeedsApproval)
            .ToListAsync();

        return tickets.Select(MapToDto);
    }

    public async Task<IEnumerable<AiTriageLogDto>> GetAuditLogAsync(Guid triageId)
    {
        var logs = await _context.AiTriageLogs
            .IgnoreQueryFilters()
            .Where(l => l.TriageTicketId == triageId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync();

        return logs.Select(l => new AiTriageLogDto
        {
            Id = l.Id,
            TriageTicketId = l.TriageTicketId,
            LogMessage = l.LogMessage,
            CreatedAt = l.CreatedAt
        });
    }

    public async Task<bool> DeleteTriageAsync(Guid triageId)
    {
        var ticket = await _context.TriageTickets.FirstOrDefaultAsync(t => t.Id == triageId);
        if (ticket == null) return false;

        ticket.IsDeleted = true;

        var deletionLog = new AiTriageLog
        {
            TriageTicketId = ticket.Id,
            LogMessage = "Triage ticket deleted."
        };
        await _context.AiTriageLogs.AddAsync(deletionLog);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ApproveTriageAsync(Guid triageId, ApproveTriageRequestDto request, string reviewedByDoctorId)
    {
        var ticket = await _context.TriageTickets
            .Include(t => t.ApprovalQueue)
            .FirstOrDefaultAsync(t => t.Id == triageId);

        if (ticket == null || ticket.Status != "NEEDS_DOCTOR_APPROVAL" ||
            await _context.AiWorkflows.AnyAsync(w => w.TriageTicketId == triageId && w.ExecutionStatus == "Running"))
            return false;

        ticket.Status = "APPROVED_BY_DOCTOR";

        if (ticket.ApprovalQueue != null)
        {
            ticket.ApprovalQueue.ReviewStatus = "APPROVED";
            ticket.ApprovalQueue.ReviewedAt = DateTime.UtcNow;
            ticket.ApprovalQueue.ReviewedByDoctorId = reviewedByDoctorId;
        }

        var approvalLog = new AiTriageLog
        {
            TriageTicketId = ticket.Id,
            LogMessage = $"Triage ticket approved by doctor. Notes: {request.Notes}"
        };
        await _context.AiTriageLogs.AddAsync(approvalLog);

        await WorkflowProgress.RecordAsync(_context, triageId, "AwaitingDispatch", "DoctorApproved", new { Actor = reviewedByDoctorId, request.Notes });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectTriageAsync(Guid triageId, ApproveTriageRequestDto request, string? reviewedByDoctorId)
    {
        var ticket = await _context.TriageTickets
            .Include(t => t.ApprovalQueue)
            .FirstOrDefaultAsync(t => t.Id == triageId);

        if (ticket == null || ticket.Status != TriageConstants.StatusNeedsApproval ||
            await _context.AiWorkflows.AnyAsync(w => w.TriageTicketId == triageId && w.ExecutionStatus == "Running"))
            return false;

        ticket.Status = TriageConstants.StatusRejected;

        if (ticket.ApprovalQueue != null)
        {
            ticket.ApprovalQueue.ReviewStatus = TriageConstants.StatusRejected;
            ticket.ApprovalQueue.ReviewedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(reviewedByDoctorId))
            {
                ticket.ApprovalQueue.ReviewedByDoctorId = reviewedByDoctorId;
            }
        }

        var rejectionLog = new AiTriageLog
        {
            TriageTicketId = ticket.Id,
            LogMessage = $"Triage ticket rejected by doctor. Notes: {request.Notes}"
        };
        await _context.AiTriageLogs.AddAsync(rejectionLog);

        await WorkflowProgress.RecordAsync(_context, triageId, "Rejected", "DoctorRejected", new { Actor = reviewedByDoctorId, request.Notes }, terminal: true);
        await _context.SaveChangesAsync();
        return true;
    }

    private TriageResponseDto MapToDto(TriageTicket ticket)
    {
        return new TriageResponseDto
        {
            Id = ticket.Id,
            AssessmentFailed = ticket.AssessmentFailed, Latitude = ticket.Latitude, Longitude = ticket.Longitude,
            PatientProfileId = ticket.PatientProfileId,
            Symptoms = ticket.Symptoms,
            Status = ticket.Status,
            RiskScore = ticket.RiskScore,
            RiskLevel = ticket.RiskLevel,
            RecommendedAction = ticket.RecommendedAction,
            RequiresDoctorApproval = ticket.RequiresDoctorApproval,
            Reason = ticket.Reason,
            FollowUpRecommended = ticket.FollowUpRecommended,
            RecommendedSpecialty = ticket.RecommendedSpecialty
        };
    }
}
