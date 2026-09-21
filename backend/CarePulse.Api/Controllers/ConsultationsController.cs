using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/consultations")]
public class ConsultationsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public ConsultationsController(CarePulseDbContext db)
    {
        _db = db;
    }

    // POST /api/consultations/summary
    // Business operation: doctor finishes a visit, logs notes/prescription,
    // and the underlying slot is left as "Booked" (the visit already
    // happened - Booked here doubles as "completed" in this simplified model).
    [HttpPost("summary")]
    public async Task<ActionResult<ConsultationRecord>> CreateSummary(
        [FromBody] ConsultationSummaryRequestDto request)
    {
        var slot = await _db.AppointmentSlots.FirstOrDefaultAsync(s => s.Id == request.SlotId);

        if (slot is null)
        {
            return NotFound($"Slot {request.SlotId} not found.");
        }

        if (slot.Status != SlotStatus.Booked)
        {
            return BadRequest("Cannot record a consultation for a slot that was never booked.");
        }

        var alreadyExists = await _db.ConsultationRecords.AnyAsync(c => c.SlotId == request.SlotId);
        if (alreadyExists)
        {
            return Conflict("A consultation summary already exists for this slot.");
        }

        var record = new ConsultationRecord
        {
            SlotId = request.SlotId,
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            Notes = request.Notes,
            Prescription = request.Prescription
        };

        _db.ConsultationRecords.Add(record);
        await _db.SaveChangesAsync();

        return Ok(record);
    }
}
