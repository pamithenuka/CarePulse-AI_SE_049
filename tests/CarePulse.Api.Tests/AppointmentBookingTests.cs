using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class AppointmentBookingTests
{
    private readonly TestDatabaseFixture _fixture;

    public AppointmentBookingTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

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

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task BookingAnAlreadyBookedSlot_ReturnsConflict()
    {
        var slotId = await SeedOpenSlot();

        using (var firstContext = _fixture.CreateContext())
        {
            var firstController = new AppointmentsController(firstContext);
            await firstController.BookAppointment(
                new BookAppointmentRequestDto(slotId, Guid.NewGuid()));
        }

        using var secondContext = _fixture.CreateContext();
        var secondController = new AppointmentsController(secondContext);
        var result = await secondController.BookAppointment(
            new BookAppointmentRequestDto(slotId, Guid.NewGuid()));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    // THE key concurrency test required by the assignment brief.
    // Two "requests" read the slot while it's still Open, before either
    // has saved. Only one booking may succeed - the other must be told
    // the slot is gone, never silently overwritten.
    [Fact]
    public async Task TwoSimultaneousBookingAttempts_OnlyOneSucceeds()
    {
        var slotId = await SeedOpenSlot();

        using var contextA = _fixture.CreateContext();
        using var contextB = _fixture.CreateContext();

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

        var outcomes = new object?[] { resultA.Result, resultB.Result };
        Assert.Single(outcomes, r => r is OkObjectResult);
        Assert.Single(outcomes, r => r is ConflictObjectResult);
    }
}
