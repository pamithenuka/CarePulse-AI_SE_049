using System.Net.Http.Headers;
using System.Net.Http.Json;
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

namespace CarePulse.Api.Tests.Group;

/// <summary>
/// GROUP test helper (not an individual student's contribution): hosts the real API with an isolated
/// in-memory database and seeds two patients, two doctors and an admin for scheduling tests.
/// </summary>
public abstract class SchedulingApiTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly WebApplicationFactory<Program> Factory;

    protected readonly ApplicationUser PatientAUser = NewUser("Patient A");
    protected readonly ApplicationUser PatientBUser = NewUser("Patient B");
    protected readonly ApplicationUser DoctorAUser = NewUser("Doctor A");
    protected readonly ApplicationUser DoctorBUser = NewUser("Doctor B");
    protected readonly ApplicationUser AdminUser = NewUser("Admin");

    protected readonly Guid PatientAId = Guid.NewGuid();
    protected readonly Guid PatientBId = Guid.NewGuid();
    protected readonly Guid DoctorAId = Guid.NewGuid();
    protected readonly Guid DoctorBId = Guid.NewGuid();

    protected SchedulingApiTestBase(WebApplicationFactory<Program> factory)
    {
        var dbName = "group-scheduling-" + Guid.NewGuid();
        Factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "group-scheduling-tests-signing-secret-1234567890",
                ["Database:SeedOnStartup"] = "false",
                ["AI:GeminiApiKey"] = ""
            }));
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CarePulseDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<CarePulseDbContext>(o => o.UseInMemoryDatabase(dbName));
            });
        });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
        db.Users.AddRange(PatientAUser, PatientBUser, DoctorAUser, DoctorBUser, AdminUser);
        db.PatientProfiles.Add(new PatientProfile { Id = PatientAId, UserId = PatientAUser.Id, FullName = "Patient A", NationalId = "199011111111" });
        db.PatientProfiles.Add(new PatientProfile { Id = PatientBId, UserId = PatientBUser.Id, FullName = "Patient B", NationalId = "199022222222" });
        db.DoctorProfiles.Add(new DoctorProfile { Id = DoctorAId, UserId = DoctorAUser.Id, FullName = "Dr A", Specialty = "CARDIOLOGY" });
        db.DoctorProfiles.Add(new DoctorProfile { Id = DoctorBId, UserId = DoctorBUser.Id, FullName = "Dr B", Specialty = "NEUROLOGY" });
        db.SaveChanges();
    }

    private static ApplicationUser NewUser(string name) =>
        new() { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = name };

    /// <summary>Client carrying a real JWT for the user, or no token when <paramref name="user"/> is null.</summary>
    protected HttpClient ClientFor(ApplicationUser? user, string? role)
    {
        var client = Factory.CreateClient();
        if (user is null) return client;
        using var scope = Factory.Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().GenerateToken(user, new[] { role! }).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected Guid SeedSlot(Guid doctorId, TimeSpan fromNow, SlotStatus status = SlotStatus.Open, Guid? patientId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
        var start = DateTime.UtcNow.Add(fromNow);
        var slot = new AppointmentSlot { DoctorId = doctorId, SlotStart = start, SlotEnd = start.AddMinutes(30), Status = status, PatientId = patientId };
        db.AppointmentSlots.Add(slot);
        db.SaveChanges();
        return slot.Id;
    }

    protected void MoveSlotStart(Guid slotId, TimeSpan fromNow)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();
        var slot = db.AppointmentSlots.Single(s => s.Id == slotId);
        slot.SlotStart = DateTime.UtcNow.Add(fromNow);
        slot.SlotEnd = slot.SlotStart.AddMinutes(30);
        db.SaveChanges();
    }

    protected static object Roster(Guid doctorId, int day, string start, string end, int minutes) =>
        new { doctorId, dayOfWeek = day, startTime = start, endTime = end, slotDurationMinutes = minutes };

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();

    protected static HttpRequestMessage Request(string method, string url, object? body = null) =>
        new(new HttpMethod(method), url) { Content = body is null ? (method is "POST" or "PUT" ? JsonContent.Create(new { }) : null) : JsonContent.Create(body) };
}
