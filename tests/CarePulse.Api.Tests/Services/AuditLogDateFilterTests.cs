using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Auth;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarePulse.Api.Tests.Services;

/// <summary>
/// Regression tests for the audit log date filter. The web page sends plain dates; the backend used to pass them
/// to PostgreSQL with no time zone, which the database driver rejects ("Cannot write DateTime with Kind=Unspecified").
/// </summary>
public class AuditLogDateFilterTests
{
    // Clinic days are Sri Lanka time (UTC+5:30), so 14 Oct runs from 13 Oct 18:30 UTC to 14 Oct 18:30 UTC.
    private static DateTime Date(int day) => new(2026, 10, day, 0, 0, 0, DateTimeKind.Unspecified);

    // ---------- the boundary helper (this is what PostgreSQL cares about: every value must be UTC) ----------

    [Fact]
    public void DateOnlyFilters_BecomeUtcValues_ThatPostgresAccepts()
    {
        var (from, toExclusive) = AuditDateRange.Normalize(Date(14), Date(16));

        Assert.Equal(DateTimeKind.Utc, from!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, toExclusive!.Value.Kind);
    }

    [Fact]
    public void FromStartsAtTheBeginningOfTheClinicDay_AndToRunsToTheEndOfThatDay()
    {
        var (from, toExclusive) = AuditDateRange.Normalize(Date(14), Date(14));

        Assert.Equal(new DateTime(2026, 10, 13, 18, 30, 0, DateTimeKind.Utc), from);
        Assert.Equal(new DateTime(2026, 10, 14, 18, 30, 0, DateTimeKind.Utc), toExclusive);
    }

    [Fact]
    public void NoDates_MeansNoFilter()
    {
        var (from, toExclusive) = AuditDateRange.Normalize(null, null);

        Assert.Null(from);
        Assert.Null(toExclusive);
    }

    [Fact]
    public void OnlyOneDateGiven_FiltersOnlyThatSide()
    {
        var onlyFrom = AuditDateRange.Normalize(Date(14), null);
        var onlyTo = AuditDateRange.Normalize(null, Date(14));

        Assert.NotNull(onlyFrom.FromUtc);
        Assert.Null(onlyFrom.ToExclusiveUtc);
        Assert.Null(onlyTo.FromUtc);
        Assert.NotNull(onlyTo.ToExclusiveUtc);
    }

    [Fact]
    public void ExactTimestamps_AreKeptAsGiven_ConvertedToUtc()
    {
        var utcMoment = new DateTime(2026, 10, 14, 9, 15, 0, DateTimeKind.Utc);
        var localMoment = utcMoment.ToLocalTime();

        var (fromUtc, _) = AuditDateRange.Normalize(utcMoment, null);
        var (fromLocal, _) = AuditDateRange.Normalize(localMoment, null);
        var (_, toExclusive) = AuditDateRange.Normalize(null, utcMoment);

        Assert.Equal(utcMoment, fromUtc);
        Assert.Equal(utcMoment, fromLocal);
        Assert.True(toExclusive > utcMoment, "the instant itself stays inside the range");
        Assert.True(toExclusive <= utcMoment.AddMilliseconds(1));
    }

    [Fact]
    public void ATimeWithNoZone_IsTakenAsUtc_NotLeftUnspecified()
    {
        var noZone = new DateTime(2026, 10, 14, 9, 15, 0, DateTimeKind.Unspecified);

        var (from, _) = AuditDateRange.Normalize(noZone, null);

        Assert.Equal(DateTimeKind.Utc, from!.Value.Kind);
        Assert.Equal(new DateTime(2026, 10, 14, 9, 15, 0, DateTimeKind.Utc), from);
    }

    // ---------- the query itself ----------

