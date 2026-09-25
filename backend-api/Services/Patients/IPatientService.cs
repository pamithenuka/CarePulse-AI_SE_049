using CarePulse.Api.DTOs.Patients;
using CarePulse.Api.Entities.Patients;
using Microsoft.AspNetCore.Http;

namespace CarePulse.Api.Services.Patients;

public enum ServiceErrorType
{
    None,
    NotFound,
    Forbidden,
    Conflict,
    ValidationFailed
}

public class ServiceResult<T>
{
    public bool Succeeded => ErrorType == ServiceErrorType.None;
    public T? Value { get; init; }
    public ServiceErrorType ErrorType { get; init; }
    public string? ErrorMessage { get; init; }

    public static ServiceResult<T> Success(T value) => new() { Value = value, ErrorType = ServiceErrorType.None };
    public static ServiceResult<T> Fail(ServiceErrorType type, string message) => new() { ErrorType = type, ErrorMessage = message };
}

public interface IPatientService
{
    // Profile CRUD
    Task<ServiceResult<PatientProfileDetailDto>> CreateProfileAsync(string userId, CreatePatientProfileDto dto);

    /// <summary>Admin-only: creates the login account and profile together, entirely from the web app.</summary>
    Task<ServiceResult<PatientProfileDetailDto>> RegisterPatientAsync(RegisterPatientDto dto);

    Task<PaginatedResult<PatientListItemDto>> GetPatientsAsync(
        string? search, string? bloodGroup, string? condition, int? minAge, int? maxAge,
        string status, string sortBy, bool descending, int page, int pageSize);

    Task<ServiceResult<PatientProfileDetailDto>> GetProfileDetailAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    /// <summary>Resolves the calling Patient's own profile by their user id, so the mobile
    /// app can find its GUID without a Doctor/Admin-only patient search.</summary>
    Task<ServiceResult<PatientProfileDetailDto>> GetMyProfileAsync(string requestingUserId);

    Task<ServiceResult<PatientProfileDetailDto>> UpdateProfileAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, UpdatePatientProfileDto dto);

    Task<ServiceResult<bool>> DeactivateProfileAsync(Guid patientProfileId, string requestingUserId);

    Task<ServiceResult<bool>> ReactivateProfileAsync(Guid patientProfileId);

    // Medical history CRUD
    Task<ServiceResult<List<MedicalHistoryDto>>> GetMedicalHistoryAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    Task<ServiceResult<List<MedicalHistoryDto>>> AddMedicalHistoryAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, AddMedicalHistoryDto dto);

    Task<ServiceResult<MedicalHistoryDto>> UpdateMedicalHistoryAsync(
        Guid patientProfileId, Guid historyId, string requestingUserId, IList<string> requestingRoles, UpdateMedicalHistoryDto dto);

    Task<ServiceResult<bool>> DeleteMedicalHistoryAsync(Guid patientProfileId, Guid historyId, string requestingUserId, IList<string> requestingRoles);

    // Emergency contact CRUD
    Task<ServiceResult<List<EmergencyContactDto>>> GetEmergencyContactsAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    Task<ServiceResult<List<EmergencyContactDto>>> AddEmergencyContactAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, CreateEmergencyContactDto dto);

    Task<ServiceResult<EmergencyContactDto>> UpdateEmergencyContactAsync(
        Guid patientProfileId, Guid contactId, string requestingUserId, IList<string> requestingRoles, UpdateEmergencyContactDto dto);

    Task<ServiceResult<bool>> DeleteEmergencyContactAsync(Guid patientProfileId, Guid contactId, string requestingUserId, IList<string> requestingRoles);

    // Document CRUD
    Task<ServiceResult<List<MedicalDocumentDto>>> GetDocumentsAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    Task<ServiceResult<MedicalDocumentDto>> UploadDocumentAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, IFormFile file, MedicalDocumentType documentType);

    Task<ServiceResult<MedicalDocumentDto>> UpdateDocumentAsync(
        Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles, UpdateMedicalDocumentDto dto);

    Task<ServiceResult<bool>> DeleteDocumentAsync(Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles);

    Task<ServiceResult<(string AbsolutePath, string FileName, string ContentType)>> GetDocumentFileAsync(
        Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles);

    // Audit — patientProfileId/changedByUserId/from/to are all optional filters for the
    // Clinical History Auditor Viewer (Doctor/Admin, cross-patient by default).
    Task<PaginatedResult<PatientAuditLogDto>> GetAuditLogAsync(
        Guid? patientProfileId, string? changedByUserId, DateTime? from, DateTime? to, int page, int pageSize);

    // Emergency alert (business operation)
    Task<ServiceResult<EmergencyAlertResultDto>> TriggerEmergencyAlertAsync(Guid patientProfileId, string requestingUserId, IList<string> requestingRoles);

    Task<PaginatedResult<EmergencyAlertLogSummaryDto>> GetEmergencyAlertLogsAsync(int page, int pageSize);

    // Agent 1 (Planner) context
    Task<ServiceResult<PatientContextDto>> GetPatientContextAsync(Guid patientProfileId);
}
