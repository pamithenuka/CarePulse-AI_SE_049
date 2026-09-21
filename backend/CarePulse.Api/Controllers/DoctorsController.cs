using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public DoctorsController(CarePulseDbContext db)
    {
        _db = db;
    }
        // GET /api/doctors
    // Lists all active doctors - used by the frontend to populate a doctor picker.
    [HttpGet]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _db.DoctorProfiles
            .Where(d => d.IsActive)
            .OrderBy(d => d.FullName)
            .Select(d => new { d.Id, d.FullName, d.Specialty })
            .ToListAsync();

        return Ok(doctors);
    }
    // GET /api/doctors/slots?date=2026-09-20&specialty=Cardiology
    // Search open appointment slots, filtered by date and/or specialty.
    [HttpGet("slots")]
    public async Task<ActionResult<IEnumerable<SlotSearchResultDto>>> GetOpenSlots(
        [FromQuery] DateOnly? date,
        [FromQuery] string? specialty,
        [FromQuery] Guid? doctorId)
    {
        var query = _db.AppointmentSlots
            .Include(s => s.Doctor)
            .Where(s => s.Status == SlotStatus.Open);

        if (doctorId is not null)
        {
            query = query.Where(s => s.DoctorId == doctorId);
        }

                if (date is not null)
        {
            var dayStart = DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(s => s.SlotStart >= dayStart && s.SlotStart < dayEnd);
        }

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            query = query.Where(s => s.Doctor!.Specialty.ToLower() == specialty.ToLower());
        }

        var results = await query
            .OrderBy(s => s.SlotStart)
            .Select(s => new SlotSearchResultDto(
                s.Id,
                s.DoctorId,
                s.Doctor!.FullName,
                s.Doctor!.Specialty,
                s.SlotStart,
                s.SlotEnd))
            .ToListAsync();

        return Ok(results);
    }
        // GET /api/doctors/{doctorId}/roster
    // Returns this doctor's currently saved weekly availability.
    [HttpGet("{doctorId}/roster")]
    public async Task<IActionResult> GetRoster(Guid doctorId)
    {
        var roster = await _db.ClinicRosters
            .Where(r => r.DoctorId == doctorId && r.IsActive)
            .OrderBy(r => r.DayOfWeek)
            .Select(r => new
            {
                r.DayOfWeek,
                StartTime = r.StartTime.ToString(@"hh\:mm"),
                EndTime = r.EndTime.ToString(@"hh\:mm"),
                r.SlotDurationMinutes
            })
            .ToListAsync();

        return Ok(roster);
    }
        // POST /api/doctors/{doctorId}/generate-slots
    // Turns a doctor's recurring roster (e.g. "Mondays 9-1") into real,
    // individually bookable AppointmentSlot rows across a date range.
    // Safe to call more than once - it skips any slot that already exists.
    [HttpPost("{doctorId}/generate-slots")]
    public async Task<IActionResult> GenerateSlots(Guid doctorId, [FromBody] GenerateSlotsRequestDto request)
    {
        var doctor = await _db.DoctorProfiles.FindAsync(doctorId);
        if (doctor is null)
        {
            return NotFound($"Doctor {doctorId} not found.");
        }

        if (request.EndDate < request.StartDate)
        {
            return BadRequest("EndDate must be on or after StartDate.");
        }

        var rosters = await _db.ClinicRosters
            .Where(r => r.DoctorId == doctorId && r.IsActive)
            .ToListAsync();

        if (rosters.Count == 0)
        {
            return BadRequest("This doctor has no active roster entries yet. Set one via PUT /api/doctors/roster first.");
        }

        var existingStarts = (await _db.AppointmentSlots
            .Where(s => s.DoctorId == doctorId)
            .Select(s => s.SlotStart)
            .ToListAsync())
            .ToHashSet();

        var newSlots = new List<AppointmentSlot>();

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            var roster = rosters.FirstOrDefault(r => r.DayOfWeek == date.DayOfWeek);
            if (roster is null)
            {
                continue; // doctor doesn't work this day of the week
            }

            var slotStart = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.FromTimeSpan(roster.StartTime)), DateTimeKind.Utc);
            var dayEnd = DateTime.SpecifyKind(
                date.ToDateTime(TimeOnly.FromTimeSpan(roster.EndTime)), DateTimeKind.Utc);

            while (slotStart.AddMinutes(roster.SlotDurationMinutes) <= dayEnd)
            {
                var slotEnd = slotStart.AddMinutes(roster.SlotDurationMinutes);

                if (!existingStarts.Contains(slotStart))
                {
                    newSlots.Add(new AppointmentSlot
                    {
                        DoctorId = doctorId,
                        SlotStart = slotStart,
                        SlotEnd = slotEnd,
                        Status = SlotStatus.Open
                    });
                    existingStarts.Add(slotStart);
                }

                slotStart = slotEnd;
            }
        }

        _db.AppointmentSlots.AddRange(newSlots);
        await _db.SaveChangesAsync();

        return Ok(new { SlotsCreated = newSlots.Count });
    }

    // PUT /api/doctors/roster
    // Doctor updates their recurring weekly availability. This does not
    // retroactively touch already-generated slots.
    [HttpPut("roster")]
    public async Task<IActionResult> UpdateRoster([FromBody] RosterUpdateRequestDto request)
    {
        var doctorExists = await _db.DoctorProfiles.AnyAsync(d => d.Id == request.DoctorId);
        if (!doctorExists)
        {
            return NotFound($"Doctor {request.DoctorId} not found.");
        }

        if (request.StartTime >= request.EndTime)
        {
            return BadRequest("StartTime must be before EndTime.");
        }

        var roster = await _db.ClinicRosters.FirstOrDefaultAsync(r =>
            r.DoctorId == request.DoctorId && r.DayOfWeek == request.DayOfWeek);

        if (roster is null)
        {
            roster = new ClinicRoster { DoctorId = request.DoctorId, DayOfWeek = request.DayOfWeek };
            _db.ClinicRosters.Add(roster);
        }

        roster.StartTime = request.StartTime;
        roster.EndTime = request.EndTime;
        roster.SlotDurationMinutes = request.SlotDurationMinutes;
        roster.IsActive = true;

        await _db.SaveChangesAsync();

        return Ok(roster);
    }
}
