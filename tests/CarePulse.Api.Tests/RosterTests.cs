using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class RosterTests
{
    private readonly TestDatabaseFixture _fixture;

    public RosterTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedDoctor()
    {
        using var context = _fixture.CreateContext();
        var doctor = new DoctorProfile
        {
            FullName = "Dr. Roster Test",
            Specialty = "Testing",
            PhoneNumber = "0000000000"
        };
        context.DoctorProfiles.Add(doctor);
        await context.SaveChangesAsync();
        return doctor.Id;
    }

    [Fact]
    public async Task ValidRoster_ForAnExistingDoctor_Succeeds()
    {
        var doctorId = await SeedDoctor();
        using var context = _fixture.CreateContext();
        var controller = new DoctorsController(context);

        var result = await controller.UpdateRoster(new RosterUpdateRequestDto(
            doctorId, DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0), 30));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Roster_ForANonExistentDoctor_ReturnsNotFound()
    {
        using var context = _fixture.CreateContext();
        var controller = new DoctorsController(context);

        var result = await controller.UpdateRoster(new RosterUpdateRequestDto(
            Guid.NewGuid(), DayOfWeek.Monday, new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0), 30));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Roster_WithStartTimeAfterEndTime_IsRejected()
    {
        var doctorId = await SeedDoctor();
        using var context = _fixture.CreateContext();
        var controller = new DoctorsController(context);

        var result = await controller.UpdateRoster(new RosterUpdateRequestDto(
            doctorId, DayOfWeek.Monday, new TimeSpan(17, 0, 0), new TimeSpan(9, 0, 0), 30));

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
