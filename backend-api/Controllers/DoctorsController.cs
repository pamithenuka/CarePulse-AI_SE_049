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
[Route("api/v1/doctors")]
public class DoctorsController : ControllerBase
{
    private readonly CarePulseDbContext _db;

    public DoctorsController(CarePulseDbContext db)
    {
        _db = db;
    }

    // GET /api/v1/doctors
    [HttpGet]
    public async Task<IActionResult> GetDoctors()
    {
        var doctors = await _db.DoctorProfiles            
            .OrderBy(d => d.FullName)
            .Select(d => new { d.Id, d.UserId, d.FullName, d.Specialty, d.PhoneNumber, d.Email })
            .ToListAsync();

        return Ok(doctors);
    }


    // GET /api/v1/doctors/slots?date=2026-09-20&specialty=Cardiology&doctorId=...
    [HttpGet("slots")]
    public async Task<ActionResult<IEnumerable<SlotSearchResultDto>>> GetOpenSlots(
        [FromQuery] DateOnly? date,
        [FromQuery] string? specialty,
        [FromQuery] Guid? doctorId)
    {
        var query = _db.AppointmentSlots
            .Include(s => s.Doctor)
            .Where(s => s.Status == SlotStatus.Open && s.SlotStart > DateTime.UtcNow);

        if (doctorId is not null)
        {
            query = query.Where(s => s.DoctorId == doctorId);
        }

        if (date is not null)
        {
            var dayStart = ClinicTime.ToUtc(date.Value, TimeSpan.Zero);
            var dayEnd = dayStart.AddDays(1);
            query = query.Where(s => s.SlotStart >= dayStart && s.SlotStart < dayEnd);
        }

        if (!string.IsNullOrWhiteSpace(specialty))
        {
            query = query.Where(s => s.Doctor!.Specialty.Replace(" ", "_").ToLower() == specialty.Replace(" ", "_").ToLower());
        }

        var results = await query
            .OrderBy(s => s.SlotStart)
            .Select(s => new SlotSearchResultDto(
                s.Id, s.DoctorId, s.Doctor!.FullName, s.Doctor!.Specialty, s.SlotStart, s.SlotEnd))
            .ToListAsync();

        return Ok(results);
    }

    // GET /api/v1/doctors/{doctorId}/roster
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

    // POST /api/v1/doctors/{doctorId}/generate-slots
    [HttpPost("{doctorId}/generate-slots")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GenerateSlots(Guid doctorId, [FromBody] GenerateSlotsRequestDto request)
    {
        await _db.RequireDoctorAsync(User, doctorId);
        var doctor = await _db.DoctorProfiles.FindAsync(doctorId);
        if (doctor is null)
        {
            return NotFound($"Doctor {doctorId} not found.");
        }

        if (request.EndDate < request.StartDate || request.EndDate.DayNumber - request.StartDate.DayNumber > 90)
        {
            return BadRequest("EndDate must be on or after StartDate.");
        }

        var rosters = await _db.ClinicRosters
            .Where(r => r.DoctorId == doctorId && r.IsActive)
            .ToListAsync();

        if (rosters.Count == 0)
        {
            return BadRequest("This doctor has no active roster entries yet. Set one via PUT /api/v1/doctors/roster first.");
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
            if (roster is null) continue;
            if (roster.SlotDurationMinutes < 5 || roster.SlotDurationMinutes > 240)
                return BadRequest("Correct the invalid roster duration before generating slots.");

            var slotStart = ClinicTime.ToUtc(date, roster.StartTime);
            var dayEnd = ClinicTime.ToUtc(date, roster.EndTime);

            while (slotStart.AddMinutes(roster.SlotDurationMinutes) <= dayEnd)
            {
                var slotEnd = slotStart.AddMinutes(roster.SlotDurationMinutes);

                if (slotStart > DateTime.UtcNow && !existingStarts.Contains(slotStart))
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

    // PUT /api/v1/doctors/roster
    [HttpPut("roster")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> UpdateRoster([FromBody] RosterUpdateRequestDto request)
    {
        if (!Enum.IsDefined(request.DayOfWeek) || request.SlotDurationMinutes < 5 || request.SlotDurationMinutes > 240 ||
            request.StartTime < TimeSpan.Zero || request.EndTime > TimeSpan.FromDays(1) ||
            request.EndTime - request.StartTime < TimeSpan.FromMinutes(request.SlotDurationMinutes))
            return BadRequest("Use a valid weekday, a 5–240 minute slot, and a valid daily time range.");
        var doctorExists = await _db.DoctorProfiles.AnyAsync(d => d.Id == request.DoctorId);
        if (!doctorExists)
        {
            return NotFound($"Doctor {request.DoctorId} not found.");
        }

        await _db.RequireDoctorAsync(User, request.DoctorId);
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
