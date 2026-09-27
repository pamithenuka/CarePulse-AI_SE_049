using CarePulse.Api.DTOs;

namespace CarePulse.Api.Services;

public interface ITriageAiAgent
{
    Task<TriageAssessmentResult> AnalyzeSymptomsAsync(TriageSubmitRequestDto request);
}
