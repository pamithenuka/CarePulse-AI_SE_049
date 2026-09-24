namespace CarePulse.Api.DTOs;

public class TriageAssessmentResult
{
    public int RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool FollowUpRecommended { get; set; }
}
