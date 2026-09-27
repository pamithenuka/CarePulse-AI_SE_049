using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/v1/consultations")]
public class ConsultationsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public ConsultationsController(CarePulseDbContext db)
    {
        _db = db;
    }

    // GET /api/v1/consultations/{id}
    // Fetches a single consultation record by its own Id.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetConsultation(Guid id)
    {
        var record = await _db.ConsultationRecords.FirstOrDefaultAsync(c => c.Id == id);

        if (record is null)
        {
            return NotFound($"Consultation {id} not found.");
        }

        return Ok(record);
    }

    // POST /api/v1/consultations/complete
    // Business operation: doctor finishes a visit, logs notes/prescription.
    [HttpPost("complete")]
    public async Task<IActionResult> CompleteConsultation([FromBody] ConsultationCompleteRequestDto request)
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