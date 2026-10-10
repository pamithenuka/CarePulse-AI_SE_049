using CarePulse.Api.Tests.Fakes;
using CarePulse.Api.Controllers;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        var patientId = await TestActors.Patient(context);
        var slot = new AppointmentSlot
        {
            DoctorId = doctor.Id,
            SlotStart = DateTime.UtcNow.AddMinutes(-30),
            SlotEnd = DateTime.UtcNow.AddMinutes(-30).AddMinutes(30),
            Status = SlotStatus.Booked,
            PatientId = patientId
        };
        context.AppointmentSlots.Add(slot);

        await context.SaveChangesAsync();
        return (doctor.Id, slot.Id, patientId);
    }

    [Fact]
    public async Task FutureAppointment_IsRejected_WithoutPersistingConsultation()
    {
        var (doctor, slot, patient) = await SeedBookedSlot();
        using (var setup = _fixture.CreateContext())
        {
            var booking = await setup.AppointmentSlots.FindAsync(slot);
            booking!.SlotStart = DateTime.UtcNow.AddHours(1);
            booking.SlotEnd = booking.SlotStart.AddMinutes(30);
            await setup.SaveChangesAsync();
        }
        using var context = _fixture.CreateContext();
        var result = await new ConsultationsController(context).As().CompleteConsultation(
            new(slot, doctor, patient, "Synthetic early completion", null));
        Assert.IsType<BadRequestObjectResult>(result);
        using var verify = _fixture.CreateContext();
        Assert.False(await verify.ConsultationRecords.AnyAsync(c => c.SlotId == slot));
        Assert.Equal(SlotStatus.Booked, (await verify.AppointmentSlots.FindAsync(slot))!.Status);
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(4000, 2000, true)]
    [InlineData(4001, 0, false)]
    [InlineData(10, 2001, false)]
    public async Task NoteAndPrescriptionLimits_EnforceBoundaryAndPersistence(int notesLength, int prescriptionLength, bool accepted)
    {
        var (doctor, slot, patient) = await SeedBookedSlot();
        var notes = new string('n', notesLength);
        var prescription = new string('p', prescriptionLength);
        using var context = _fixture.CreateContext();
        var result = await new ConsultationsController(context).As().CompleteConsultation(
            new(slot, doctor, patient, notes, prescription));
        if (accepted) Assert.IsType<OkObjectResult>(result);
        else Assert.IsType<BadRequestObjectResult>(result);
        using var verify = _fixture.CreateContext();
        var saved = await verify.ConsultationRecords.SingleOrDefaultAsync(c => c.SlotId == slot);
        if (accepted)
        {
            Assert.NotNull(saved);
            Assert.Equal(notes, saved.Notes);
            Assert.Equal(prescription, saved.Prescription);
            Assert.Equal(patient, saved.PatientId);
            Assert.Equal(doctor, saved.DoctorId);
        }
        else Assert.Null(saved);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MismatchedBookingIdentity_IsRejected_WithoutWritingRecord(bool wrongPatient)
    {
        var (doctor, slot, patient) = await SeedBookedSlot();
        using var context = _fixture.CreateContext();
        var result = await new ConsultationsController(context).As().CompleteConsultation(
            new(slot, wrongPatient ? doctor : Guid.NewGuid(), wrongPatient ? Guid.NewGuid() : patient,
                "Synthetic mismatched identity", null));
        Assert.IsType<BadRequestObjectResult>(result);
        using var verify = _fixture.CreateContext();
        Assert.False(await verify.ConsultationRecords.AnyAsync(c => c.SlotId == slot));
    }

    [Fact]
    public async Task CompletingAConsultation_ForABookedSlot_Succeeds()
    {
        var (doctorId, slotId, patientId) = await SeedBookedSlot();
        using var context = _fixture.CreateContext();
        var controller = new ConsultationsController(context).As();

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
            var firstController = new ConsultationsController(firstContext).As();
            await firstController.CompleteConsultation(new ConsultationCompleteRequestDto(
                slotId, doctorId, patientId, "First visit note.", null));
        }

        using var secondContext = _fixture.CreateContext();
        var secondController = new ConsultationsController(secondContext).As();
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
            SlotStart = DateTime.UtcNow.AddMinutes(-30),
            SlotEnd = DateTime.UtcNow.AddMinutes(-30).AddMinutes(30),
            Status = SlotStatus.Open
        };
        context.AppointmentSlots.Add(openSlot);
        await context.SaveChangesAsync();

        var controller = new ConsultationsController(context).As();
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
            var writeController = new ConsultationsController(writeContext).As();
            var createResult = await writeController.CompleteConsultation(new ConsultationCompleteRequestDto(
                slotId, doctorId, patientId, "Routine check-up.", "Paracetamol 500mg"));

            var ok = Assert.IsType<OkObjectResult>(createResult);
            var record = Assert.IsType<ConsultationRecord>(ok.Value);
            createdId = record.Id;
        }

        using var readContext = _fixture.CreateContext();
        var readController = new ConsultationsController(readContext).As();
        var getResult = await readController.GetConsultation(createdId);

        var getOk = Assert.IsType<OkObjectResult>(getResult);
        var fetched = Assert.IsType<ConsultationRecord>(getOk.Value);
        Assert.Equal("Routine check-up.", fetched.Notes);
    }
}
