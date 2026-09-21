using CarePulse.Api.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarePulse.Api.Tests;

// Shared across every test class. Points at a SEPARATE database
// ("carepulse_test") so tests never touch the real data you've been
// demoing with. Each test class gets a fresh, empty schema to work with.
public class TestDatabaseFixture : IDisposable
{
    // Same server/credentials as your real app - just a different database name.
    // If your real password isn't "postgres", update it here too.
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=carepulse_test;Username=postgres;Password=password";

    public TestDatabaseFixture()
    {
        using var context = CreateContext();
        // Wipes and rebuilds the test database schema fresh every test run,
        // from your current models - so tests always start from a known,
        // clean state.
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
    // This class has no code. It only exists so xUnit knows all test
    // classes tagged [Collection("Database collection")] should share
    // ONE TestDatabaseFixture instance instead of creating a new database
    // for every single test class.
}
