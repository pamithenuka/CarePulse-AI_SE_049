namespace CarePulse.Api.Entities;

public class TriageTicket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

public class AiTriageLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TriageTicketId { get; set; }
    public string LogMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
