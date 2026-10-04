using CarePulse.Api.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CarePulse.Api.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CarePulseDbContext>
{
    public CarePulseDbContext CreateDbContext(string[] args)
    {
        // Design-time commands do not start or seed the API and never read a teammate's local file.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=carepulse_dev;Username=postgres;Password=local-dev-only";
        return new CarePulseDbContext(new DbContextOptionsBuilder<CarePulseDbContext>().UseNpgsql(connection).Options, new DesignUser());
    }
    private sealed class DesignUser : ICurrentUserService { public string? UserId => "migration"; }
}
