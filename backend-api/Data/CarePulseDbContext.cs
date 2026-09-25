using System.Text.Json;
using CarePulse.Api.Entities.Ai;
using CarePulse.Api.Entities.Base;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CarePulse.Api.Entities.Dispatch;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CarePulse.Api.Data;

public class CarePulseDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService _currentUserService;

    // Student 4: Dispatch
    public DbSet<NurseProfiles> NurseProfiles { get; set; }
    public DbSet<DispatchTickets> DispatchTickets { get; set; }
    public DbSet<RouteLogs> RouteLogs { get; set; }
    public DbSet<OnSiteVitalsRecords> OnSiteVitalsRecords { get; set; }

    private static readonly HashSet<Type> AuditedEntityTypes = new()
    {
        typeof(PatientProfile), typeof(MedicalHistory), typeof(EmergencyContact), typeof(MedicalDocument)
    };

    private static readonly HashSet<string> AuditIgnoredProperties = new() { nameof(BaseEntity.CreatedAt), nameof(BaseEntity.UpdatedAt) };

    public CarePulseDbContext(DbContextOptions<CarePulseDbContext> options, ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Student 1: Patient Identity, Medical Records & Vault
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
    public DbSet<MedicalDocument> MedicalDocuments => Set<MedicalDocument>();
    public DbSet<PatientAuditLog> PatientAuditLogs => Set<PatientAuditLog>();
    public DbSet<EmergencyAlertLog> EmergencyAlertLogs => Set<EmergencyAlertLog>();
    public DbSet<EmergencyAlertNotification> EmergencyAlertNotifications => Set<EmergencyAlertNotification>();

    // Agent 1 (Planner/Coordinator) workflow runs
    public DbSet<AiWorkflow> AiWorkflows => Set<AiWorkflow>();

    // Placeholders for DbSets (Students 2-3 will attach entities here)

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("uuid-ossp");

        builder.Entity<PatientProfile>(entity =>
        {
            entity.HasIndex(p => p.UserId).IsUnique();
            entity.HasIndex(p => p.NationalId).IsUnique();

            entity.HasMany(p => p.EmergencyContacts)
                .WithOne(c => c.PatientProfile)
                .HasForeignKey(c => c.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.MedicalHistories)
                .WithOne(h => h.PatientProfile)
                .HasForeignKey(h => h.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.MedicalDocuments)
                .WithOne(d => d.PatientProfile)
                .HasForeignKey(d => d.PatientProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PatientAuditLog>(entity =>
        {
            entity.HasIndex(a => a.PatientProfileId);
        });

        builder.Entity<AiWorkflow>(entity =>
        {
            entity.HasIndex(a => a.PatientProfileId);
        });

        builder.Entity<EmergencyAlertLog>(entity =>
        {
            entity.HasMany(a => a.Notifications)
                .WithOne(n => n.EmergencyAlertLog)
                .HasForeignKey(n => n.EmergencyAlertLogId)
                .OnDelete(DeleteBehavior.Cascade);
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
        var currentUserId = _currentUserService.UserId ?? "system";

        StampAuditableTimestamps(currentUserId);
        var auditLogs = BuildPatientAuditLogs(currentUserId);
        if (auditLogs.Count > 0)
        {
            PatientAuditLogs.AddRange(auditLogs);
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampAuditableTimestamps(string currentUserId)
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

                var isDeletedProperty = entry.Property(nameof(BaseEntity.IsDeleted));
                if (isDeletedProperty.IsModified && entry.Entity.IsDeleted && entry.Entity.DeletedAt is null)
                {
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    entry.Entity.DeletedBy = currentUserId;
                }
            }
        }
    }

    private List<PatientAuditLog> BuildPatientAuditLogs(string currentUserId)
    {
        var logs = new List<PatientAuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (!AuditedEntityTypes.Contains(entry.Entity.GetType()))
            {
                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            var patientProfileId = GetPatientProfileId(entry);
            if (patientProfileId is null)
            {
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                logs.Add(new PatientAuditLog
                {
                    PatientProfileId = patientProfileId.Value,
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = (Guid)entry.Property("Id").CurrentValue!,
                    Action = AuditAction.Created,
                    ChangesJson = "[]",
                    ChangedByUserId = currentUserId
                });
                continue;
            }

            var changedFields = entry.Properties
                .Where(p => p.IsModified && !AuditIgnoredProperties.Contains(p.Metadata.Name))
                .Select(p => new { field = p.Metadata.Name, oldValue = p.OriginalValue, newValue = p.CurrentValue })
                .Where(change => !Equals(change.oldValue, change.newValue))
                .ToList();

            if (changedFields.Count == 0)
            {
                continue;
            }

            var isSoftDelete = entry.Property(nameof(BaseEntity.IsDeleted)).IsModified &&
                                (bool)(entry.Property(nameof(BaseEntity.IsDeleted)).CurrentValue ?? false);

            logs.Add(new PatientAuditLog
            {
                PatientProfileId = patientProfileId.Value,
                EntityName = entry.Entity.GetType().Name,
                EntityId = (Guid)entry.Property("Id").CurrentValue!,
                Action = isSoftDelete ? AuditAction.Deleted : AuditAction.Updated,
                ChangesJson = JsonSerializer.Serialize(changedFields),
                ChangedByUserId = currentUserId
            });
        }

        return logs;
    }

    private static Guid? GetPatientProfileId(EntityEntry entry) => entry.Entity switch
    {
        PatientProfile p => p.Id,
        EmergencyContact c => c.PatientProfileId,
        MedicalHistory h => h.PatientProfileId,
        MedicalDocument d => d.PatientProfileId,
        _ => null
    };
}
