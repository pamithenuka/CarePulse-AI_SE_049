using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.DTOs;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Dispatch;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Auth;
using CarePulse.Api.Tests;
using CarePulse.Api.Tests.Fakes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace CarePulse.ClientHarness;

public class EntryPoint
{
    public static async Task Main()
    {
        var connection = Environment.GetEnvironmentVariable("CAREPULSE_TEST_CONNECTION") ?? throw new Exception("Set isolated local CAREPULSE_TEST_CONNECTION.");
        if (new NpgsqlConnectionStringBuilder(connection).Host is not ("127.0.0.1" or "localhost"))
            throw new Exception("Harness permits loopback PostgreSQL only.");
        var sessionFile = Environment.GetEnvironmentVariable("CAREPULSE_CLIENT_SESSION") ?? throw new Exception("Set a private temporary session file path.");
        using var database = new TestDatabaseFixture();
        using var factory = new WebApplicationFactory<global::Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string,string?> {
                ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
                ["Jwt:Secret"] = "synthetic-client-harness-signing-key-only-123456789",
                ["AI:GeminiApiKey"] = "", ["Database:SeedOnStartup"] = "false"
            }));
            builder.ConfigureServices(services => {
                services.RemoveAll<IAiPlannerClient>();
                services.AddScoped<IAiPlannerClient>(_ => new FakeAiPlannerClient { NextRawJson = """
                {"steps":[{"agent":"DomainAnalysis","task":"Assess synthetic symptoms"},{"agent":"ActionTool","task":"Find specialist"},{"agent":"Validation","task":"Require doctor approval"}]}
                """ });
                services.RemoveAll<ITriageAiAgent>(); services.AddScoped<ITriageAiAgent, SyntheticDomainAgent>();
            });
        });
        using var api = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        using var db = database.CreateContext();
        ApplicationUser User(string role) { var u = new ApplicationUser { Id = Guid.NewGuid().ToString(), UserName = Guid.NewGuid().ToString(), FullName = "QM synthetic " + role }; db.Users.Add(u); return u; }
        var patientUser = User("Patient"); var doctorUser = User("Doctor"); var nurseUser = User("Nurse");
        var patient = new PatientProfile { UserId = patientUser.Id, FullName = patientUser.FullName, NationalId = Guid.NewGuid().ToString() };
        var doctor = new DoctorProfile { UserId = doctorUser.Id, FullName = doctorUser.FullName, Specialty = "CARDIOLOGY" };
        var nurse = new NurseProfiles { UserId = nurseUser.Id, FullName = nurseUser.FullName, IsAvailable = true };
        db.PatientProfiles.Add(patient); db.DoctorProfiles.Add(doctor); db.NurseProfiles.Add(nurse);
        db.AppointmentSlots.Add(new AppointmentSlot { DoctorId = doctor.Id, SlotStart = DateTime.UtcNow.AddDays(1), SlotEnd = DateTime.UtcNow.AddDays(1).AddMinutes(30) });
        await db.SaveChangesAsync();
        var performanceData = Environment.GetEnvironmentVariable("CAREPULSE_PERF_DATA") == "1";
        if (performanceData)
        {
            for (var i = 0; i < 50; i++)
            {
                var d = new DoctorProfile { UserId = User("LoadDoctor").Id, FullName = $"QM Load Doctor {i:D3}", Specialty = "CARDIOLOGY" };
                db.DoctorProfiles.Add(d);
                for (var slot = 0; slot < 20; slot++)
                    db.AppointmentSlots.Add(new AppointmentSlot { DoctorId = d.Id,
                        SlotStart = DateTime.UtcNow.Date.AddDays(1).AddMinutes(slot * 30),
                        SlotEnd = DateTime.UtcNow.Date.AddDays(1).AddMinutes((slot + 1) * 30) });
            }
            for (var i = 0; i < 500; i++)
                db.PatientProfiles.Add(new PatientProfile { UserId = User("LoadPatient").Id,
                    FullName = $"QM Load Patient {i:D3}", NationalId = Guid.NewGuid().ToString() });
            await db.SaveChangesAsync();
        }
        await File.WriteAllTextAsync(sessionFile, JsonSerializer.Serialize(new {
            patientId = patient.Id, nurseId = nurse.Id, performanceData,
            patientToken = tokens.GenerateToken(patientUser, new[] { "Patient" }).Token,
            doctorToken = tokens.GenerateToken(doctorUser, new[] { "Doctor" }).Token,
            nurseToken = tokens.GenerateToken(nurseUser, new[] { "Nurse" }).Token
        }));
        if (!OperatingSystem.IsWindows()) File.SetUnixFileMode(sessionFile, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        var host = WebApplication.CreateBuilder();
        host.WebHost.UseUrls("http://127.0.0.1:5516");
        var app = host.Build();
        app.Run(async context => {
            using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
            if (context.Request.ContentLength > 0) {
                using var buffer = new MemoryStream(); await context.Request.Body.CopyToAsync(buffer);
                request.Content = new ByteArrayContent(buffer.ToArray());
                request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
            }
            if (context.Request.Headers.TryGetValue("Authorization", out var auth)) request.Headers.TryAddWithoutValidation("Authorization", auth.ToString());
            using var response = await api.SendAsync(request);
            context.Response.StatusCode = (int)response.StatusCode;
            context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
            await response.Content.CopyToAsync(context.Response.Body);
        });
        try { await app.RunAsync(); }
        finally { File.Delete(sessionFile); }
    }
}
public class SyntheticDomainAgent : ITriageAiAgent
{
    public Task<TriageAssessmentResult> AnalyzeSymptomsAsync(TriageSubmitRequestDto request) => Task.FromResult(new TriageAssessmentResult {
        RiskScore = 9, RiskLevel = "HIGH", RecommendedSpecialty = "CARDIOLOGY", Reason = "Deterministic QM client integration fixture", RecommendedAction = "DOCTOR_APPROVAL_REQUIRED"
    });
}
