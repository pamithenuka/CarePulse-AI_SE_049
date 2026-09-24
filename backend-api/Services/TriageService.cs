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
    Task<bool> ApproveTriageAsync(Guid triageId, ApproveTriageRequestDto request);
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
            PatientId = request.PatientId,
            Symptoms = request.Symptoms
        };

        // AI Risk Assessment
        var aiResult = await _aiAgent.AnalyzeSymptomsAsync(request);

        ticket.RiskScore = aiResult.RiskScore;
        ticket.RiskLevel = aiResult.RiskLevel;
        ticket.RecommendedAction = aiResult.RecommendedAction;
        ticket.Reason = aiResult.Reason;
        ticket.FollowUpRecommended = aiResult.FollowUpRecommended;

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

    public async Task<bool> ApproveTriageAsync(Guid triageId, ApproveTriageRequestDto request)
    {
        var ticket = await _context.TriageTickets
            .Include(t => t.ApprovalQueue)
            .FirstOrDefaultAsync(t => t.Id == triageId);

        if (ticket == null || ticket.Status != "NEEDS_DOCTOR_APPROVAL") 
            return false;

        ticket.Status = "APPROVED_BY_DOCTOR";
        
        if (ticket.ApprovalQueue != null)
        {
            ticket.ApprovalQueue.ReviewStatus = "APPROVED";
            ticket.ApprovalQueue.ReviewedAt = DateTime.UtcNow;
            // Optionally set ReviewedByDoctorId if we have the current user's context
        }
        
        var approvalLog = new AiTriageLog 
        { 
            TriageTicketId = ticket.Id, 
            LogMessage = $"Triage ticket approved by doctor. Notes: {request.Notes}" 
        };
        await _context.AiTriageLogs.AddAsync(approvalLog);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectTriageAsync(Guid triageId, ApproveTriageRequestDto request, string? reviewedByDoctorId)
    {
        var ticket = await _context.TriageTickets
            .Include(t => t.ApprovalQueue)
            .FirstOrDefaultAsync(t => t.Id == triageId);

        if (ticket == null || ticket.Status != TriageConstants.StatusNeedsApproval)
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

        await _context.SaveChangesAsync();
        return true;
    }

    private TriageResponseDto MapToDto(TriageTicket ticket)
    {
        return new TriageResponseDto
        {
            Id = ticket.Id,
            PatientId = ticket.PatientId,
            Symptoms = ticket.Symptoms,
            Status = ticket.Status,
            RiskScore = ticket.RiskScore,
            RiskLevel = ticket.RiskLevel,
            RecommendedAction = ticket.RecommendedAction,
            RequiresDoctorApproval = ticket.RequiresDoctorApproval,
            Reason = ticket.Reason,
            FollowUpRecommended = ticket.FollowUpRecommended
        };
    }
}
