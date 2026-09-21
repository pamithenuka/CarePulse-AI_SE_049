using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class ConsultationSummaryTests
{
    private readonly TestDatabaseFixture _fixture;

    public ConsultationSummaryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<(Guid doctorId, Guid bookedSlotId, Guid patientId)> SeedBookedSlot()
    {
        using var context = _fixture.CreateContext();

        var doctor = new DoctorProfile
        {
            FullName = "Dr. Test Doctor",
            Specialty = "Testing",
            PhoneNumber = "0000000000"
        };
        context.DoctorProfiles.Add(doctor);

        var patientId = Guid.NewGuid();
        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotStart = DateTime.UtcNow.AddDays(1),
            SlotEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = SlotStatus.Booked,
            PatientId = patientId
        };
        context.AppointmentSlots.Add(slot);

        await context.SaveChangesAsync();
        return (doctor.Id, slot.Id, patientId);
    }

    [Fact]
    public async Task LoggingASummary_ForABookedSlot_Succeeds()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();
        using var context = _fixture.CreateContext();
        var controller = new ConsultationsController(context);

        var result = await controller.CreateSummary(new ConsultationSummaryRequestDto(
            slotId, doctorId, patientId, "Patient is doing well.", null));

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task LoggingASecondSummary_ForTheSameSlot_ReturnsConflict()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();

        using (var firstContext = _fixture.CreateContext())
        {
            var firstController = new ConsultationsController(firstContext);
            await firstController.CreateSummary(new ConsultationSummaryRequestDto(
                slotId, doctorId, patientId, "First visit note.", null));
        }

        using var secondContext = _fixture.CreateContext();
        var secondController = new ConsultationsController(secondContext);
        var result = await secondController.CreateSummary(new ConsultationSummaryRequestDto(
            slotId, doctorId, patientId, "Accidental duplicate entry.", null));

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task LoggingASummary_ForASlotThatWasNeverBooked_ReturnsBadRequest()
    {
        using var context = _fixture.CreateContext();

        var doctor = new DoctorProfile
        {
            FullName = "Dr. Unbooked",
            Specialty = "Testing",
            PhoneNumber = "0000000000"
        };
        context.DoctorProfiles.Add(doctor);

        var openSlot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotStart = DateTime.UtcNow.AddDays(1),
            SlotEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = SlotStatus.Open // never booked
        };
        context.AppointmentSlots.Add(openSlot);
        await context.SaveChangesAsync();

        var controller = new ConsultationsController(context);
        var result = await controller.CreateSummary(new ConsultationSummaryRequestDto(
            openSlot.Id, doctor.Id, Guid.NewGuid(), "Should not be allowed.", null));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }
}
