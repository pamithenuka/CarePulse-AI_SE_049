using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePulse.Api.Models;

public enum SlotStatus
{
    Open,
    Booked,
    Cancelled
}

// A single bookable time slot for a doctor. This is the table where
// double-booking must be prevented.
public class AppointmentSlot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DoctorId { get; set; }

    [ForeignKey(nameof(DoctorId))]
    public DoctorProfile? Doctor { get; set; }

    [Required]
    public DateTime SlotStart { get; set; }

    [Required]
    public DateTime SlotEnd { get; set; }

    [Required]
    public SlotStatus Status { get; set; } = SlotStatus.Open;

    // Set when a patient books the slot. Comes from Student 1's PatientProfiles
    // table (referenced by Id only - no hard FK across student slices).
    public Guid? PatientId { get; set; }

    // EF Core optimistic concurrency token. This is the actual mechanism that
    // stops two people booking the same slot at the same instant: if two
    // requests read the same row and both try to update it, the second write
    // will fail with a DbUpdateConcurrencyException because the RowVersion
    // it read is now stale. Postgres maps this to an "xmin" style check.
    [Timestamp]
    public uint RowVersion { get; set; }
}
