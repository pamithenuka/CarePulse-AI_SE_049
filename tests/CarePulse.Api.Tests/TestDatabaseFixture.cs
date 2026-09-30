using CarePulse.Api.Data;
using CarePulse.Api.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarePulse.Api.Tests;

public class TestDatabaseFixture : IDisposable
{
    public string ConnectionString { get; }
    public TestDatabaseFixture()
    {
        var configured = Environment.GetEnvironmentVariable("CAREPULSE_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set CAREPULSE_TEST_CONNECTION to an isolated local PostgreSQL server. See README.md.");
        var builder = new NpgsqlConnectionStringBuilder(configured) { Database = "carepulse_test_" + Guid.NewGuid().ToString("N") };
        ConnectionString = builder.ConnectionString;
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }
    public CarePulseDbContext CreateContext() => new(new DbContextOptionsBuilder<CarePulseDbContext>()
        .UseNpgsql(ConnectionString).Options, new FakeCurrentUserService());
    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }
}
[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture> { }
