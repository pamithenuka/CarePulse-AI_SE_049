using System.Security.Claims;
using CarePulse.Api.DTOs.Patients;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Patients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Controllers.V1;

[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IList<string> CurrentRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
    private bool IsAdmin => CurrentRoles.Contains("Admin");

    // ---------- Profile ----------

    /// <summary>Patient Master Registry: search, filter, sort and paginate patients (Doctor/Admin).</summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<PaginatedResult<PatientListItemDto>>> GetPatients(
        [FromQuery] string? search,
        [FromQuery] string? bloodGroup,
        [FromQuery] string? condition,
        [FromQuery] int? minAge,
        [FromQuery] int? maxAge,
        [FromQuery] string status = "active",
        [FromQuery] string sortBy = "fullName",
        [FromQuery] bool descending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Only Admins may look at deactivated patients.
        var effectiveStatus = IsAdmin ? status : "active";
        var result = await _patientService.GetPatientsAsync(search, bloodGroup, condition, minAge, maxAge, effectiveStatus, sortBy, descending, page, pageSize);
        return Ok(result);
    }

    [HttpPost("profile")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<PatientProfileDetailDto>> CreateProfile(CreatePatientProfileDto dto)
    {
        var result = await _patientService.CreateProfileAsync(CurrentUserId, dto);
        return ToActionResult(result, value => CreatedAtAction(nameof(GetProfile), new { id = value.Id }, value));
    }

    /// <summary>Admin-only: onboard a new patient (login account + profile) entirely from the web app.</summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<PatientProfileDetailDto>> RegisterPatient(RegisterPatientDto dto)
    {
        var result = await _patientService.RegisterPatientAsync(dto);
        return ToActionResult(result, value => CreatedAtAction(nameof(GetProfile), new { id = value.Id }, value));
    }

    /// <summary>Lets the mobile app resolve the logged-in Patient's own profile GUID
    /// without a Doctor/Admin-only search. 404 means they haven't onboarded yet.</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Patient")]
    public async Task<ActionResult<PatientProfileDetailDto>> GetMyProfile()
    {
        var result = await _patientService.GetMyProfileAsync(CurrentUserId);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<PatientProfileDetailDto>> GetProfile(Guid id)
    {
        var result = await _patientService.GetProfileDetailAsync(id, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult<PatientProfileDetailDto>> UpdateProfile(Guid id, UpdatePatientProfileDto dto)
    {
        var result = await _patientService.UpdateProfileAsync(id, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    /// <summary>Soft delete: deactivates the patient. Admin only.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeactivateProfile(Guid id)
    {
        var result = await _patientService.DeactivateProfileAsync(id, CurrentUserId);
        return ToPlainActionResult(result, _ => NoContent());
    }

    [HttpPut("{id:guid}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ReactivateProfile(Guid id)
    {
        var result = await _patientService.ReactivateProfileAsync(id);
        return ToPlainActionResult(result, _ => NoContent());
    }

    // ---------- Medical history ----------

    [HttpGet("{id:guid}/history")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<List<MedicalHistoryDto>>> GetMedicalHistory(Guid id)
    {
        var result = await _patientService.GetMedicalHistoryAsync(id, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPost("{id:guid}/history")]
    [Authorize(Roles = "Doctor,Patient")]
    public async Task<ActionResult<List<MedicalHistoryDto>>> AddMedicalHistory(Guid id, AddMedicalHistoryDto dto)
    {
        var result = await _patientService.AddMedicalHistoryAsync(id, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPut("{id:guid}/history/{historyId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<MedicalHistoryDto>> UpdateMedicalHistory(Guid id, Guid historyId, UpdateMedicalHistoryDto dto)
    {
        var result = await _patientService.UpdateMedicalHistoryAsync(id, historyId, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpDelete("{id:guid}/history/{historyId:guid}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult> DeleteMedicalHistory(Guid id, Guid historyId)
    {
        var result = await _patientService.DeleteMedicalHistoryAsync(id, historyId, CurrentUserId, CurrentRoles);
        return ToPlainActionResult(result, _ => NoContent());
    }

    // ---------- Emergency contacts ----------

    [HttpGet("{id:guid}/emergency-contacts")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<List<EmergencyContactDto>>> GetEmergencyContacts(Guid id)
    {
        var result = await _patientService.GetEmergencyContactsAsync(id, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPost("{id:guid}/emergency-contacts")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult<List<EmergencyContactDto>>> AddEmergencyContact(Guid id, CreateEmergencyContactDto dto)
    {
        var result = await _patientService.AddEmergencyContactAsync(id, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPut("{id:guid}/emergency-contacts/{contactId:guid}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult<EmergencyContactDto>> UpdateEmergencyContact(Guid id, Guid contactId, UpdateEmergencyContactDto dto)
    {
        var result = await _patientService.UpdateEmergencyContactAsync(id, contactId, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpDelete("{id:guid}/emergency-contacts/{contactId:guid}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult> DeleteEmergencyContact(Guid id, Guid contactId)
    {
        var result = await _patientService.DeleteEmergencyContactAsync(id, contactId, CurrentUserId, CurrentRoles);
        return ToPlainActionResult(result, _ => NoContent());
    }

    // ---------- Documents ----------

    [HttpGet("{id:guid}/documents")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<ActionResult<List<MedicalDocumentDto>>> GetDocuments(Guid id)
    {
        var result = await _patientService.GetDocumentsAsync(id, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPost("{id:guid}/documents")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult<MedicalDocumentDto>> UploadDocument(
        Guid id, IFormFile file, [FromForm] MedicalDocumentType documentType = MedicalDocumentType.Other)
    {
        var result = await _patientService.UploadDocumentAsync(id, CurrentUserId, CurrentRoles, file, documentType);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpPut("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult<MedicalDocumentDto>> UpdateDocument(Guid id, Guid documentId, UpdateMedicalDocumentDto dto)
    {
        var result = await _patientService.UpdateDocumentAsync(id, documentId, CurrentUserId, CurrentRoles, dto);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<ActionResult> DeleteDocument(Guid id, Guid documentId)
    {
        var result = await _patientService.DeleteDocumentAsync(id, documentId, CurrentUserId, CurrentRoles);
        return ToPlainActionResult(result, _ => NoContent());
    }

    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId)
    {
        var result = await _patientService.GetDocumentFileAsync(id, documentId, CurrentUserId, CurrentRoles);
        if (!result.Succeeded)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Forbidden => Forbid(),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }

        var (absolutePath, fileName, contentType) = result.Value;
        return PhysicalFile(absolutePath, contentType, fileName);
    }

    // ---------- Audit log ----------

    [HttpGet("{id:guid}/audit-log")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<PaginatedResult<PatientAuditLogDto>>> GetPatientAuditLog(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _patientService.GetAuditLogAsync(id, null, null, null, page, pageSize);
        return Ok(result);
    }

    /// <summary>Clinical History Auditor Viewer: cross-patient audit trail with filters.</summary>
    [HttpGet("audit-log")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<PaginatedResult<PatientAuditLogDto>>> GetAuditLog(
        [FromQuery] Guid? patientId,
        [FromQuery] string? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _patientService.GetAuditLogAsync(patientId, userId, from, to, page, pageSize);
        return Ok(result);
    }

    // ---------- Emergency alert ----------

    [HttpPost("{id:guid}/emergency-alert")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<ActionResult<EmergencyAlertResultDto>> TriggerEmergencyAlert(Guid id)
    {
        var result = await _patientService.TriggerEmergencyAlertAsync(id, CurrentUserId, CurrentRoles);
        return ToActionResult(result, value => Ok(value));
    }

    [HttpGet("emergency-alerts")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<ActionResult<PaginatedResult<EmergencyAlertLogSummaryDto>>> GetEmergencyAlertLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _patientService.GetEmergencyAlertLogsAsync(page, pageSize);
        return Ok(result);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result, Func<T, ActionResult<T>> onSuccess)
    {
        if (result.Succeeded)
        {
            return onSuccess(result.Value!);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Forbidden => Forbid(),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }

    private ActionResult ToPlainActionResult<T>(ServiceResult<T> result, Func<T, ActionResult> onSuccess)
    {
        if (result.Succeeded)
        {
            return onSuccess(result.Value!);
        }

        return result.ErrorType switch
        {
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Forbidden => Forbid(),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            _ => BadRequest(new { message = result.ErrorMessage })
        };
    }
}
