using System.Security.Claims;
using CarePulse.Api.Controllers;
using CarePulse.Api.Data;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CarePulse.Api.Tests;

public class NurseAvailabilityTests
{
    [Theory]
    [InlineData(-1, 13)]
    [InlineData(-6, null)]
    [InlineData(1, null)]
    public async Task Assignment_OnlyUsesRecentNurseLocation(int minutesOffset, int? expectedEta)
    {
        using var db = new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, Mock.Of<ICurrentUserService>());
        var doctor = new CarePulse.Api.Entities.DoctorProfile { UserId = "doctor" };
        var nurse = new NurseProfiles { UserId = "nurse", IsAvailable = true, CurrentLat = 6.92, CurrentLng = 79.86,
            LocationRecordedAt = DateTime.UtcNow.AddMinutes(minutesOffset) };
        var triage = new CarePulse.Api.Entities.TriageTicket { Status = CarePulse.Api.DTOs.TriageConstants.StatusApproved, Latitude = 6.93, Longitude = 79.87 };
        triage.ApprovalQueue = new CarePulse.Api.Entities.ApprovalQueue { TriageTicketId = triage.Id,
            ReviewStatus = "APPROVED", ReviewedByDoctorId = "doctor" };
        db.AddRange(doctor, nurse, triage);
        await db.SaveChangesAsync();
        var routes = new Mock<CarePulse.Api.Services.Dispatch.IGoogleMapsService>();
        routes.Setup(r => r.GetRouteMetricsAsync(6.92, 79.86, 6.93, 79.87)).ReturnsAsync((5d, 12.2d));
        var safety = new Mock<CarePulse.Api.Services.Agents.IValidationAgent>();
        int? actualEta = -1;
        safety.Setup(a => a.EvaluateDispatchSafetyAsync(It.IsAny<CarePulse.Api.Services.Agents.ValidationAgentRequest>()))
            .Callback<CarePulse.Api.Services.Agents.ValidationAgentRequest>(r => actualEta = r.EtaMinutes)
            .ReturnsAsync(new CarePulse.Api.Services.Agents.ValidationAgentResponse { FlaggedRules = ["Review required"] });
        var controller = new DispatchController(db, routes.Object, safety.Object) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "doctor"),
                    new Claim(ClaimTypes.Role, "Doctor") }, "test")) } }
        };
        Assert.IsType<ConflictObjectResult>(await controller.AssignDispatch(new() { TriageTicketId = triage.Id, NurseId = nurse.Id }));
        Assert.Equal(expectedEta, actualEta);
        routes.Verify(r => r.GetRouteMetricsAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()),
            expectedEta.HasValue ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task SelfServiceLocation_IsOwnedAndRejectsUnavailableOrBusyNurses()
    {
        using var db = new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, Mock.Of<ICurrentUserService>());
        var own = new NurseProfiles { UserId = "nurse-a", IsAvailable = false };
        var other = new NurseProfiles { UserId = "nurse-b", CurrentLat = 10, IsAvailable = true };
        db.AddRange(own, other);
        await db.SaveChangesAsync();
        var controller = new DispatchController(db, null!, null!) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, "nurse-a"), new Claim(ClaimTypes.Role, "Nurse") }, "test")) } }
        };
        var location = new CarePulse.Api.DTOs.Dispatch.NurseLocationUpdateDto { Latitude = 6.92, Longitude = 79.86 };
        Assert.IsType<ConflictObjectResult>(await controller.UpdateAvailableLocation(location));
        Assert.IsType<OkObjectResult>(await controller.SetMyAvailability(new() { IsAvailable = true }));
        Assert.Null(own.LocationRecordedAt);
        Assert.IsType<OkObjectResult>(await controller.UpdateAvailableLocation(location));
        Assert.Equal(6.92, own.CurrentLat);
        Assert.NotNull(own.LocationRecordedAt);
        Assert.Equal(10, other.CurrentLat);
        Assert.Null(other.LocationRecordedAt);
        Assert.IsType<OkObjectResult>(await controller.SetMyAvailability(new() { IsAvailable = false }));
        Assert.Null(own.LocationRecordedAt);
        Assert.IsType<ConflictObjectResult>(await controller.UpdateAvailableLocation(location));
        db.Add(new DispatchTickets { NurseId = own.Id, Status = "Assigned" });
        await db.SaveChangesAsync();
        Assert.IsType<ConflictObjectResult>(await controller.SetMyAvailability(new() { IsAvailable = true }));
        Assert.IsType<ConflictObjectResult>(await controller.UpdateAvailableLocation(location));
        Assert.False(own.IsAvailable);
    }
}
