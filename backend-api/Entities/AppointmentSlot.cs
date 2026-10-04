using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CarePulse.Api.Entities.Base;

namespace CarePulse.Api.Entities;

public enum SlotStatus
{
    Open,
    Booked,
    Cancelled
}

public class AppointmentSlot : BaseEntity
{
    public Guid DoctorId { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public DoctorProfile? Doctor { get; set; }

    public DateTime SlotStart { get; set; }
    public DateTime SlotEnd { get; set; }

    public SlotStatus Status { get; set; } = SlotStatus.Open;

    // Comes from Student 1's PatientProfiles table (referenced by Id only).
    public Guid? PatientId { get; set; }

    // The actual mechanism that stops double-booking: if two requests try
    // to book this same slot, the second write fails because the
    // RowVersion it read is now stale.
    [Timestamp]
    public uint RowVersion { get; set; }
}