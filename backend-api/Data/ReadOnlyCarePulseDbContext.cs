using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Data;

// Per PROJECT_SPECIFICATION.md: "AI agents access data exclusively through
// read-only C# interfaces. Agents are prohibited from performing database
// writes or directly executing state-changing logic."
//
// This is that read-only interface. It's the SAME database as
// CarePulseDbContext (same connection string, same tables) - the only
// difference is that SaveChanges is blocked here. Any AI agent (yours or
// another student's) can safely query through this context, but if any
// code path ever tries to persist a change through it, it fails loudly
// and immediately instead of silently succeeding.
public class ReadOnlyCarePulseDbContext : CarePulseDbContext
{
    public ReadOnlyCarePulseDbContext(DbContextOptions<CarePulseDbContext> options)
        : base(options)
    {
    }

    public override int SaveChanges()
    {
        throw new InvalidOperationException(
            "ReadOnlyCarePulseDbContext is read-only. AI agents must never write to the database directly.");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        throw new InvalidOperationException(
            "ReadOnlyCarePulseDbContext is read-only. AI agents must never write to the database directly.");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "ReadOnlyCarePulseDbContext is read-only. AI agents must never write to the database directly.");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "ReadOnlyCarePulseDbContext is read-only. AI agents must never write to the database directly.");
    }
}
