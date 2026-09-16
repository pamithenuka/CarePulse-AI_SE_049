namespace CarePulse.Api.DTOs;

public class TriageSubmitRequestDto
{
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
}

public class TriageResponseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
}

public class AiTriageLogDto
{
    public Guid Id { get; set; }
    public Guid TriageTicketId { get; set; }
    public string LogMessage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ApproveTriageRequestDto
{
    public string Notes { get; set; } = string.Empty;
}
