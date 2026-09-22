using CarePulse.Api.DTOs.Ai;
using CarePulse.Api.Services.Patients;

namespace CarePulse.Api.Services.Ai;

public interface IAgentPlannerService
{
    Task<ServiceResult<AiWorkflowDto>> CreatePlanAsync(Guid patientProfileId, string requestingUserId, CreateAiPlanDto dto);

    Task<ServiceResult<List<AiWorkflowDto>>> GetPlansAsync(Guid patientProfileId);

    Task<ServiceResult<AiWorkflowDto>> ReviewPlanAsync(
        Guid patientProfileId, Guid workflowId, string requestingUserId, ReviewAiPlanDto dto);
}
