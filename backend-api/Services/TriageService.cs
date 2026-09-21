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
}

public class TriageService : ITriageService
{
    private readonly CarePulseDbContext _context;

    public TriageService(CarePulseDbContext context)
    {
        _context = context;
    }

    public async Task<TriageResponseDto> SubmitTriageAsync(TriageSubmitRequestDto request)
    {
        var ticket = new TriageTicket
        {
            PatientId = request.PatientId,
            Symptoms = request.Symptoms
        };

        // Mock AI Risk Assessment
        string symptomsLower = request.Symptoms.ToLower();
        if (symptomsLower.Contains("severe chest pain"))
        {
            ticket.RiskScore = 9;
            ticket.RiskLevel = TriageConstants.RiskHigh;
            ticket.RecommendedAction = TriageConstants.ActionDoctorApproval;
            ticket.Status = TriageConstants.StatusNeedsApproval;
            ticket.RequiresDoctorApproval = true;
            ticket.FollowUpRecommended = true;
        }
        else if (symptomsLower.Contains("fever and headache"))
        {
            ticket.RiskScore = 5;
            ticket.RiskLevel = TriageConstants.RiskMedium;
            ticket.RecommendedAction = TriageConstants.ActionConsultation;
            ticket.Status = TriageConstants.StatusConsultationRecommended;
            ticket.RequiresDoctorApproval = false;
            ticket.FollowUpRecommended = true;
        }
        else
        {
            ticket.RiskScore = 2;
            ticket.RiskLevel = TriageConstants.RiskLow;
            ticket.RecommendedAction = TriageConstants.ActionSelfCare;
            ticket.Status = TriageConstants.StatusCompleted;
            ticket.RequiresDoctorApproval = false;
            ticket.FollowUpRecommended = false;
        }

        await _context.TriageTickets.AddAsync(ticket);

        // Attach Risk Assessment
        var riskAssessment = new RiskAssessment
        {
            TriageTicket = ticket,
            Score = ticket.RiskScore,
            Level = ticket.RiskLevel,
            RecommendedAction = ticket.RecommendedAction,
            Reason = "Mock assessment logic evaluated this case."
        };
        await _context.RiskAssessments.AddAsync(riskAssessment);

        // Log generation based on risk
        string logMessage = ticket.RiskLevel switch
        {
            TriageConstants.RiskHigh => $"Symptoms submitted\nRisk assessed: HIGH ({ticket.RiskScore}/10)\nDoctor approval required\nAdded to approval queue",
            TriageConstants.RiskMedium => $"Symptoms submitted\nRisk assessed: MEDIUM ({ticket.RiskScore}/10)\nDoctor consultation recommended\nDoctor approval not required",
            _ => $"Symptoms submitted\nRisk assessed: LOW ({ticket.RiskScore}/10)\nSelf-care monitoring recommended\nDoctor approval not required"
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