    private static async Task<(CarePulseDbContext Db, PatientService Service, Guid PatientId)> SeedAsync()
    {
        var db = new CarePulseDbContext(
            new DbContextOptionsBuilder<CarePulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new FakeCurrentUserService("test-actor"));
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<CarePulseDbContext>();
        var users = services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
        var service = new PatientService(db, new FakeWebHostEnvironment(), new FakeNotificationService(), users);

        var patientId = Guid.NewGuid();
        db.PatientAuditLogs.RemoveRange(db.PatientAuditLogs);
        await db.SaveChangesAsync();
        void Add(string label, DateTime utc) => db.PatientAuditLogs.Add(new PatientAuditLog
        {
            PatientProfileId = patientId, EntityName = label, EntityId = Guid.NewGuid(), ChangedByUserId = "u1", ChangedAt = utc,
        });
        Add("just-before-14th", new DateTime(2026, 10, 13, 18, 29, 0, DateTimeKind.Utc));  // 13 Oct 23:59 in Sri Lanka
        Add("start-of-14th",    new DateTime(2026, 10, 13, 18, 31, 0, DateTimeKind.Utc));  // 14 Oct 00:01 in Sri Lanka
        Add("end-of-14th",      new DateTime(2026, 10, 14, 18, 29, 0, DateTimeKind.Utc));  // 14 Oct 23:59 in Sri Lanka
        Add("start-of-15th",    new DateTime(2026, 10, 14, 18, 31, 0, DateTimeKind.Utc));  // 15 Oct 00:01 in Sri Lanka
        Add("on-the-16th",      new DateTime(2026, 10, 16, 6, 0, 0, DateTimeKind.Utc));
        await db.SaveChangesAsync();
        return (db, service, patientId);
    }

    private static async Task<List<string>> NamesAsync(PatientService s, DateTime? from, DateTime? to) =>
        (await s.GetAuditLogAsync(null, null, from, to, 1, 50)).Items.Select(i => i.EntityName).OrderBy(n => n).ToList();

    [Fact]
    public async Task AWholeDay_ReturnsOnlyEntriesFromThatClinicDay()
    {
        var (db, service, _) = await SeedAsync();
        await using var _db = db;

        var names = await NamesAsync(service, Date(14), Date(14));

        Assert.Equal(new[] { "end-of-14th", "start-of-14th" }, names);
    }

    [Fact]
    public async Task ARange_IncludesBothEndDays()
    {
        var (db, service, _) = await SeedAsync();
        await using var _db = db;

        var names = await NamesAsync(service, Date(14), Date(15));

        Assert.Equal(new[] { "end-of-14th", "start-of-14th", "start-of-15th" }, names);
    }

    [Fact]
    public async Task OnlyAFromDate_ReturnsThatDayAndEverythingAfter()
    {
        var (db, service, _) = await SeedAsync();
        await using var _db = db;

        var names = await NamesAsync(service, Date(15), null);

        Assert.Equal(new[] { "on-the-16th", "start-of-15th" }, names);
    }

    [Fact]
    public async Task NoDates_ReturnsEverything()
    {
        var (db, service, _) = await SeedAsync();
        await using var _db = db;

        Assert.Equal(5, (await NamesAsync(service, null, null)).Count);
    }

    [Fact]
    public async Task ARangeWithNothingInIt_ReturnsAnEmptyListNotAnError()
    {
        var (db, service, _) = await SeedAsync();
        await using var _db = db;

        Assert.Empty(await NamesAsync(service, Date(20), Date(21)));
    }
}

/// <summary>The same filter through the real HTTP endpoint the web page calls.</summary>
public class AuditLogDateFilterApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ApplicationUser _admin = new() { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = "Admin" };
    private readonly ApplicationUser _patientUser = new() { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = "Patient" };

    public AuditLogDateFilterApiTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "audit-dates-" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "audit-date-tests-signing-secret-1234567890-abcdef",
                ["Database:SeedOnStartup"] = "false"
            }));
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CarePulseDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<CarePulseDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
        db.Users.AddRange(_admin, _patientUser);
        db.SaveChanges();
        db.PatientAuditLogs.Add(new PatientAuditLog
        {
            PatientProfileId = Guid.NewGuid(), EntityName = "MedicalHistory", EntityId = Guid.NewGuid(), ChangedByUserId = _admin.Id,
            ChangedAt = new DateTime(2026, 10, 15, 6, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();
    }

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        using var scope = _factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(_admin, new[] { "Admin" }).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Theory]
    [InlineData("from=2026-10-14&to=2026-10-16", 1)]
    [InlineData("from=2026-10-15&to=2026-10-15", 1)]
    [InlineData("from=2026-10-16&to=2026-10-17", 0)]
    [InlineData("from=2026-10-14", 1)]
    [InlineData("to=2026-10-14", 0)]
    [InlineData("", 1)]
    public async Task DateOnlyQueryStrings_FromTheWebPage_ReturnTheRightCount_NotAServerError(string query, int expected)
    {
        using var client = AdminClient();

        var response = await client.GetAsync($"/api/v1/patients/audit-log?{query}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(expected, json.GetProperty("totalCount").GetInt32());
    }
}
