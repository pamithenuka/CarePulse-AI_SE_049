using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Authorization;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[Authorize]
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

        await _db.RequirePatientAsync(User, record.PatientId);
        if (User.IsInRole("Doctor")) await _db.RequireDoctorAsync(User, record.DoctorId);
        return Ok(record);
    }

    // GET /api/v1/consultations/patient/{patientId}
    // A patient's consultation history, newest first. A patient sees only their own;
    // Doctor and Admin accounts may view any patient (same rule as the other patient endpoints).
    [HttpGet("patient/{patientId:guid}")]
    public async Task<ActionResult<IEnumerable<ConsultationSummaryDto>>> GetPatientConsultations(Guid patientId)
    {
        await _db.RequirePatientAsync(User, patientId);

        var records = await _db.ConsultationRecords
            .AsNoTracking()
            .Include(c => c.Slot)
            .Where(c => c.PatientId == patientId)
            .ToListAsync();

        // Soft-deleted doctors are still named on their old consultations.
        var doctorIds = records.Select(c => c.DoctorId).Distinct().ToList();
        var doctors = await _db.DoctorProfiles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(d => doctorIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id);

        var history = records
            .Select(c =>
            {
                doctors.TryGetValue(c.DoctorId, out var doctor);
                return new ConsultationSummaryDto(
                    c.Id, c.SlotId, c.DoctorId,
                    doctor?.FullName ?? "Unknown doctor",
                    doctor?.Specialty ?? string.Empty,
                    c.Slot?.SlotStart ?? default,
                    c.Slot?.SlotEnd ?? default,
                    c.Notes, c.Prescription);
            })
            .OrderByDescending(c => c.AppointmentStart)
            .ToList();

        return Ok(history);
    }

    // POST /api/v1/consultations/complete
    // Business operation: doctor finishes a visit, logs notes/prescription.
    [HttpPost("complete")]
    [Authorize(Roles = "Doctor,Admin")]
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

        await _db.RequireDoctorAsync(User, slot.DoctorId);
        if (slot.PatientId != request.PatientId || slot.DoctorId != request.DoctorId)
            return BadRequest("Doctor and patient must match the booking.");
        if (slot.SlotStart > DateTime.UtcNow) return BadRequest("The appointment has not started yet.");
        if (string.IsNullOrWhiteSpace(request.Notes) || request.Notes.Length > 4000 || request.Prescription?.Length > 2000)
            return BadRequest("Notes are required (maximum 4000 characters); prescriptions may contain at most 2000 characters.");
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