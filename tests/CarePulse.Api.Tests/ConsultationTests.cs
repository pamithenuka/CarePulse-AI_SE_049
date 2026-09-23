using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CarePulse.Api.Tests;

[Collection("Database collection")]
public class ConsultationTests
{
    private readonly TestDatabaseFixture _fixture;

    public ConsultationTests(TestDatabaseFixture fixture)
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
    public async Task CompletingAConsultation_ForABookedSlot_Succeeds()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();
        using var context = _fixture.CreateContext();
        var controller = new ConsultationsController(context);

        var result = await controller.CompleteConsultation(new ConsultationCompleteRequestDto(
            slotId, doctorId, patientId, "Patient is doing well.", null));

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task CompletingASecondConsultation_ForTheSameSlot_ReturnsConflict()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();

        using (var firstContext = _fixture.CreateContext())
        {
            var firstController = new ConsultationsController(firstContext);
            await firstController.CompleteConsultation(new ConsultationCompleteRequestDto(
                slotId, doctorId, patientId, "First visit note.", null));
        }

        using var secondContext = _fixture.CreateContext();
        var secondController = new ConsultationsController(secondContext);
        var result = await secondController.CompleteConsultation(new ConsultationCompleteRequestDto(
            slotId, doctorId, patientId, "Accidental duplicate entry.", null));

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CompletingAConsultation_ForASlotThatWasNeverBooked_ReturnsBadRequest()
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
            Status = SlotStatus.Open
        };
        context.AppointmentSlots.Add(openSlot);
        await context.SaveChangesAsync();

        var controller = new ConsultationsController(context);
        var result = await controller.CompleteConsultation(new ConsultationCompleteRequestDto(
            openSlot.Id, doctor.Id, Guid.NewGuid(), "Should not be allowed.", null));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GettingAConsultation_ByItsId_ReturnsTheRecord()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();

        Guid createdId;
        using (var writeContext = _fixture.CreateContext())
        {
            var writeController = new ConsultationsController(writeContext);
            var createResult = await writeController.CompleteConsultation(new ConsultationCompleteRequestDto(
                slotId, doctorId, patientId, "Routine check-up.", "Paracetamol 500mg"));

            var ok = Assert.IsType<OkObjectResult>(createResult);
            var record = Assert.IsType<ConsultationRecord>(ok.Value);
            createdId = record.Id;
        }

        using var readContext = _fixture.CreateContext();
        var readController = new ConsultationsController(readContext);
        var getResult = await readController.GetConsultation(createdId);

        var getOk = Assert.IsType<OkObjectResult>(getResult);
        var fetched = Assert.IsType<ConsultationRecord>(getOk.Value);
        Assert.Equal("Routine check-up.", fetched.Notes);
    }
}
