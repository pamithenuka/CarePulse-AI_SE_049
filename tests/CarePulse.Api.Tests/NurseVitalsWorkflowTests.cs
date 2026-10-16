using System.Security.Claims;
using System.Text.Json;
using CarePulse.Api.Controllers;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Dispatch;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CarePulse.Api.Tests;
public class NurseVitalsWorkflowTests
{
    [Fact]
    public async Task SaveKeepsVisitActive_UpdatesOneRecord_CompletionReleasesNurse_AndHistoryPersists()
    {
        using var db = new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, Mock.Of<ICurrentUserService>());
        var patient = new PatientProfile { FullName = "Test patient" };
        var other = new PatientProfile { FullName = "Other patient" };
        var triage = new TriageTicket { PatientProfileId = patient.Id, Status = "DISPATCH_ASSIGNED" };
        var nurse = new NurseProfiles { UserId = "nurse", FullName = "Test nurse", IsAvailable = false };
        var ticket = new DispatchTickets { NurseId = nurse.Id, TriageTicketId = triage.Id, Status = "Assigned" };
        db.AddRange(patient, other, triage, nurse, ticket);
        await db.SaveChangesAsync();
        var controller = new DispatchController(db, null!, null!);
        void SignIn(string id, string role = "Nurse") => controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Role, role) }, "test")) } };
        var values = new CompleteOnsiteDto { HeartRate = 80, BloodPressure = "120/80", BodyTempC = 37, OxygenSaturation = 98, ClinicalNotes = "Test visit" };
        SignIn("other-nurse");
        Assert.Equal(403, (await Assert.ThrowsAsync<ApiProblem>(() => controller.SaveVitals(ticket.Id, values))).StatusCode);
        Assert.Equal(403, (await Assert.ThrowsAsync<ApiProblem>(() => controller.CompleteVisit(ticket.Id))).StatusCode);
        SignIn("nurse");
        Assert.IsType<ConflictObjectResult>(await controller.SaveVitals(ticket.Id, values));
        ticket.Status = "ArrivedOnSite";
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await controller.CompleteVisit(ticket.Id));
        Assert.IsType<OkObjectResult>(await controller.SaveVitals(ticket.Id, values));
        Assert.Equal("ArrivedOnSite", ticket.Status);
        Assert.False(nurse.IsAvailable);
        Assert.Equal("DISPATCH_ASSIGNED", triage.Status);
        values.HeartRate = 82;
        Assert.IsType<OkObjectResult>(await controller.SaveVitals(ticket.Id, values));
        Assert.Equal(82, (await db.OnSiteVitalsRecords.SingleAsync()).HeartRate);
        Assert.IsType<OkObjectResult>(await controller.CompleteVisit(ticket.Id));
        Assert.Equal("Completed", ticket.Status);
        Assert.True(nurse.IsAvailable);
        Assert.Equal("VISIT_COMPLETED", triage.Status);
        Assert.IsType<OkObjectResult>(await controller.CompleteVisit(ticket.Id));
        Assert.IsType<ConflictObjectResult>(await controller.SaveVitals(ticket.Id, values));
        SignIn("doctor", "Doctor");
        var history = JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.PatientVisits(patient.Id)).Value);
        Assert.Equal(1, history.GetArrayLength());
        Assert.Equal(82, history[0].GetProperty("Vitals")[0].GetProperty("HeartRate").GetInt32());
        Assert.Equal(0, JsonSerializer.SerializeToElement(Assert.IsType<OkObjectResult>(await controller.PatientVisits(other.Id)).Value).GetArrayLength());
    }
}
