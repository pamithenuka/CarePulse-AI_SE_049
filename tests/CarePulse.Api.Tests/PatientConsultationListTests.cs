using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarePulse.Api.Tests;

/// <summary>
/// GET /api/v1/consultations/patient/{patientId}: a patient's consultation history.
/// Runs through the real HTTP pipeline (JWT, roles, ownership) with an isolated in-memory database.
/// </summary>
public class PatientConsultationListTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private readonly ApplicationUser _patientAUser = NewUser("Patient A");
    private readonly ApplicationUser _patientBUser = NewUser("Patient B");
    private readonly ApplicationUser _doctorUser = NewUser("Dr Active");
    private readonly ApplicationUser _formerDoctorUser = NewUser("Dr Former");

    private readonly Guid _patientA = Guid.NewGuid();
    private readonly Guid _patientB = Guid.NewGuid();
    private readonly Guid _doctor = Guid.NewGuid();
    private readonly Guid _formerDoctor = Guid.NewGuid();
    private readonly Guid _patientWithNone = Guid.NewGuid();
    private readonly ApplicationUser _patientNoneUser = NewUser("Patient None");

    public PatientConsultationListTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "consultation-list-" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "consultation-list-tests-signing-secret-1234567890",
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
        db.Users.AddRange(_patientAUser, _patientBUser, _patientNoneUser, _doctorUser, _formerDoctorUser);
        db.PatientProfiles.AddRange(
            new PatientProfile { Id = _patientA, UserId = _patientAUser.Id, FullName = "Patient A", NationalId = "199011111111" },
            new PatientProfile { Id = _patientB, UserId = _patientBUser.Id, FullName = "Patient B", NationalId = "199022222222" },
            new PatientProfile { Id = _patientWithNone, UserId = _patientNoneUser.Id, FullName = "Patient None", NationalId = "199033333333" });
        db.DoctorProfiles.AddRange(
            new DoctorProfile { Id = _doctor, UserId = _doctorUser.Id, FullName = "Dr. Silva", Specialty = "CARDIOLOGY" },
            new DoctorProfile { Id = _formerDoctor, UserId = _formerDoctorUser.Id, FullName = "Dr. Fernando", Specialty = "NEUROLOGY" });
        db.SaveChanges();

        // Patient A: an older and a newer consultation with the active doctor, one with a doctor who was later removed.
        AddConsultation(db, _patientA, _doctor, DateTime.UtcNow.AddDays(-20), "Older visit: mild headache.", null);
        AddConsultation(db, _patientA, _doctor, DateTime.UtcNow.AddDays(-2), "Newer visit: blood pressure stable.", "Amlodipine 5 mg");
        AddConsultation(db, _patientA, _formerDoctor, DateTime.UtcNow.AddDays(-40), "Oldest visit with a former doctor.", null);
        // Patient B has their own record that must never appear in A's list.
        AddConsultation(db, _patientB, _doctor, DateTime.UtcNow.AddDays(-5), "Patient B private notes.", null);
        db.SaveChanges();

        // The former doctor is removed (soft-deleted) after seeing patient A.
        var former = db.DoctorProfiles.IgnoreQueryFilters().Single(d => d.Id == _formerDoctor);
        former.IsDeleted = true;
        db.SaveChanges();
    }

    private static ApplicationUser NewUser(string name) =>
        new() { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = name };

    private static void AddConsultation(CarePulseDbContext db, Guid patient, Guid doctor, DateTime start, string notes, string? prescription)
    {
        var slot = new AppointmentSlot
        {
            DoctorId = doctor, PatientId = patient, SlotStart = start, SlotEnd = start.AddMinutes(30), Status = SlotStatus.Booked,
        };
        db.AppointmentSlots.Add(slot);
        db.ConsultationRecords.Add(new ConsultationRecord
        {
            SlotId = slot.Id, DoctorId = doctor, PatientId = patient, Notes = notes, Prescription = prescription,
        });
    }

    private HttpClient ClientFor(ApplicationUser? user, string? role)
    {
        var client = _factory.CreateClient();
        if (user is null) return client;
        using var scope = _factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user, new[] { role! }).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    [Fact]
    public async Task WithoutAToken_Returns401()
    {
        using var client = ClientFor(null, null);

        var response = await client.GetAsync($"/api/v1/consultations/patient/{_patientA}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownPatient_Returns404()
    {
        using var client = ClientFor(_doctorUser, "Doctor");

        var response = await client.GetAsync($"/api/v1/consultations/patient/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Patient_SeesOwnConsultations_NewestFirst_WithDoctorDetails()
    {
        using var client = ClientFor(_patientAUser, "Patient");

        var response = await client.GetAsync($"/api/v1/consultations/patient/{_patientA}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var list = await ReadAsync(response);
        Assert.Equal(3, list.GetArrayLength());
        Assert.Equal("Newer visit: blood pressure stable.", list[0].GetProperty("notes").GetString());
        Assert.Equal("Dr. Silva", list[0].GetProperty("doctorName").GetString());
        Assert.Equal("CARDIOLOGY", list[0].GetProperty("specialty").GetString());
        Assert.Equal("Amlodipine 5 mg", list[0].GetProperty("prescription").GetString());
        Assert.Equal("Older visit: mild headache.", list[1].GetProperty("notes").GetString());
        Assert.Equal(JsonValueKind.Null, list[1].GetProperty("prescription").ValueKind);
        var times = list.EnumerateArray().Select(r => r.GetProperty("appointmentStart").GetDateTime()).ToList();
        Assert.Equal(times.OrderByDescending(t => t), times);
    }

    [Fact]
    public async Task List_DoesNotIncludeAnotherPatientsRecords()
    {
        using var client = ClientFor(_patientAUser, "Patient");

        var list = await ReadAsync(await client.GetAsync($"/api/v1/consultations/patient/{_patientA}"));

        Assert.DoesNotContain(list.EnumerateArray(), r => r.GetProperty("notes").GetString()!.Contains("Patient B"));
    }

    [Fact]
    public async Task Patient_CannotReadAnotherPatientsConsultations()
    {
        using var client = ClientFor(_patientBUser, "Patient");

        var response = await client.GetAsync($"/api/v1/consultations/patient/{_patientA}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("Doctor")]
    [InlineData("Admin")]
    public async Task Staff_CanReadAnyPatientsConsultations(string role)
    {
        using var client = ClientFor(_doctorUser, role);

        var response = await client.GetAsync($"/api/v1/consultations/patient/{_patientA}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, (await ReadAsync(response)).GetArrayLength());
    }

    [Fact]
    public async Task PatientWithNoConsultations_GetsAnEmptyList()
    {
        using var client = ClientFor(_patientNoneUser, "Patient");

        var response = await client.GetAsync($"/api/v1/consultations/patient/{_patientWithNone}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await ReadAsync(response)).GetArrayLength());
    }

    [Fact]
    public async Task RemovedDoctor_IsStillNamedOnOldConsultations()
    {
        using var client = ClientFor(_patientAUser, "Patient");

        var list = await ReadAsync(await client.GetAsync($"/api/v1/consultations/patient/{_patientA}"));

        var oldest = list.EnumerateArray().Single(r => r.GetProperty("notes").GetString()!.StartsWith("Oldest visit"));
        Assert.Equal("Dr. Fernando", oldest.GetProperty("doctorName").GetString());
    }

    [Fact]
    public async Task NonGuidPatientId_Returns404_NotAServerError()
    {
        using var client = ClientFor(_doctorUser, "Doctor");

        var response = await client.GetAsync("/api/v1/consultations/patient/not-a-guid");

        Assert.True((int)response.StatusCode < 500, $"Got {(int)response.StatusCode}");
    }
}
