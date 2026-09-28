using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/v1/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public AppointmentsController(CarePulseDbContext db)
    {
        _db = db;
    }

    // POST /api/v1/appointments/book
    // Reserves an open slot for a patient. Uses AppointmentSlot.RowVersion
    // (optimistic concurrency) so two simultaneous bookings on the same
    // slot can never both succeed - the second write is rejected with a
    // clean 409 Conflict instead of silently overwriting the first.
    [HttpPost("book")]
    public async Task<ActionResult<object>> BookAppointment([FromBody] BookAppointmentRequestDto request)
    {
        var slot = await _db.AppointmentSlots.FirstOrDefaultAsync(s => s.Id == request.SlotId);

        if (slot is null)
        {
            return NotFound($"Slot {request.SlotId} not found.");
        }

        if (slot.Status != SlotStatus.Open)
        {
            return Conflict(new { slot.Id, slot.Status, Message = "This slot is no longer available." });
        }

        slot.Status = SlotStatus.Booked;
        slot.PatientId = request.PatientId;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                slot.Id,
                Status = SlotStatus.Booked,
                Message = "This slot was just booked by someone else. Please pick another time."
            });
        }

        return Ok(new
        {
            slot.Id,
            slot.Status,
            slot.SlotStart,
            slot.SlotEnd,
            Message = "Appointment booked successfully."
        });
    }
}