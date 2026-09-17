using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Patients;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Patients;

public class PatientService : IPatientService
{
    private readonly CarePulseDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;

    private static readonly HashSet<string> ClinicalRoles = new() { "Doctor", "Admin" };

    public PatientService(
        CarePulseDbContext db, IWebHostEnvironment environment, INotificationService notificationService, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _environment = environment;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    // ---------- Profile CRUD ----------

    /// <summary>
    /// Admin-only: registers a brand-new patient (login account + profile) entirely
    /// from the web app, so front-desk staff never need the mobile app to onboard someone.
    /// </summary>
    public async Task<ServiceResult<PatientProfileDetailDto>> RegisterPatientAsync(RegisterPatientDto dto)
    {
        if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.ValidationFailed, "Date of birth cannot be in the future.");
        }

        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Conflict, "An account with this email already exists.");
        }

        if (await _db.PatientProfiles.AnyAsync(p => p.NationalId == dto.NationalId))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Conflict, "A patient profile with this National ID already exists.");
        }

        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, FullName = dto.FullName, EmailConfirmed = true };
        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(
                ServiceErrorType.ValidationFailed, string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }
        await _userManager.AddToRoleAsync(user, "Patient");

        var profile = new PatientProfile
        {
            UserId = user.Id,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            BloodType = dto.BloodType,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            NationalId = dto.NationalId,
            Allergies = dto.Allergies,
            EmergencyContacts = dto.EmergencyContacts.Select(c => new EmergencyContact
            {
                FullName = c.FullName,
                RelationshipToPatient = c.RelationshipToPatient,
                PhoneNumber = c.PhoneNumber,
                IsPrimary = c.IsPrimary
            }).ToList()
        };

        _db.PatientProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return ServiceResult<PatientProfileDetailDto>.Success(MapToDetailDto(profile));
    }

    public async Task<ServiceResult<PatientProfileDetailDto>> CreateProfileAsync(string userId, CreatePatientProfileDto dto)
    {
        if (dto.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.ValidationFailed, "Date of birth cannot be in the future.");
        }

        if (await _db.PatientProfiles.AnyAsync(p => p.UserId == userId))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Conflict, "A patient profile already exists for this account.");
        }

        if (await _db.PatientProfiles.AnyAsync(p => p.NationalId == dto.NationalId))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Conflict, "A patient profile with this National ID already exists.");
        }

        var profile = new PatientProfile
        {
            UserId = userId,
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            BloodType = dto.BloodType,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            NationalId = dto.NationalId,
            Allergies = dto.Allergies,
            EmergencyContacts = dto.EmergencyContacts.Select(c => new EmergencyContact
            {
                FullName = c.FullName,
                RelationshipToPatient = c.RelationshipToPatient,
                PhoneNumber = c.PhoneNumber,
                IsPrimary = c.IsPrimary
            }).ToList()
        };

        _db.PatientProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return ServiceResult<PatientProfileDetailDto>.Success(MapToDetailDto(profile));
    }

    public async Task<PaginatedResult<PatientListItemDto>> GetPatientsAsync(
        string? search, string? bloodGroup, string? condition, int? minAge, int? maxAge,
        string status, string sortBy, bool descending, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = status.ToLower() switch
        {
            "inactive" => _db.PatientProfiles.IgnoreQueryFilters().Where(p => p.IsDeleted).AsNoTracking(),
            "all" => _db.PatientProfiles.IgnoreQueryFilters().AsNoTracking(),
            _ => _db.PatientProfiles.AsNoTracking()
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(term) ||
                p.NationalId.ToLower().Contains(term) ||
                p.PhoneNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(bloodGroup))
        {
            query = query.Where(p => p.BloodType == bloodGroup);
        }

        if (!string.IsNullOrWhiteSpace(condition))
        {
            var term = condition.Trim().ToLower();
            query = query.Where(p => p.MedicalHistories.Any(h => !h.IsDeleted && h.ConditionName.ToLower().Contains(term)));
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (maxAge.HasValue)
        {
            var minDob = today.AddYears(-(maxAge.Value + 1)).AddDays(1);
            query = query.Where(p => p.DateOfBirth >= minDob);
        }
        if (minAge.HasValue)
        {
            var maxDob = today.AddYears(-minAge.Value);
            query = query.Where(p => p.DateOfBirth <= maxDob);
        }

        query = sortBy.ToLower() switch
        {
            "dateofbirth" or "age" => descending ? query.OrderByDescending(p => p.DateOfBirth) : query.OrderBy(p => p.DateOfBirth),
            "createdat" => descending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            _ => descending ? query.OrderByDescending(p => p.FullName) : query.OrderBy(p => p.FullName)
        };

        var totalCount = await query.CountAsync();

        var page_ = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new { p.Id, p.FullName, p.DateOfBirth, p.Gender, p.BloodType, p.PhoneNumber, p.NationalId, p.IsDeleted, p.CreatedAt })
            .ToListAsync();

        var pageIds = page_.Select(p => p.Id).ToList();
        var conditionsByPatient = await _db.MedicalHistories
            .IgnoreQueryFilters()
            .Where(h => !h.IsDeleted && !h.IsResolved && pageIds.Contains(h.PatientProfileId))
            .GroupBy(h => h.PatientProfileId)
            .Select(g => new { PatientProfileId = g.Key, Conditions = g.Select(h => h.ConditionName).ToList() })
            .ToListAsync();
        var conditionsLookup = conditionsByPatient.ToDictionary(c => c.PatientProfileId, c => string.Join(", ", c.Conditions.Take(3)));

        var items = page_.Select(p => new PatientListItemDto
        {
            Id = p.Id,
            FullName = p.FullName,
            DateOfBirth = p.DateOfBirth,
            Age = CalculateAge(p.DateOfBirth),
            Gender = p.Gender,
            BloodType = p.BloodType,
            PhoneNumber = p.PhoneNumber,
            NationalId = p.NationalId,
            MainConditions = conditionsLookup.GetValueOrDefault(p.Id),
            Status = p.IsDeleted ? "Inactive" : "Active",
            CreatedAt = p.CreatedAt
        }).ToList();

        return new PaginatedResult<PatientListItemDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    public async Task<ServiceResult<PatientProfileDetailDto>> GetProfileDetailAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await LoadProfileWithChildrenAsync(patientProfileId);
        if (profile is null)
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to view this patient profile.");
        }

        return ServiceResult<PatientProfileDetailDto>.Success(MapToDetailDto(profile));
    }

    public async Task<ServiceResult<PatientProfileDetailDto>> UpdateProfileAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, UpdatePatientProfileDto dto)
    {
        var profile = await _db.PatientProfiles
            .Include(p => p.EmergencyContacts)
            .Include(p => p.MedicalHistories)
            .Include(p => p.MedicalDocuments)
            .FirstOrDefaultAsync(p => p.Id == patientProfileId);

        if (profile is null)
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to update this patient profile.");
        }

        var isAdmin = requestingRoles.Contains("Admin");

        // A patient may only change their own contact details; an Admin may correct any field.
        if (dto.PhoneNumber is not null) profile.PhoneNumber = dto.PhoneNumber;
        if (dto.Address is not null) profile.Address = dto.Address;

        if (isAdmin)
        {
            if (dto.DateOfBirth is { } dob)
            {
                if (dob > DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.ValidationFailed, "Date of birth cannot be in the future.");
                }
                profile.DateOfBirth = dob;
            }
            if (dto.FullName is not null) profile.FullName = dto.FullName;
            if (dto.Gender is not null) profile.Gender = dto.Gender;
            if (dto.BloodType is not null) profile.BloodType = dto.BloodType;
            if (dto.Allergies is not null) profile.Allergies = dto.Allergies;
            if (dto.NationalId is not null && dto.NationalId != profile.NationalId)
            {
                if (await _db.PatientProfiles.AnyAsync(p => p.NationalId == dto.NationalId && p.Id != patientProfileId))
                {
                    return ServiceResult<PatientProfileDetailDto>.Fail(ServiceErrorType.Conflict, "Another patient already uses this National ID.");
                }
                profile.NationalId = dto.NationalId;
            }
        }

        await _db.SaveChangesAsync();

        return ServiceResult<PatientProfileDetailDto>.Success(MapToDetailDto(profile));
    }

    public async Task<ServiceResult<bool>> DeactivateProfileAsync(Guid patientProfileId, string requestingUserId)
    {
        var profile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        profile.IsDeleted = true;
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> ReactivateProfileAsync(Guid patientProfileId)
    {
        var profile = await _db.PatientProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        profile.IsDeleted = false;
        profile.DeletedAt = null;
        profile.DeletedBy = null;
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    // ---------- Medical history CRUD ----------

    public async Task<ServiceResult<List<MedicalHistoryDto>>> GetMedicalHistoryAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.MedicalHistories).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<List<MedicalHistoryDto>>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<List<MedicalHistoryDto>>.Fail(ServiceErrorType.Forbidden, "You are not allowed to view this patient's history.");
        }

        var history = profile.MedicalHistories.OrderByDescending(h => h.DiagnosedOn).Select(MapToHistoryDto).ToList();
        return ServiceResult<List<MedicalHistoryDto>>.Success(history);
    }

    public async Task<ServiceResult<List<MedicalHistoryDto>>> AddMedicalHistoryAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, AddMedicalHistoryDto dto)
    {
        var profile = await _db.PatientProfiles
            .Include(p => p.MedicalHistories)
            .FirstOrDefaultAsync(p => p.Id == patientProfileId);

        if (profile is null)
        {
            return ServiceResult<List<MedicalHistoryDto>>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<List<MedicalHistoryDto>>.Fail(ServiceErrorType.Forbidden, "You are not allowed to update this patient's history.");
        }

        var newEntry = new MedicalHistory
        {
            PatientProfileId = profile.Id,
            ConditionName = dto.ConditionName,
            Notes = dto.Notes,
            DiagnosedOn = dto.DiagnosedOn,
            IsChronic = dto.IsChronic,
            CurrentMedications = dto.CurrentMedications,
            RecordedByUserId = requestingUserId
        };

        // Added explicitly via the DbSet rather than profile.MedicalHistories.Add(...):
        // EF's change detector treats a client-generated (non-default) Guid key discovered
        // through a tracked parent's collection as Modified/Unchanged, not Added.
        // Relationship fixup attaches it to profile.MedicalHistories automatically.
        _db.MedicalHistories.Add(newEntry);

        await _db.SaveChangesAsync();

        var history = profile.MedicalHistories
            .OrderByDescending(h => h.DiagnosedOn)
            .Select(MapToHistoryDto)
            .ToList();

        return ServiceResult<List<MedicalHistoryDto>>.Success(history);
    }

    public async Task<ServiceResult<MedicalHistoryDto>> UpdateMedicalHistoryAsync(
        Guid patientProfileId, Guid historyId, string requestingUserId, IList<string> requestingRoles, UpdateMedicalHistoryDto dto)
    {
        if (!requestingRoles.Any(ClinicalRoles.Contains))
        {
            return ServiceResult<MedicalHistoryDto>.Fail(ServiceErrorType.Forbidden, "Only a doctor or admin may update a medical history entry.");
        }

        var history = await _db.MedicalHistories.FirstOrDefaultAsync(h => h.Id == historyId && h.PatientProfileId == patientProfileId);
        if (history is null)
        {
            return ServiceResult<MedicalHistoryDto>.Fail(ServiceErrorType.NotFound, "Medical history entry not found.");
        }

        history.ConditionName = dto.ConditionName;
        history.Notes = dto.Notes;
        history.DiagnosedOn = dto.DiagnosedOn;
        history.IsChronic = dto.IsChronic;
        history.CurrentMedications = dto.CurrentMedications;
        history.IsResolved = dto.IsResolved;
        history.ResolvedOn = dto.IsResolved ? dto.ResolvedOn ?? DateOnly.FromDateTime(DateTime.UtcNow) : null;

        await _db.SaveChangesAsync();

        return ServiceResult<MedicalHistoryDto>.Success(MapToHistoryDto(history));
    }

    public async Task<ServiceResult<bool>> DeleteMedicalHistoryAsync(
        Guid patientProfileId, Guid historyId, string requestingUserId, IList<string> requestingRoles)
    {
        if (!requestingRoles.Any(ClinicalRoles.Contains))
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.Forbidden, "Only a doctor or admin may remove a medical history entry.");
        }

        var history = await _db.MedicalHistories.FirstOrDefaultAsync(h => h.Id == historyId && h.PatientProfileId == patientProfileId);
        if (history is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Medical history entry not found.");
        }

        // Soft delete: marks the entry "entered in error" instead of erasing the legal record.
        history.IsDeleted = true;
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    // ---------- Emergency contact CRUD ----------

    public async Task<ServiceResult<List<EmergencyContactDto>>> GetEmergencyContactsAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.EmergencyContacts).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<List<EmergencyContactDto>>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<List<EmergencyContactDto>>.Fail(ServiceErrorType.Forbidden, "You are not allowed to view this patient's contacts.");
        }

        return ServiceResult<List<EmergencyContactDto>>.Success(profile.EmergencyContacts.Select(MapToContactDto).ToList());
    }

    public async Task<ServiceResult<List<EmergencyContactDto>>> AddEmergencyContactAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, CreateEmergencyContactDto dto)
    {
        var profile = await _db.PatientProfiles.Include(p => p.EmergencyContacts).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<List<EmergencyContactDto>>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<List<EmergencyContactDto>>.Fail(ServiceErrorType.Forbidden, "You are not allowed to modify this patient's contacts.");
        }

        var contact = new EmergencyContact
        {
            PatientProfileId = profile.Id,
            FullName = dto.FullName,
            RelationshipToPatient = dto.RelationshipToPatient,
            PhoneNumber = dto.PhoneNumber,
            IsPrimary = dto.IsPrimary
        };
        _db.EmergencyContacts.Add(contact);
        await _db.SaveChangesAsync();

        return ServiceResult<List<EmergencyContactDto>>.Success(profile.EmergencyContacts.Select(MapToContactDto).ToList());
    }

    public async Task<ServiceResult<EmergencyContactDto>> UpdateEmergencyContactAsync(
        Guid patientProfileId, Guid contactId, string requestingUserId, IList<string> requestingRoles, UpdateEmergencyContactDto dto)
    {
        var profile = await _db.PatientProfiles.Include(p => p.EmergencyContacts).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<EmergencyContactDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<EmergencyContactDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to modify this patient's contacts.");
        }

        var contact = profile.EmergencyContacts.FirstOrDefault(c => c.Id == contactId);
        if (contact is null)
        {
            return ServiceResult<EmergencyContactDto>.Fail(ServiceErrorType.NotFound, "Emergency contact not found.");
        }

        if (dto.IsPrimary)
        {
            foreach (var other in profile.EmergencyContacts.Where(c => c.Id != contactId))
            {
                other.IsPrimary = false;
            }
        }

        contact.FullName = dto.FullName;
        contact.RelationshipToPatient = dto.RelationshipToPatient;
        contact.PhoneNumber = dto.PhoneNumber;
        contact.IsPrimary = dto.IsPrimary;

        await _db.SaveChangesAsync();

        return ServiceResult<EmergencyContactDto>.Success(MapToContactDto(contact));
    }

    public async Task<ServiceResult<bool>> DeleteEmergencyContactAsync(
        Guid patientProfileId, Guid contactId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.EmergencyContacts).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.Forbidden, "You are not allowed to modify this patient's contacts.");
        }

        var contact = profile.EmergencyContacts.FirstOrDefault(c => c.Id == contactId);
        if (contact is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Emergency contact not found.");
        }

        if (profile.EmergencyContacts.Count <= 1)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.ValidationFailed, "A patient must keep at least one emergency contact.");
        }

        _db.EmergencyContacts.Remove(contact);
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    // ---------- Document CRUD ----------

    public async Task<ServiceResult<List<MedicalDocumentDto>>> GetDocumentsAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.MedicalDocuments).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<List<MedicalDocumentDto>>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<List<MedicalDocumentDto>>.Fail(ServiceErrorType.Forbidden, "You are not allowed to view this patient's documents.");
        }

        return ServiceResult<List<MedicalDocumentDto>>.Success(
            profile.MedicalDocuments.OrderByDescending(d => d.CreatedAt).Select(MapToDocumentDto).ToList());
    }

    public async Task<ServiceResult<MedicalDocumentDto>> UploadDocumentAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles, IFormFile file, MedicalDocumentType documentType)
    {
        var profile = await _db.PatientProfiles.FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to upload documents for this patient.");
        }

        if (file.Length == 0)
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.ValidationFailed, "The uploaded file is empty.");
        }

        var webRoot = string.IsNullOrEmpty(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;

        var relativeDir = Path.Combine("uploads", "patients", patientProfileId.ToString());
        var absoluteDir = Path.Combine(webRoot, relativeDir);
        Directory.CreateDirectory(absoluteDir);

        var storedFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var absolutePath = Path.Combine(absoluteDir, storedFileName);

        await using (var stream = new FileStream(absolutePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var document = new MedicalDocument
        {
            PatientProfileId = patientProfileId,
            FileName = file.FileName,
            FilePath = Path.Combine(relativeDir, storedFileName).Replace("\\", "/"),
            ContentType = file.ContentType,
            DocumentType = documentType
        };

        _db.MedicalDocuments.Add(document);
        await _db.SaveChangesAsync();

        return ServiceResult<MedicalDocumentDto>.Success(MapToDocumentDto(document));
    }

    public async Task<ServiceResult<MedicalDocumentDto>> UpdateDocumentAsync(
        Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles, UpdateMedicalDocumentDto dto)
    {
        var profile = await _db.PatientProfiles.Include(p => p.MedicalDocuments).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to modify this patient's documents.");
        }

        var document = profile.MedicalDocuments.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return ServiceResult<MedicalDocumentDto>.Fail(ServiceErrorType.NotFound, "Document not found.");
        }

        document.FileName = dto.FileName;
        document.DocumentType = dto.DocumentType;
        await _db.SaveChangesAsync();

        return ServiceResult<MedicalDocumentDto>.Success(MapToDocumentDto(document));
    }

    public async Task<ServiceResult<bool>> DeleteDocumentAsync(
        Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.MedicalDocuments).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanModifyContacts(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.Forbidden, "You are not allowed to modify this patient's documents.");
        }

        var document = profile.MedicalDocuments.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Document not found.");
        }

        document.IsDeleted = true;
        await _db.SaveChangesAsync();

        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<(string AbsolutePath, string FileName, string ContentType)>> GetDocumentFileAsync(
        Guid patientProfileId, Guid documentId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles.Include(p => p.MedicalDocuments).FirstOrDefaultAsync(p => p.Id == patientProfileId);
        if (profile is null)
        {
            return ServiceResult<(string, string, string)>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }
        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<(string, string, string)>.Fail(ServiceErrorType.Forbidden, "You are not allowed to access this patient's documents.");
        }

        var document = profile.MedicalDocuments.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
        {
            return ServiceResult<(string, string, string)>.Fail(ServiceErrorType.NotFound, "Document not found.");
        }

        var webRoot = string.IsNullOrEmpty(_environment.WebRootPath)
            ? Path.Combine(_environment.ContentRootPath, "wwwroot")
            : _environment.WebRootPath;
        var absolutePath = Path.Combine(webRoot, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

        if (!File.Exists(absolutePath))
        {
            return ServiceResult<(string, string, string)>.Fail(ServiceErrorType.NotFound, "The stored file could not be found.");
        }

        return ServiceResult<(string, string, string)>.Success((absolutePath, document.FileName, document.ContentType));
    }

    // ---------- Audit ----------

    public async Task<PaginatedResult<PatientAuditLogDto>> GetAuditLogAsync(
        Guid? patientProfileId, string? changedByUserId, DateTime? from, DateTime? to, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.PatientAuditLogs.AsQueryable();
        if (patientProfileId.HasValue) query = query.Where(a => a.PatientProfileId == patientProfileId.Value);
        if (!string.IsNullOrWhiteSpace(changedByUserId)) query = query.Where(a => a.ChangedByUserId == changedByUserId);
        if (from.HasValue) query = query.Where(a => a.ChangedAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.ChangedAt <= to.Value);

        query = query.OrderByDescending(a => a.ChangedAt);

        var totalCount = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = logs.Select(l => l.ChangedByUserId).Distinct().ToList();
        var userNames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        var patientIds = logs.Select(l => l.PatientProfileId).Distinct().ToList();
        var patientNames = await _db.PatientProfiles.IgnoreQueryFilters()
            .Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.FullName);

        var items = logs.Select(l => new PatientAuditLogDto
        {
            Id = l.Id,
            PatientProfileId = l.PatientProfileId,
            PatientFullName = patientNames.GetValueOrDefault(l.PatientProfileId, "Unknown"),
            EntityName = l.EntityName,
            EntityId = l.EntityId,
            Action = l.Action.ToString(),
            ChangesJson = l.ChangesJson,
            ChangedByUserId = l.ChangedByUserId,
            ChangedByName = userNames.GetValueOrDefault(l.ChangedByUserId, l.ChangedByUserId == "system" ? "System" : "Unknown"),
            ChangedAt = l.ChangedAt
        }).ToList();

        return new PaginatedResult<PatientAuditLogDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    // ---------- Emergency alert ----------

    public async Task<ServiceResult<EmergencyAlertResultDto>> TriggerEmergencyAlertAsync(
        Guid patientProfileId, string requestingUserId, IList<string> requestingRoles)
    {
        var profile = await _db.PatientProfiles
            .Include(p => p.EmergencyContacts)
            .Include(p => p.MedicalHistories)
            .FirstOrDefaultAsync(p => p.Id == patientProfileId);

        if (profile is null)
        {
            return ServiceResult<EmergencyAlertResultDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        if (!CanAccessProfile(profile, requestingUserId, requestingRoles))
        {
            return ServiceResult<EmergencyAlertResultDto>.Fail(ServiceErrorType.Forbidden, "You are not allowed to trigger an alert for this patient.");
        }

        if (profile.EmergencyContacts.Count == 0)
        {
            return ServiceResult<EmergencyAlertResultDto>.Fail(ServiceErrorType.ValidationFailed, "Add at least one emergency contact before triggering an alert.");
        }

        var chronicConditions = profile.MedicalHistories.Where(h => !h.IsResolved).Select(h => h.ConditionName).Distinct().ToList();
        var activeMedications = profile.MedicalHistories
            .Where(h => !h.IsResolved && !string.IsNullOrWhiteSpace(h.CurrentMedications))
            .Select(h => h.CurrentMedications!)
            .Distinct()
            .ToList();
        var allergies = SplitCsv(profile.Allergies);

        var alertLog = new EmergencyAlertLog
        {
            PatientProfileId = profile.Id,
            TriggeredByUserId = requestingUserId,
            TriggeredAt = DateTime.UtcNow,
            BloodGroupSnapshot = profile.BloodType,
            AllergiesSnapshot = profile.Allergies,
            ActiveMedicationsSnapshot = string.Join(", ", activeMedications),
            ChronicConditionsSnapshot = string.Join(", ", chronicConditions)
        };

        var summary = $"CarePulse EMERGENCY ALERT: {profile.FullName} ({CalculateAge(profile.DateOfBirth)}y, {profile.Gender}) needs help. " +
                      $"Blood: {profile.BloodType ?? "unknown"}. Allergies: {(allergies.Count > 0 ? string.Join(", ", allergies) : "none recorded")}. " +
                      $"Chronic conditions: {(chronicConditions.Count > 0 ? string.Join(", ", chronicConditions) : "none recorded")}.";

        var notificationDtos = new List<EmergencyAlertNotificationDto>();
        foreach (var contact in profile.EmergencyContacts)
        {
            var smsResult = await _notificationService.SendSmsAsync(contact.PhoneNumber, summary);
            var status = smsResult.Succeeded ? NotificationDeliveryStatus.Sent : NotificationDeliveryStatus.Failed;

            alertLog.Notifications.Add(new EmergencyAlertNotification
            {
                ContactName = contact.FullName,
                PhoneNumber = contact.PhoneNumber,
                DeliveryStatus = status,
                DeliveredAt = smsResult.Succeeded ? DateTime.UtcNow : null
            });

            notificationDtos.Add(new EmergencyAlertNotificationDto
            {
                ContactName = contact.FullName,
                PhoneNumber = contact.PhoneNumber,
                DeliveryStatus = status
            });
        }

        _db.EmergencyAlertLogs.Add(alertLog);
        profile.LastEmergencyBroadcastAt = alertLog.TriggeredAt;
        await _db.SaveChangesAsync();

        return ServiceResult<EmergencyAlertResultDto>.Success(new EmergencyAlertResultDto
        {
            AlertLogId = alertLog.Id,
            PatientProfileId = profile.Id,
            TriggeredAt = alertLog.TriggeredAt,
            BloodGroup = profile.BloodType,
            Allergies = allergies,
            ChronicConditions = chronicConditions,
            ActiveMedications = activeMedications,
            NotifiedContacts = notificationDtos
        });
    }

    public async Task<PaginatedResult<EmergencyAlertLogSummaryDto>> GetEmergencyAlertLogsAsync(int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.EmergencyAlertLogs
            .Include(a => a.Notifications)
            .Include(a => a.PatientProfile)
            .OrderByDescending(a => a.TriggeredAt);

        var totalCount = await query.CountAsync();
        var logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var userIds = logs.Select(l => l.TriggeredByUserId).Distinct().ToList();
        var userNames = await _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        var items = logs.Select(a => new EmergencyAlertLogSummaryDto
        {
            Id = a.Id,
            PatientProfileId = a.PatientProfileId,
            PatientFullName = a.PatientProfile?.FullName ?? "Unknown",
            TriggeredByUserId = a.TriggeredByUserId,
            TriggeredByName = userNames.GetValueOrDefault(a.TriggeredByUserId, "Unknown"),
            TriggeredAt = a.TriggeredAt,
            ContactsNotified = a.Notifications.Count(n => n.DeliveryStatus == NotificationDeliveryStatus.Sent),
            ContactsFailed = a.Notifications.Count(n => n.DeliveryStatus == NotificationDeliveryStatus.Failed)
        }).ToList();

        return new PaginatedResult<EmergencyAlertLogSummaryDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = totalCount };
    }

    // ---------- Agent 1 context ----------

    public async Task<ServiceResult<PatientContextDto>> GetPatientContextAsync(Guid patientProfileId)
    {
        var profile = await _db.PatientProfiles
            .Include(p => p.EmergencyContacts)
            .Include(p => p.MedicalHistories)
            .FirstOrDefaultAsync(p => p.Id == patientProfileId);

        if (profile is null)
        {
            return ServiceResult<PatientContextDto>.Fail(ServiceErrorType.NotFound, "Patient profile not found.");
        }

        var context = new PatientContextDto
        {
            PatientProfileId = profile.Id,
            FullName = profile.FullName,
            Age = CalculateAge(profile.DateOfBirth),
            Gender = profile.Gender,
            BloodType = profile.BloodType,
            Allergies = SplitCsv(profile.Allergies),
            ChronicConditions = profile.MedicalHistories.Where(h => !h.IsResolved).Select(h => h.ConditionName).Distinct().ToList(),
            ActiveMedications = profile.MedicalHistories
                .Where(h => !h.IsResolved && !string.IsNullOrWhiteSpace(h.CurrentMedications))
                .Select(h => h.CurrentMedications!)
                .Distinct()
                .ToList(),
            EmergencyContacts = profile.EmergencyContacts.Select(MapToContactDto).ToList()
        };

        return ServiceResult<PatientContextDto>.Success(context);
    }

    // ---------- Helpers ----------

    private async Task<PatientProfile?> LoadProfileWithChildrenAsync(Guid patientProfileId) =>
        await _db.PatientProfiles
            .Include(p => p.EmergencyContacts)
            .Include(p => p.MedicalHistories)
            .Include(p => p.MedicalDocuments)
            .FirstOrDefaultAsync(p => p.Id == patientProfileId);

    private static bool CanAccessProfile(PatientProfile profile, string requestingUserId, IList<string> requestingRoles) =>
        profile.UserId == requestingUserId || requestingRoles.Any(ClinicalRoles.Contains);

    /// <summary>Contacts/documents may be changed by the owning patient or an Admin correcting a mistake.</summary>
    private static bool CanModifyContacts(PatientProfile profile, string requestingUserId, IList<string> requestingRoles) =>
        profile.UserId == requestingUserId || requestingRoles.Contains("Admin");

    private static int CalculateAge(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age;
    }

    private static List<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static PatientProfileDetailDto MapToDetailDto(PatientProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FullName = profile.FullName,
        DateOfBirth = profile.DateOfBirth,
        Age = CalculateAge(profile.DateOfBirth),
        Gender = profile.Gender,
        BloodType = profile.BloodType,
        PhoneNumber = profile.PhoneNumber,
        Address = profile.Address,
        NationalId = profile.NationalId,
        ProfilePhotoUrl = profile.ProfilePhotoUrl,
        Allergies = profile.Allergies,
        Status = profile.IsDeleted ? "Inactive" : "Active",
        LastEmergencyBroadcastAt = profile.LastEmergencyBroadcastAt,
        CreatedAt = profile.CreatedAt,
        EmergencyContacts = profile.EmergencyContacts.Select(MapToContactDto).ToList(),
        MedicalHistories = profile.MedicalHistories.OrderByDescending(h => h.DiagnosedOn).Select(MapToHistoryDto).ToList(),
        MedicalDocuments = profile.MedicalDocuments.OrderByDescending(d => d.CreatedAt).Select(MapToDocumentDto).ToList()
    };

    private static EmergencyContactDto MapToContactDto(EmergencyContact contact) => new()
    {
        Id = contact.Id,
        FullName = contact.FullName,
        RelationshipToPatient = contact.RelationshipToPatient,
        PhoneNumber = contact.PhoneNumber,
        IsPrimary = contact.IsPrimary
    };

    private static MedicalHistoryDto MapToHistoryDto(MedicalHistory history) => new()
    {
        Id = history.Id,
        ConditionName = history.ConditionName,
        Notes = history.Notes,
        DiagnosedOn = history.DiagnosedOn,
        IsChronic = history.IsChronic,
        CurrentMedications = history.CurrentMedications,
        IsResolved = history.IsResolved,
        ResolvedOn = history.ResolvedOn,
        CreatedAt = history.CreatedAt
    };

    private static MedicalDocumentDto MapToDocumentDto(MedicalDocument document) => new()
    {
        Id = document.Id,
        FileName = document.FileName,
        FileUrl = $"/{document.FilePath}",
        DocumentType = document.DocumentType,
        CreatedAt = document.CreatedAt
    };
}
