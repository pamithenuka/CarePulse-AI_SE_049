using System.Net;
using System.Net.Http.Json;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarePulse.Api.Tests.Group;

/// <summary>
/// GROUP integrated workflow test for scheduling: doctor roster -> slot generation -> patient search and booking
/// -> double-booking protection -> consultation rules -> consultation record and access control.
/// Runs through the real HTTP pipeline (JWT, roles, ownership, services, EF Core).
/// </summary>
public class SchedulingWorkflowE2ETests(WebApplicationFactory<Program> factory) : SchedulingApiTestBase(factory)
{
    [Fact]
    public async Task SchedulingWorkflow_RosterSlotsSearchBookingConsultationAndRecordAccess_WorksEndToEnd()
    {
        using var doctor = ClientFor(DoctorAUser, "Doctor");
        using var patientA = ClientFor(PatientAUser, "Patient");
        using var patientB = ClientFor(PatientBUser, "Patient");

        // 1. The doctor sets a roster for every weekday (09:00-11:00, 30-minute slots).
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            var saved = await doctor.PutAsJsonAsync("/api/v1/doctors/roster", Roster(DoctorAId, (int)day, "09:00:00", "11:00:00", 30));
            Assert.Equal(HttpStatusCode.OK, saved.StatusCode);
        }
        var roster = await ReadJsonAsync(await doctor.GetAsync($"/api/v1/doctors/{DoctorAId}/roster"));
        Assert.Equal(7, roster.GetArrayLength());

        // 2. Slots are generated for the next 3 days: 4 slots a day, and a repeat run creates no duplicates.
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var generated = await doctor.PostAsJsonAsync($"/api/v1/doctors/{DoctorAId}/generate-slots", new { startDate = from, endDate = from.AddDays(2) });
        Assert.Equal(HttpStatusCode.OK, generated.StatusCode);
        Assert.Equal(12, (await ReadJsonAsync(generated)).GetProperty("slotsCreated").GetInt32());
        var again = await doctor.PostAsJsonAsync($"/api/v1/doctors/{DoctorAId}/generate-slots", new { startDate = from, endDate = from.AddDays(2) });
        Assert.Equal(0, (await ReadJsonAsync(again)).GetProperty("slotsCreated").GetInt32());

        // 3. Patient A finds open slots for this doctor and books the first one.
        var open = await ReadJsonAsync(await patientA.GetAsync($"/api/v1/doctors/slots?doctorId={DoctorAId}"));
        Assert.Equal(12, open.GetArrayLength());
        var slotId = open[0].GetProperty("slotId").GetGuid();
        var booked = await patientA.PostAsJsonAsync("/api/v1/appointments/book", new { slotId, patientId = PatientAId });
        Assert.Equal(HttpStatusCode.OK, booked.StatusCode);

        // 4. The same slot cannot be booked again (by anyone) and disappears from the open list.
        Assert.Equal(HttpStatusCode.Conflict, (await patientB.PostAsJsonAsync("/api/v1/appointments/book", new { slotId, patientId = PatientBId })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await patientA.PostAsJsonAsync("/api/v1/appointments/book", new { slotId, patientId = PatientAId })).StatusCode);
        var openAfter = await ReadJsonAsync(await patientA.GetAsync($"/api/v1/doctors/slots?doctorId={DoctorAId}"));
        Assert.Equal(11, openAfter.GetArrayLength());
        Assert.DoesNotContain(openAfter.EnumerateArray(), s => s.GetProperty("slotId").GetGuid() == slotId);

        // 5. The doctor sees the booking in the appointment list.
        var bookedList = await ReadJsonAsync(await doctor.GetAsync($"/api/v1/appointments/booked?doctorId={DoctorAId}"));
        Assert.Contains(bookedList.EnumerateArray(), b => b.GetProperty("slotId").GetGuid() == slotId && b.GetProperty("patientName").GetString() == "Patient A");

        // 6. A consultation cannot be completed before the appointment starts.
        var completeBody = new { slotId, doctorId = DoctorAId, patientId = PatientAId, notes = "Patient is recovering well.", prescription = "Rest and fluids" };
        Assert.Equal(HttpStatusCode.BadRequest, (await doctor.PostAsJsonAsync("/api/v1/consultations/complete", completeBody)).StatusCode);

        // 7. Time passes (the slot start moves into the past); the doctor completes the consultation once.
        MoveSlotStart(slotId, TimeSpan.FromMinutes(-45));
        var completed = await doctor.PostAsJsonAsync("/api/v1/consultations/complete", completeBody);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        var consultationId = (await ReadJsonAsync(completed)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await doctor.PostAsJsonAsync("/api/v1/consultations/complete", completeBody)).StatusCode);

        // 8. The record is saved in the database and is visible to its patient only.
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
            var record = await db.ConsultationRecords.SingleAsync(c => c.Id == consultationId);
            Assert.Equal(slotId, record.SlotId);
            Assert.Equal("Rest and fluids", record.Prescription);
        }
        Assert.Equal(HttpStatusCode.OK, (await patientA.GetAsync($"/api/v1/consultations/{consultationId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await patientB.GetAsync($"/api/v1/consultations/{consultationId}")).StatusCode);

        // 9. The finished appointment no longer appears in the doctor's list of pending appointments.
        var pending = await ReadJsonAsync(await doctor.GetAsync($"/api/v1/appointments/booked?doctorId={DoctorAId}"));
        Assert.DoesNotContain(pending.EnumerateArray(), b => b.GetProperty("slotId").GetGuid() == slotId);
    }

    // Simultaneous-booking protection depends on the PostgreSQL xmin concurrency token, which the in-memory
    // provider does not maintain, so it is NOT tested here. It is covered on a real PostgreSQL server by
    // AppointmentBookingTests.TwoSimultaneousBookingAttempts_OnlyOneSucceeds.
}
