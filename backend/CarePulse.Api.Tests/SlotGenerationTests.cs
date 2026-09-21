using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

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

    // Finds the date of the next Monday on or after "from", so this test
    // works correctly no matter what day it's actually run on.
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
        Assert.Equal(2, slotsCreated); // 9:00-9:30 and 9:30-10:00
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

        // Still only 2 slots total, not 4 - the second call should have
        // skipped every slot that already existed from the first call.
        Assert.Equal(2, totalSlots);
    }
}
