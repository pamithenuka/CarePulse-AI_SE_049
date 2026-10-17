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

public record RosterUpdateRequestDto(
    Guid DoctorId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int SlotDurationMinutes
);

public record GenerateSlotsRequestDto(
    DateOnly StartDate,
    DateOnly EndDate
);

public record ConsultationCompleteRequestDto(
    Guid SlotId,
    Guid DoctorId,
    Guid PatientId,
    string Notes,
    string? Prescription
);

/// <summary>One past consultation, as shown in a patient's Consultation History.</summary>
public record ConsultationSummaryDto(
    Guid Id,
    Guid SlotId,
    Guid DoctorId,
    string DoctorName,
    string Specialty,
    DateTime AppointmentStart,
    DateTime AppointmentEnd,
    string Notes,
    string? Prescription
);
