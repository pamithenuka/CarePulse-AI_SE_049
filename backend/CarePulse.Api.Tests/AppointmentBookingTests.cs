using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class AppointmentBookingTests
{
    private readonly TestDatabaseFixture _fixture;

    public AppointmentBookingTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // Helper: creates a fresh doctor + one open slot for a test to use.
    // A brand new doctor per test keeps tests independent of each other.
    private async Task<Guid> SeedOpenSlot()
    {
        using var context = _fixture.CreateContext();

        var doctor = new DoctorProfile
        {
            FullName = "Dr. Test Doctor",
            Specialty = "Testing",
            PhoneNumber = "0000000000"
        };
        context.DoctorProfiles.Add(doctor);

        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotStart = DateTime.UtcNow.AddDays(1),
            SlotEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = SlotStatus.Open
        };
        context.AppointmentSlots.Add(slot);

        await context.SaveChangesAsync();
        return slot.Id;
    }

    [Fact]
    public async Task BookingAnOpenSlot_Succeeds()
    {
        var slotId = await SeedOpenSlot();
        using var context = _fixture.CreateContext();
        var controller = new AppointmentsController(context);

        var result = await controller.BookAppointment(
            new BookAppointmentRequestDto(slotId, Guid.NewGuid()));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BookAppointmentResponseDto>(okResult.Value);
        Assert.Equal(SlotStatus.Booked, response.Status);
    }

    [Fact]
    public async Task BookingAnAlreadyBookedSlot_ReturnsConflict()
    {
        var slotId = await SeedOpenSlot();

        // First booking - should succeed and mark the slot Booked.
        using (var firstContext = _fixture.CreateContext())
        {
            var firstController = new AppointmentsController(firstContext);
            await firstController.BookAppointment(
                new BookAppointmentRequestDto(slotId, Guid.NewGuid()));
        }

        // A second, later attempt on the same now-booked slot.
        using var secondContext = _fixture.CreateContext();
        var secondController = new AppointmentsController(secondContext);
        var result = await secondController.BookAppointment(
            new BookAppointmentRequestDto(slotId, Guid.NewGuid()));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // THE key concurrency test required by the assignment brief.
    // Simulates two patients trying to book the SAME slot at effectively
    // the same instant: both "requests" read the slot while it's still
    // Open, before either one has saved. Only one booking may succeed -
    // the other must be told the slot is gone, never silently overwritten.
    [Fact]
    public async Task TwoSimultaneousBookingAttempts_OnlyOneSucceeds()
    {
        var slotId = await SeedOpenSlot();

        // Two separate DbContexts stand in for two separate concurrent
        // web requests - each has its own independent view of the data.
        using var contextA = _fixture.CreateContext();
        using var contextB = _fixture.CreateContext();

        // Both "requests" read the slot before either has booked it.
        var slotSeenByA = await contextA.AppointmentSlots.FindAsync(slotId);
        var slotSeenByB = await contextB.AppointmentSlots.FindAsync(slotId);
        Assert.NotNull(slotSeenByA);
        Assert.NotNull(slotSeenByB);

        var controllerA = new AppointmentsController(contextA);
        var controllerB = new AppointmentsController(contextB);

        var resultA = await controllerA.BookAppointment(
            new BookAppointmentRequestDto(slotId, Guid.NewGuid()));
        var resultB = await controllerB.BookAppointment(
            new BookAppointmentRequestDto(slotId, Guid.NewGuid()));

        // Exactly one of the two must have succeeded (200 OK) and the
        // other must have been rejected (409 Conflict) - never both
        // succeeding, which would mean two patients hold the same slot.
        var outcomes = new object?[] { resultA.Result, resultB.Result };
        Assert.Single(outcomes, r => r is OkObjectResult);
        Assert.Single(outcomes, r => r is ConflictObjectResult);
    }
}
