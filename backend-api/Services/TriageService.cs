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
            ticket.RiskLevel = "HIGH";
            ticket.Status = "NEEDS_DOCTOR_APPROVAL";
        }
        else if (symptomsLower.Contains("difficulty breathing"))
        {
            ticket.RiskScore = 8;
            ticket.RiskLevel = "HIGH";
            ticket.Status = "NEEDS_DOCTOR_APPROVAL";
        }
        else if (symptomsLower.Contains("moderate symptoms"))
        {
            ticket.RiskScore = 6;
            ticket.RiskLevel = "MEDIUM";
            ticket.Status = "PENDING_DISPATCH";
        }
        else if (symptomsLower.Contains("mild headache"))
        {
            ticket.RiskScore = 3;
            ticket.RiskLevel = "LOW";
            ticket.Status = "PENDING_DISPATCH";
        }
        else
        {
            ticket.RiskScore = 5;
            ticket.RiskLevel = "MEDIUM";
            ticket.Status = "PENDING_DISPATCH";
        }

        _tickets.Add(ticket);
        
        _logs.Add(new AiTriageLog 
        { 
            TriageTicketId = ticket.Id, 
            LogMessage = $"Triage submitted. Symptoms analyzed. Risk Level: {ticket.RiskLevel}, Score: {ticket.RiskScore}." 
        });

        return MapToDto(ticket);
    }

    public IEnumerable<TriageResponseDto> GetPendingApprovals()
    {
        return _tickets
            .Where(t => t.Status == "NEEDS_DOCTOR_APPROVAL" && !t.IsDeleted)
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
            RiskLevel = ticket.RiskLevel
        };
    }
}
