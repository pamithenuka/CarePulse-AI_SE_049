using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;

namespace CarePulse.Api.Services;

public interface ITriageService
{
    TriageResponseDto SubmitTriage(TriageSubmitRequestDto request);
    IEnumerable<TriageResponseDto> GetPendingApprovals();
    IEnumerable<AiTriageLogDto> GetAuditLog(Guid triageId);
    bool DeleteTriage(Guid triageId);
    bool ApproveTriage(Guid triageId, ApproveTriageRequestDto request);
}

public class TriageService : ITriageService
{
    private readonly List<TriageTicket> _tickets = new();
    private readonly List<AiTriageLog> _logs = new();

    public TriageResponseDto SubmitTriage(TriageSubmitRequestDto request)
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

        _tickets.Add(ticket);
        
        // Log generation based on risk
        string logMessage = ticket.RiskLevel switch
        {
            TriageConstants.RiskHigh => $"Symptoms submitted\nRisk assessed: HIGH ({ticket.RiskScore}/10)\nDoctor approval required\nAdded to approval queue",
            TriageConstants.RiskMedium => $"Symptoms submitted\nRisk assessed: MEDIUM ({ticket.RiskScore}/10)\nDoctor consultation recommended\nDoctor approval not required",
            _ => $"Symptoms submitted\nRisk assessed: LOW ({ticket.RiskScore}/10)\nSelf-care monitoring recommended\nDoctor approval not required"
        };

        _logs.Add(new AiTriageLog 
        { 
            TriageTicketId = ticket.Id, 
            LogMessage = logMessage 
        });

        return MapToDto(ticket);
    }

    public IEnumerable<TriageResponseDto> GetPendingApprovals()
    {
        return _tickets
            .Where(t => t.RequiresDoctorApproval && t.Status == TriageConstants.StatusNeedsApproval && !t.IsDeleted)
            .Select(MapToDto);
    }

    public IEnumerable<AiTriageLogDto> GetAuditLog(Guid triageId)
    {
        return _logs
            .Where(l => l.TriageTicketId == triageId)
            .Select(l => new AiTriageLogDto
            {
                Id = l.Id,
                TriageTicketId = l.TriageTicketId,
                LogMessage = l.LogMessage,
                CreatedAt = l.CreatedAt
            });
    }

    public bool DeleteTriage(Guid triageId)
    {
        var ticket = _tickets.FirstOrDefault(t => t.Id == triageId && !t.IsDeleted);
        if (ticket == null) return false;

        ticket.IsDeleted = true;
        _logs.Add(new AiTriageLog 
        { 
            TriageTicketId = ticket.Id, 
            LogMessage = "Triage ticket deleted." 
        });
        return true;
    }

    public bool ApproveTriage(Guid triageId, ApproveTriageRequestDto request)
    {
        var ticket = _tickets.FirstOrDefault(t => t.Id == triageId && !t.IsDeleted);
        if (ticket == null) return false;

        if (ticket.Status != "NEEDS_DOCTOR_APPROVAL") return false;

        ticket.Status = "APPROVED_BY_DOCTOR";
        
        _logs.Add(new AiTriageLog 
        { 
            TriageTicketId = ticket.Id, 
            LogMessage = $"Triage ticket approved by doctor. Notes: {request.Notes}" 
        });

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
