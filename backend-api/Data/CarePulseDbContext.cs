using CarePulse.Api.Entities.Base;
using CarePulse.Api.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Data;

public class CarePulseDbContext : IdentityDbContext
{
    public CarePulseDbContext(DbContextOptions<CarePulseDbContext> options) : base(options) { }

    // Placeholders for DbSets (Students 1-4 will attach entities here)
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<ClinicRoster> ClinicRosters => Set<ClinicRoster>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<ConsultationRecord> ConsultationRecords => Set<ConsultationRecord>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("uuid-ossp");

        
        builder.Entity<AppointmentSlot>()
            .HasIndex(s => new { s.DoctorId, s.SlotStart })
            .IsUnique();

        builder.Entity<AppointmentSlot>()
            .HasOne(s => s.Doctor)
            .WithMany(d => d.AppointmentSlots)
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ClinicRoster>()
            .HasOne(r => r.Doctor)
            .WithMany(d => d.ClinicRosters)
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ConsultationRecord>()
            .HasOne(c => c.Slot)
            .WithOne()
            .HasForeignKey<ConsultationRecord>(c => c.SlotId)
            .OnDelete(DeleteBehavior.Restrict);
        // Global query filter for soft deletes
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(CarePulseDbContext)
                    .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);
                method?.Invoke(null, new object[] { builder });
            }
        }
    }

    private static void SetGlobalQueryFilter<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}