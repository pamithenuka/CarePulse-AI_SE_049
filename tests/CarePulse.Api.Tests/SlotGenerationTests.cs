using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class SlotGenerationTests
{
    private readonly TestDatabaseFixture _fixture;

    public SlotGenerationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<Guid> SeedDoctorWithRoster()
    {
        using var context = _fixture.CreateContext();
        var doctor = new DoctorProfile
        {
            FullName = "Dr. Slot Gen",
            Specialty = "Testing",
            PhoneNumber = "0000000000"
        };
        context.DoctorProfiles.Add(doctor);

        // Every Monday, 9:00-10:00, in 30-minute slots = 2 slots per Monday.
        context.ClinicRosters.Add(new ClinicRoster
        {
            DoctorId = doctor.Id,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            SlotDurationMinutes = 30
        });

        await context.SaveChangesAsync();
        return doctor.Id;
    }

    private static DateOnly NextMonday(DateOnly from)
    {
        while (from.DayOfWeek != DayOfWeek.Monday)
        {
            from = from.AddDays(1);
        }
        return from;
    }

    [Fact]
    public async Task GeneratingSlots_ForAWeekWithOneMonday_CreatesExpectedCount()
    {
        var doctorId = await SeedDoctorWithRoster();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));

        using var context = _fixture.CreateContext();
        var controller = new DoctorsController(context);

        var result = await controller.GenerateSlots(doctorId,
            new GenerateSlotsRequestDto(monday, monday.AddDays(6)));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var slotsCreated = (int)okResult.Value!.GetType().GetProperty("SlotsCreated")!.GetValue(okResult.Value)!;
        Assert.Equal(2, slotsCreated);
    }

    [Fact]
    public async Task GeneratingSlots_TwiceForTheSameRange_DoesNotCreateDuplicates()
    {
        var doctorId = await SeedDoctorWithRoster();
        var monday = NextMonday(DateOnly.FromDateTime(DateTime.UtcNow));
        var request = new GenerateSlotsRequestDto(monday, monday.AddDays(6));

        using (var firstContext = _fixture.CreateContext())
        {
            var firstController = new DoctorsController(firstContext);
            await firstController.GenerateSlots(doctorId, request);
        }

        using (var secondContext = _fixture.CreateContext())
        {
            var secondController = new DoctorsController(secondContext);
            await secondController.GenerateSlots(doctorId, request);
        }

        using var verifyContext = _fixture.CreateContext();
        var totalSlots = await verifyContext.AppointmentSlots
            .CountAsync(s => s.DoctorId == doctorId);

        Assert.Equal(2, totalSlots);
    }
}
