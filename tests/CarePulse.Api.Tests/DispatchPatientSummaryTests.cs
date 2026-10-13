using System.Security.Claims;
using System.Text.Json;
using CarePulse.Api.Controllers;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CarePulse.Api.Tests;

public class DispatchPatientSummaryTests
{
    [Fact]
    public async Task PatientDetails_AreScopedToAssignedNurse_AndClosedVisitsAreDenied()
    {
        using var db = new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, Mock.Of<ICurrentUserService>());
        var patient = new PatientProfile { FullName = "Synthetic Patient", PhoneNumber = "Test contact", Allergies = null };
        var triage = new TriageTicket { PatientProfileId = patient.Id, Symptoms = "Synthetic symptoms" };
        var nurse = new NurseProfiles { UserId = "assigned-nurse", FullName = "Synthetic Nurse" };
        var ticket = new DispatchTickets { NurseId = nurse.Id, TriageTicketId = triage.Id, Status = "Assigned" };
        db.AddRange(patient, triage, nurse, ticket);
        await db.SaveChangesAsync();
        var controller = new DispatchController(db, null!, null!);
        void SignIn(string id) => controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, "Nurse") }, "test")) }
        };
        SignIn("other-nurse");
        var denied = await Assert.ThrowsAsync<ApiProblem>(() => controller.GetPatientSummary(ticket.Id));
        Assert.Equal(403, denied.StatusCode);
        var otherList = Assert.IsType<OkObjectResult>(await controller.GetActiveDispatches());
        Assert.Equal(0, JsonSerializer.SerializeToElement(otherList.Value).GetProperty("totalItems").GetInt32());
        SignIn("assigned-nurse");
        var result = Assert.IsType<OkObjectResult>(await controller.GetPatientSummary(ticket.Id));
        var json = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal(patient.FullName, json.GetProperty("PatientName").GetString());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("Allergies").ValueKind);
        Assert.False(json.TryGetProperty("NationalId", out _));
        var list = Assert.IsType<OkObjectResult>(await controller.GetActiveDispatches());
        Assert.Equal(patient.FullName, JsonSerializer.SerializeToElement(list.Value).GetProperty("tickets")[0].GetProperty("PatientName").GetString());
        ticket.Status = "Completed";
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await controller.GetPatientSummary(ticket.Id));
    }
}
