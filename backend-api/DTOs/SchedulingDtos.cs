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

public record CreateDoctorRequestDto(
    string FullName,
    string Specialty,
    string PhoneNumber,
    string? Email
);

public record UpdateDoctorRequestDto(
    string FullName,
    string Specialty,
    string PhoneNumber,
    string? Email,
    bool IsActive
);