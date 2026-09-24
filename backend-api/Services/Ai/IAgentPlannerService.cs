using CarePulse.Api.DTOs.Ai;
using CarePulse.Api.Services.Patients;

namespace CarePulse.Api.Services.Ai;

public interface IAgentPlannerService
{
    Task<ServiceResult<AiWorkflowDto>> CreatePlanAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, CreateAiPlanDto dto);

    Task<ServiceResult<List<AiWorkflowDto>>> GetPlansAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    Task<ServiceResult<AiWorkflowDto>> ReviewPlanAsync(
        Guid patientProfileId, Guid workflowId, string requestingUserId, ReviewAiPlanDto dto);
}
