using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Base;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Data;

public class CarePulseDbContext : IdentityDbContext
{
    public CarePulseDbContext(DbContextOptions<CarePulseDbContext> options) : base(options) { }

    // Student 2: Patient Triage
    public DbSet<TriageTicket> TriageTickets { get; set; } = null!;
    public DbSet<AiTriageLog> AiTriageLogs { get; set; } = null!;
    public DbSet<RiskAssessment> RiskAssessments { get; set; } = null!;
    public DbSet<ApprovalQueue> ApprovalQueues { get; set; } = null!;

    // Placeholders for DbSets (Students 1, 3, 4 will attach entities here)
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("uuid-ossp");

        // Student 2 Models Configuration
        builder.Entity<TriageTicket>(entity =>
        {
            entity.HasMany(t => t.AiTriageLogs)
                  .WithOne(l => l.TriageTicket)
                  .HasForeignKey(l => l.TriageTicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.RiskAssessment)
                  .WithOne(r => r.TriageTicket)
                  .HasForeignKey<RiskAssessment>(r => r.TriageTicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.ApprovalQueue)
                  .WithOne(a => a.TriageTicket)
                  .HasForeignKey<ApprovalQueue>(a => a.TriageTicketId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.Property(e => e.Symptoms).HasMaxLength(2000);
            entity.Property(e => e.Status).HasMaxLength(100);
            entity.Property(e => e.RiskLevel).HasMaxLength(50);
            entity.Property(e => e.RecommendedSpecialty).HasMaxLength(50);
        });

        builder.Entity<AiTriageLog>(entity =>
        {
            entity.Property(e => e.LogMessage).HasMaxLength(1000);
        });

        builder.Entity<RiskAssessment>(entity =>
        {
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.RecommendedAction).HasMaxLength(200);
        });

        builder.Entity<ApprovalQueue>(entity =>
        {
            entity.Property(e => e.ReviewStatus).HasMaxLength(100);
            entity.Property(e => e.ReviewedByDoctorId).HasMaxLength(256);
        });

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