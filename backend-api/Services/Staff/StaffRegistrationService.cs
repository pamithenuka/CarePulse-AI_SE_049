using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Services.Patients;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Staff;

public class StaffRegistrationService : IStaffRegistrationService
{
    private readonly CarePulseDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StaffRegistrationService(CarePulseDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<ServiceResult<DoctorProfileDto>> RegisterDoctorAsync(RegisterDoctorDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
        {
            return ServiceResult<DoctorProfileDto>.Fail(ServiceErrorType.Conflict, "An account with this email already exists.");
        }

        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, FullName = dto.FullName, EmailConfirmed = true };
        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<DoctorProfileDto>.Fail(
                ServiceErrorType.ValidationFailed, string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }
        await _userManager.AddToRoleAsync(user, "Doctor");

        var profile = new DoctorProfile
        {
            UserId = user.Id,
            FullName = dto.FullName,
            Specialty = dto.Specialty,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email
        };

        _db.DoctorProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return ServiceResult<DoctorProfileDto>.Success(MapDoctor(profile));
    }

    public async Task<ServiceResult<NurseProfileDto>> RegisterNurseAsync(RegisterNurseDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
        {
            return ServiceResult<NurseProfileDto>.Fail(ServiceErrorType.Conflict, "An account with this email already exists.");
        }

        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, FullName = dto.FullName, EmailConfirmed = true };
        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<NurseProfileDto>.Fail(
                ServiceErrorType.ValidationFailed, string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }
        await _userManager.AddToRoleAsync(user, "Nurse");

        var profile = new NurseProfiles
        {
            UserId = user.Id,
            FullName = dto.FullName,
            LicenseNumber = dto.LicenseNumber,
            Specialization = dto.Specialization,
            IsAvailable = false
        };

        _db.NurseProfiles.Add(profile);
        await _db.SaveChangesAsync();

        return ServiceResult<NurseProfileDto>.Success(MapNurse(profile));
    }

    public async Task<List<DoctorProfileDto>> GetDoctorsAsync(string status = "active")
    {
        var query = status.ToLower() switch
        {
            "inactive" => _db.DoctorProfiles.IgnoreQueryFilters().Where(d => d.IsDeleted),
            "all" => _db.DoctorProfiles.IgnoreQueryFilters(),
            _ => _db.DoctorProfiles.AsQueryable()
        };
        var doctors = await query.OrderBy(d => d.FullName).ToListAsync();
        return doctors.Select(MapDoctor).ToList();
    }

    public async Task<ServiceResult<DoctorProfileDto>> GetDoctorByIdAsync(Guid id)
    {
        // IgnoreQueryFilters: an Admin viewing a deleted doctor's detail page (to restore
        // them) still needs to be able to load the record past the soft-delete filter.
        var doctor = await _db.DoctorProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return ServiceResult<DoctorProfileDto>.Fail(ServiceErrorType.NotFound, "Doctor not found.");
        }
        return ServiceResult<DoctorProfileDto>.Success(MapDoctor(doctor));
    }

    public async Task<List<NurseProfileDto>> GetNursesAsync(string status = "active")
    {
        var query = status.ToLower() switch
        {
            "inactive" => _db.NurseProfiles.IgnoreQueryFilters().Where(n => n.IsDeleted),
            "all" => _db.NurseProfiles.IgnoreQueryFilters(),
            _ => _db.NurseProfiles.AsQueryable()
        };
        var nurses = await query.OrderBy(n => n.FullName).ToListAsync();
        return nurses.Select(MapNurse).ToList();
    }

    public async Task<ServiceResult<NurseProfileDto>> GetNurseByIdAsync(Guid id)
    {
        var nurse = await _db.NurseProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == id);
        if (nurse is null)
        {
            return ServiceResult<NurseProfileDto>.Fail(ServiceErrorType.NotFound, "Nurse not found.");
        }
        return ServiceResult<NurseProfileDto>.Success(MapNurse(nurse));
    }

    public async Task<ServiceResult<bool>> DeleteDoctorAsync(Guid id)
    {
        var doctor = await _db.DoctorProfiles.FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Doctor not found.");
        }
        doctor.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> RestoreDoctorAsync(Guid id)
    {
        var doctor = await _db.DoctorProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == id);
        if (doctor is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Doctor not found.");
        }
        doctor.IsDeleted = false;
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> DeleteNurseAsync(Guid id)
    {
        var nurse = await _db.NurseProfiles.FirstOrDefaultAsync(n => n.Id == id);
        if (nurse is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Nurse not found.");
        }
        nurse.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    public async Task<ServiceResult<bool>> RestoreNurseAsync(Guid id)
    {
        var nurse = await _db.NurseProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.Id == id);
        if (nurse is null)
        {
            return ServiceResult<bool>.Fail(ServiceErrorType.NotFound, "Nurse not found.");
        }
        nurse.IsDeleted = false;
        await _db.SaveChangesAsync();
        return ServiceResult<bool>.Success(true);
    }

    private static DoctorProfileDto MapDoctor(DoctorProfile p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        FullName = p.FullName,
        Specialty = p.Specialty,
        PhoneNumber = p.PhoneNumber,
        Email = p.Email,
        Status = p.IsDeleted ? "Inactive" : "Active"
    };

    private static NurseProfileDto MapNurse(NurseProfiles p) => new()
    {
        Id = p.Id,
        UserId = p.UserId,
        FullName = p.FullName,
        LicenseNumber = p.LicenseNumber,
        Specialization = p.Specialization,
        IsAvailable = p.IsAvailable,
        Status = p.IsDeleted ? "Inactive" : "Active"
    };
}
