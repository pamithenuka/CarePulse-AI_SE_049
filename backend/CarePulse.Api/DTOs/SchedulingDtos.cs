using CarePulse.Api.Models;

namespace CarePulse.Api.DTOs;

public record SlotSearchResultDto(
    Guid SlotId,
    Guid DoctorId,
    string DoctorName,
    string Specialty,
    DateTime SlotStart,
    DateTime SlotEnd
);

public record BookAppointmentRequestDto(
    Guid SlotId,
    Guid PatientId
);

public record BookAppointmentResponseDto(
    Guid SlotId,
    SlotStatus Status,
    DateTime SlotStart,
    DateTime SlotEnd,
    string Message
);

public record GenerateSlotsRequestDto(
    DateOnly StartDate,
    DateOnly EndDate
);

public record RosterUpdateRequestDto(
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes
);

public record ConsultationSummaryRequestDto(
    Guid SlotId,
    Guid DoctorId,
    Guid PatientId,
    string Notes,
    string? Prescription
);
