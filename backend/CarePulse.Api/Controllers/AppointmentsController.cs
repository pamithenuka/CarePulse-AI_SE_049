using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public AppointmentsController(CarePulseDbContext db)
    {
        _db = db;
    }

    // POST /api/appointments/book
    // Reserves an open slot for a patient. This is the endpoint that must
    // never let two patients book the same slot.
    //
    // How the safety works:
    // 1. We load the slot as normal (EF tracks its RowVersion).
    // 2. We check Status == Open in memory.
    // 3. We call SaveChangesAsync(). Postgres compares the RowVersion we
    //    read against the current row. If another request already booked
    //    this slot in between our read and our write, the RowVersion will
    //    have changed and SaveChangesAsync throws DbUpdateConcurrencyException.
    // 4. We catch that and return 409 Conflict - the slot was taken by
    //    someone else a moment ago.
    [HttpPost("book")]
    public async Task<ActionResult<BookAppointmentResponseDto>> BookAppointment(
        [FromBody] BookAppointmentRequestDto request)
    {
        var slot = await _db.AppointmentSlots.FirstOrDefaultAsync(s => s.Id == request.SlotId);

        if (slot is null)
        {
            return NotFound($"Slot {request.SlotId} not found.");
        }

        if (slot.Status != SlotStatus.Open)
        {
            return Conflict(new BookAppointmentResponseDto(
                slot.Id, slot.Status, slot.SlotStart, slot.SlotEnd,
                "This slot is no longer available."));
        }

        slot.Status = SlotStatus.Booked;
        slot.PatientId = request.PatientId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone else booked this exact slot in the tiny window between
            // our read and our write. Tell the caller honestly.
            return Conflict(new BookAppointmentResponseDto(
                slot.Id, SlotStatus.Booked, slot.SlotStart, slot.SlotEnd,
                "This slot was just booked by someone else. Please pick another time."));
        }

        return Ok(new BookAppointmentResponseDto(
            slot.Id, slot.Status, slot.SlotStart, slot.SlotEnd,
            "Appointment booked successfully."));
    }
}
