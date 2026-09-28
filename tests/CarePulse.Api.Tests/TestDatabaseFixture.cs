using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Tests;

// Shared across every test class. Points at a SEPARATE database
// ("carepulse_test_db") so tests never touch the real dev data.
public class TestDatabaseFixture : IDisposable
{
    // Same server/credentials as backend-api's appsettings.json - just a
    // different database name. Update the password here to match yours.
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=carepulse_test_db;Username=postgres;Password=Ht123";

    public TestDatabaseFixture()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    public CarePulseDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new CarePulseDbContext(options);
    }

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
}
