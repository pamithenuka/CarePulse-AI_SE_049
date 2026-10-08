using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CarePulse.Api.Tests.Student1;

/// <summary>
/// Student 1 (QM): HTTP-level authentication and authorization tests for /api/v1/patients,
/// run through the real ASP.NET pipeline (JWT middleware, [Authorize(Roles)], routing)
/// with WebApplicationFactory and an isolated in-memory database.
/// </summary>
public class PatientsApiSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Secret = "student1-security-tests-signing-secret-1234567890";
    private static readonly Guid AnyId = Guid.NewGuid();

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ApplicationUser _patientA = NewUser("Patient A");
    private readonly ApplicationUser _patientB = NewUser("Patient B");
    private readonly ApplicationUser _doctor = NewUser("Doctor");
    private readonly ApplicationUser _nurse = NewUser("Nurse");
    private readonly Guid _profileA = Guid.NewGuid();

    public PatientsApiSecurityTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "student1-http-" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Secret,
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
        db.Users.AddRange(_patientA, _patientB, _doctor, _nurse);
        db.PatientProfiles.Add(new PatientProfile { Id = _profileA, UserId = _patientA.Id, FullName = "Patient A", NationalId = "199012345V" });
        db.DoctorProfiles.Add(new DoctorProfile { UserId = _doctor.Id, FullName = "Doctor", Specialty = "CARDIOLOGY" });
        db.NurseProfiles.Add(new NurseProfiles { UserId = _nurse.Id, FullName = "Nurse" });
        db.SaveChanges();
    }

    private static ApplicationUser NewUser(string name) =>
        new() { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = name };

    private HttpClient ClientFor(ApplicationUser? user, string? role)
    {
        var client = _factory.CreateClient();
        if (user is null) return client;
        using var scope = _factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user, new[] { role! }).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static HttpRequestMessage Request(string method, string url)
    {
        HttpContent? content = method is "POST" or "PUT" ? JsonContent.Create(new { }) : null;
        // The document upload endpoint only accepts multipart/form-data (otherwise routing answers 415 before authorization).
        if (url.EndsWith("/documents") && method == "POST")
            content = new MultipartFormDataContent { { new ByteArrayContent("%PDF-1.4"u8.ToArray()), "file", "a.pdf" } };
        return new HttpRequestMessage(new HttpMethod(method), url) { Content = content };
    }

    private static string Url(string template) => template.Replace("{id}", AnyId.ToString()).Replace("{sub}", AnyId.ToString());

    // ---------- Authentication (no / bad token -> 401) ----------

    public static IEnumerable<object[]> ProtectedEndpoints() => new[]
    {
        new object[] { "GET", "/api/v1/patients" },
        new object[] { "GET", "/api/v1/patients/me" },
        new object[] { "GET", "/api/v1/patients/{id}" },
        new object[] { "POST", "/api/v1/patients/profile" },
        new object[] { "POST", "/api/v1/patients/register" },
        new object[] { "DELETE", "/api/v1/patients/{id}" },
        new object[] { "GET", "/api/v1/patients/{id}/history" },
        new object[] { "GET", "/api/v1/patients/{id}/emergency-contacts" },
        new object[] { "GET", "/api/v1/patients/{id}/documents" },
        new object[] { "GET", "/api/v1/patients/{id}/documents/{sub}/download" },
        new object[] { "GET", "/api/v1/patients/{id}/audit-log" },
        new object[] { "GET", "/api/v1/patients/audit-log" },
        new object[] { "POST", "/api/v1/patients/{id}/emergency-alert" },
        new object[] { "GET", "/api/v1/patients/emergency-alerts" },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task NoToken_Returns401(string method, string template)
    {
        using var client = ClientFor(null, null);

        var response = await client.SendAsync(Request(method, Url(template)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenWithTamperedSignature_Returns401()
    {
        using var client = ClientFor(_doctor, "Doctor");
        var good = client.DefaultRequestHeaders.Authorization!.Parameter!;
        var tampered = good[..^4] + (good.EndsWith("AAAA") ? "BBBB" : "AAAA");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tampered);

        var response = await client.GetAsync("/api/v1/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithAnotherSecret_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            MakeToken("a-completely-different-signing-secret-0000000", DateTime.UtcNow.AddHours(1)));

        var response = await client.GetAsync("/api/v1/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            MakeToken(Secret, DateTime.UtcNow.AddDays(-1)));

        var response = await client.GetAsync("/api/v1/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GarbageBearerValue_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.jwt");

        var response = await client.GetAsync("/api/v1/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidToken_ForADoctorWithNoProfile_IsTreatedAsDeactivatedAndReturns401()
    {
        // Program.cs rejects tokens whose Doctor/Nurse profile row no longer exists ("account is no longer active").
        var orphan = NewUser("Removed Doctor");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
            db.Users.Add(orphan);
            await db.SaveChangesAsync();
        }
        using var client = ClientFor(orphan, "Doctor");

        var response = await client.GetAsync("/api/v1/patients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string MakeToken(string secret, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var token = new JwtSecurityToken(
            issuer: "CarePulseAPI", audience: "CarePulseClients",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, _doctor.Id), new Claim(ClaimTypes.Role, "Doctor") },
            notBefore: expires.AddHours(-2), expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    // ---------- Authorization (wrong role -> 403) ----------

    [Theory]
    [InlineData("Patient", "GET", "/api/v1/patients")]
    [InlineData("Nurse", "GET", "/api/v1/patients")]
    [InlineData("Patient", "GET", "/api/v1/patients/audit-log")]
    [InlineData("Patient", "GET", "/api/v1/patients/emergency-alerts")]
    [InlineData("Nurse", "GET", "/api/v1/patients/{id}")]
    [InlineData("Patient", "GET", "/api/v1/patients/{id}/audit-log")]
    [InlineData("Doctor", "POST", "/api/v1/patients/profile")]
    [InlineData("Doctor", "POST", "/api/v1/patients/register")]
    [InlineData("Patient", "POST", "/api/v1/patients/register")]
    [InlineData("Doctor", "DELETE", "/api/v1/patients/{id}")]
    [InlineData("Patient", "DELETE", "/api/v1/patients/{id}")]
    [InlineData("Nurse", "POST", "/api/v1/patients/{id}/emergency-alert")]
    [InlineData("Doctor", "POST", "/api/v1/patients/{id}/documents")]
    public async Task WrongRole_Returns403(string role, string method, string template)
    {
        var user = role switch { "Patient" => _patientB, "Doctor" => _doctor, _ => _nurse };
        using var client = ClientFor(user, role);

        var response = await client.SendAsync(Request(method, Url(template)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Ownership / IDOR ----------

    [Fact]
    public async Task Patient_CannotReadAnotherPatientsProfile()
    {
        using var client = ClientFor(_patientB, "Patient");

        var response = await client.GetAsync($"/api/v1/patients/{_profileA}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("history")]
    [InlineData("emergency-contacts")]
    [InlineData("documents")]
    public async Task Patient_CannotReadAnotherPatientsSubResources(string subResource)
    {
        using var client = ClientFor(_patientB, "Patient");

        var response = await client.GetAsync($"/api/v1/patients/{_profileA}/{subResource}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patient_CanReadOwnProfile_AndDoctorCanReadAnyProfile()
    {
        using var owner = ClientFor(_patientA, "Patient");
        using var doctor = ClientFor(_doctor, "Doctor");

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/api/v1/patients/{_profileA}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await doctor.GetAsync($"/api/v1/patients/{_profileA}")).StatusCode);
    }

    [Fact]
    public async Task Patient_CannotTriggerAnEmergencyAlertForAnotherPatient()
    {
        using var client = ClientFor(_patientB, "Patient");

        var response = await client.PostAsync($"/api/v1/patients/{_profileA}/emergency-alert", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---------- Robustness ----------

    [Fact]
    public async Task UnknownProfileId_Returns404()
    {
        using var client = ClientFor(_doctor, "Doctor");

        var response = await client.GetAsync($"/api/v1/patients/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NonGuidProfileId_Returns404_NotServerError()
    {
        using var client = ClientFor(_doctor, "Doctor");

        var response = await client.GetAsync("/api/v1/patients/not-a-guid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("search=' OR 1=1 --")]
    [InlineData("search=%27%3B%20DROP%20TABLE%20PatientProfiles%3B--")]
    [InlineData("search=<script>alert(1)</script>")]
    [InlineData("sortBy=;%20DROP%20TABLE")]
    [InlineData("page=-1&pageSize=-5")]
    [InlineData("page=0&pageSize=0")]
    [InlineData("pageSize=2147483647")]
    [InlineData("minAge=200&maxAge=-1")]
    [InlineData("status=bogus")]
    public async Task HostileOrOutOfRangeQueryParameters_NeverCauseAServerError(string query)
    {
        using var client = ClientFor(_doctor, "Doctor");

        var response = await client.GetAsync($"/api/v1/patients?{query}");

        Assert.True((int)response.StatusCode < 500, $"Server error {(int)response.StatusCode} for query '{query}'");
    }
}
