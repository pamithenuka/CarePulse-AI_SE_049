using System.Security.Claims;
using CarePulse.Api.Data;
using CarePulse.Api.Entities.Patients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Tests.Fakes;

public static class TestActors
{
    public static T As<T>(this T controller, string role = "Admin", string id = "test-actor") where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role) }, "Test")) } };
        return controller;
    }
    public static async Task<Guid> Patient(CarePulseDbContext db)
    {
        var profile = new PatientProfile { UserId = Guid.NewGuid().ToString(), NationalId = Guid.NewGuid().ToString(), FullName = "Synthetic Patient" };
        db.PatientProfiles.Add(profile); await db.SaveChangesAsync(); return profile.Id;
    }
}
