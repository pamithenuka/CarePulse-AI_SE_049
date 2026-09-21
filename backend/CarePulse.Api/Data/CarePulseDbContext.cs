using CarePulse.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Data;

// NOTE for the team: this DbContext currently only contains Student 3's
// tables (doctor rostering & scheduling). When merging with the other
// students' branches, each student's DbSet<> properties and OnModelCreating
// configuration should be added into this SAME class, so the whole app
// shares one DbContext / one migration history.
public class CarePulseDbContext : DbContext
{
    public CarePulseDbContext(DbContextOptions<CarePulseDbContext> options)
        : base(options)
    {
    }

    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<ClinicRoster> ClinicRosters => Set<ClinicRoster>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<ConsultationRecord> ConsultationRecords => Set<ConsultationRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Prevent creating two identical slots for the same doctor at the
        // same start time (separate from the booking-concurrency check,
        // which is handled by AppointmentSlot.RowVersion).
        modelBuilder.Entity<AppointmentSlot>()
            .HasIndex(s => new { s.DoctorId, s.SlotStart })
            .IsUnique();

        modelBuilder.Entity<AppointmentSlot>()
            .HasOne(s => s.Doctor)
            .WithMany(d => d.AppointmentSlots)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ClinicRoster>()
            .HasOne(r => r.Doctor)
            .WithMany(d => d.ClinicRosters)
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ConsultationRecord>()
            .HasOne(c => c.Slot)
            .WithOne()
            .HasForeignKey<ConsultationRecord>(c => c.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
