using CarePulse.Api.Data;
using CarePulse.Api.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace CarePulse.Api.Tests;

public class MigrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EntireMigrationHistory_ReplaysOnEmptyPostgres_AndMatchesModel(bool existingAuditColumns)
    {
        var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("CAREPULSE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set CAREPULSE_TEST_CONNECTION.")) { Database = "carepulse_migration_" + Guid.NewGuid().ToString("N") };
        var log = new System.Text.StringBuilder();
        using var db = new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>().UseNpgsql(builder.ConnectionString).LogTo(message => log.AppendLine(message)).Options, new FakeCurrentUserService());
        try
        {
            if (existingAuditColumns)
            {
                await db.GetService<IMigrator>().MigrateAsync("20260927104724_AddStudent2TriageIntegration");
                foreach (var table in new[] { "AppointmentSlots", "ClinicRosters", "ConsultationRecords" })
                    await db.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"DeletedAt\" timestamp with time zone; ALTER TABLE \"{table}\" ADD COLUMN \"DeletedBy\" text;");
            }
            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.False(db.Database.HasPendingModelChanges());
            Assert.Empty(await db.AppointmentSlots.ToListAsync());
            Assert.Empty(await db.DispatchTickets.ToListAsync());
        }
        catch (Exception ex) { throw new Exception(log.ToString(), ex); }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
