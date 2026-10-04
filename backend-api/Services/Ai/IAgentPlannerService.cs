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

    /// <summary>Cross-patient inbox for the mobile Doctor review flow: every plan
    /// still awaiting a Doctor/Admin decision, across all patients. Controller
    /// enforces Doctor/Admin-only; no ownership check needed here since it never
    /// takes a specific patient as input.</summary>
    Task<ServiceResult<List<AiWorkflowDto>>> GetPendingReviewPlansAsync();
}
