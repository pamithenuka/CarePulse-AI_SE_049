using System.Security.Claims;
using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Services.Common;

public static class ResourceAccess
{
    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new ApiProblem(401, "Please sign in.");

    public static async Task RequirePatientAsync(this CarePulseDbContext db, ClaimsPrincipal user, Guid patientId, bool staffAllowed = true)
    {
        var patient = await db.PatientProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId);
        if (patient == null) throw new ApiProblem(404, "Patient not found.");
        if (patient.UserId != user.UserId() && !(staffAllowed && (user.IsInRole("Doctor") || user.IsInRole("Admin"))))
            throw new ApiProblem(403, "You cannot access this patient.");
    }

    public static async Task RequireDoctorAsync(this CarePulseDbContext db, ClaimsPrincipal user, Guid doctorId)
    {
        var doctor = await db.DoctorProfiles.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);
        if (doctor == null) throw new ApiProblem(404, "Doctor not found.");
        if (!user.IsInRole("Admin") && (!user.IsInRole("Doctor") || doctor.UserId != user.UserId()))
            throw new ApiProblem(403, "You cannot manage this doctor's records.");
    }
}
