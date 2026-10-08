using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CarePulse.Api.Data;
using CarePulse.Api.Entities;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Notifications;
using CarePulse.Api.Tests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarePulse.Api.Tests.Student1;

/// <summary>
/// Student 1 (QM): integrated end-to-end workflow through the real ASP.NET pipeline
/// (routing, JWT login, role authorization, services, EF Core, Planner agent).
/// Only the LLM and the SMS gateway are replaced with fakes.
/// Admin registers a patient -> patient logs in -> adds contact and history -> creates an AI plan
/// -> doctor approves it -> patient triggers an emergency alert -> doctor reads the audit trail.
/// </summary>
public class PatientWorkflowE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Password = "Workflow@12345";
    private const string ValidPlan =
        "{\"summary\":\"x\",\"steps\":[" +
        "{\"agent\":\"DomainAnalysis\",\"task\":\"Assess symptoms\"}," +
        "{\"agent\":\"ActionTool\",\"task\":\"Find a clinician\"}," +
        "{\"agent\":\"Validation\",\"task\":\"Pause for doctor approval\"}]}";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly FakeNotificationService _sms = new();

    public PatientWorkflowE2ETests(WebApplicationFactory<Program> factory)
    {
        var dbName = "student1-e2e-" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "student1-e2e-signing-secret-1234567890-abcdef",
                ["Database:SeedOnStartup"] = "false",
                ["AI:GeminiApiKey"] = ""
            }));
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CarePulseDbContext>));
                if (descriptor != null) services.Remove(descriptor);
                services.AddDbContext<CarePulseDbContext>(o => o.UseInMemoryDatabase(dbName));

                services.RemoveAll<IAiPlannerClient>();
                services.AddScoped<IAiPlannerClient>(_ => new FakeAiPlannerClient { NextRawJson = ValidPlan });
                services.RemoveAll<INotificationService>();
                services.AddSingleton<INotificationService>(_sms);
            });
        });
    }

    private async Task SeedStaffAsync(string adminEmail, string doctorEmail)
    {
        using var scope = _factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Admin", "Doctor", "Patient" })
            Assert.True((await roles.CreateAsync(new IdentityRole(role))).Succeeded);

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<CarePulseDbContext>();

        var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "Workflow Admin" };
        Assert.True((await users.CreateAsync(admin, Password)).Succeeded);
        await users.AddToRoleAsync(admin, "Admin");

        var doctor = new ApplicationUser { UserName = doctorEmail, Email = doctorEmail, FullName = "Workflow Doctor" };
        Assert.True((await users.CreateAsync(doctor, Password)).Succeeded);
        await users.AddToRoleAsync(doctor, "Doctor");
        db.DoctorProfiles.Add(new DoctorProfile { UserId = doctor.Id, FullName = "Workflow Doctor", Specialty = "CARDIOLOGY" });
        await db.SaveChangesAsync();
    }

    private async Task<HttpClient> LoginAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Login failed for {email}: {(int)response.StatusCode} {body}");
        var token = JsonDocument.Parse(body).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<JsonElement> ExpectAsync(HttpResponseMessage response, HttpStatusCode expected, string step)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{step}: expected {(int)expected} but got {(int)response.StatusCode}. Body: {body}");
        return string.IsNullOrWhiteSpace(body) ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    [Fact]
    public async Task PatientWorkflow_RegisterLoginRecordsAiPlanApprovalAlertAndAudit_WorksEndToEnd()
    {
        const string adminEmail = "admin@workflow.test", doctorEmail = "doctor@workflow.test", patientEmail = "patient@workflow.test";
        await SeedStaffAsync(adminEmail, doctorEmail);

        // 1. Admin registers a patient (creates the login account and the profile together).
        using var admin = await LoginAsync(adminEmail);
        var registered = await ExpectAsync(await admin.PostAsJsonAsync("/api/v1/patients/register", new
        {
            email = patientEmail, password = Password, fullName = "Workflow Patient", dateOfBirth = "1985-03-14",
            gender = "Female", bloodType = "O+", phoneNumber = "0771234567", nationalId = "198507312345",
            address = "12 Test Road, Colombo", allergies = "Penicillin",
            emergencyContacts = Array.Empty<object>()
        }), HttpStatusCode.Created, "1. register patient");
        var patientId = registered.GetProperty("id").GetGuid();

        // 2. The new patient can log in, and sees their own profile.
        using var patient = await LoginAsync(patientEmail);
        var me = await ExpectAsync(await patient.GetAsync("/api/v1/patients/me"), HttpStatusCode.OK, "2. patient reads own profile");
        Assert.Equal(patientId, me.GetProperty("id").GetGuid());

        // 3. Patient adds an emergency contact and a medical history entry.
        await ExpectAsync(await patient.PostAsJsonAsync($"/api/v1/patients/{patientId}/emergency-contacts",
            new { fullName = "Sam Perera", relationshipToPatient = "Spouse", phoneNumber = "0779876543", isPrimary = true }),
            HttpStatusCode.OK, "3a. add emergency contact");
        var history = await ExpectAsync(await patient.PostAsJsonAsync($"/api/v1/patients/{patientId}/history",
            new { conditionName = "Hypertension", notes = "Diagnosed last year", diagnosedOn = "2025-06-01", isChronic = true, currentMedications = "Amlodipine" }),
            HttpStatusCode.OK, "3b. add medical history");
        Assert.Contains(history.EnumerateArray(), h => h.GetProperty("conditionName").GetString() == "Hypertension");

        // 4. Patient creates an AI plan: three delegation steps, waiting for a doctor.
        var plan = await ExpectAsync(await patient.PostAsJsonAsync($"/api/v1/patients/{patientId}/ai-plan",
            new { objective = "Chest tightness since this morning" }), HttpStatusCode.OK, "4. create AI plan");
        Assert.Equal("PlanCreated", plan.GetProperty("status").GetString());
        Assert.Equal("NotReviewed", plan.GetProperty("reviewStatus").GetString());
        Assert.Equal(3, plan.GetProperty("steps").GetArrayLength());
        var workflowId = plan.GetProperty("id").GetGuid();

        // 5. Approval is enforced: the patient cannot approve their own plan.
        var selfApprove = await patient.PutAsJsonAsync($"/api/v1/patients/{patientId}/ai-plan/{workflowId}/review",
            new { approved = true, reviewNotes = "approve myself" });
        Assert.Equal(HttpStatusCode.Forbidden, selfApprove.StatusCode);

        // 6. The doctor approves the plan; the decision is saved and visible to the patient.
        using var doctor = await LoginAsync(doctorEmail);
        var reviewed = await ExpectAsync(await doctor.PutAsJsonAsync($"/api/v1/patients/{patientId}/ai-plan/{workflowId}/review",
            new { approved = true, reviewNotes = "Reviewed for end-to-end test" }), HttpStatusCode.OK, "6. doctor approves plan");
        Assert.Equal("Approved", reviewed.GetProperty("reviewStatus").GetString());
        var plansSeenByPatient = await ExpectAsync(await patient.GetAsync($"/api/v1/patients/{patientId}/ai-plan"), HttpStatusCode.OK, "6b. patient lists plans");
        Assert.Equal("Approved", plansSeenByPatient[0].GetProperty("reviewStatus").GetString());

        // 7. Patient triggers an emergency alert; the contact is notified and key details are snapshotted.
        var alert = await ExpectAsync(await patient.PostAsync($"/api/v1/patients/{patientId}/emergency-alert", null),
            HttpStatusCode.OK, "7. emergency alert");
        Assert.Equal("O+", alert.GetProperty("bloodGroup").GetString());
        Assert.Single(alert.GetProperty("notifiedContacts").EnumerateArray());
        Assert.Single(_sms.SentMessages);
        Assert.Equal("0779876543", _sms.SentMessages[0].PhoneNumber);

        // 8. The doctor sees the alert in the alert log and the changes in the audit trail.
        var alerts = await ExpectAsync(await doctor.GetAsync("/api/v1/patients/emergency-alerts"), HttpStatusCode.OK, "8a. alert log");
        Assert.True(alerts.GetProperty("totalCount").GetInt32() >= 1);
        var audit = await ExpectAsync(await doctor.GetAsync($"/api/v1/patients/{patientId}/audit-log"), HttpStatusCode.OK, "8b. audit log");
        Assert.True(audit.GetProperty("totalCount").GetInt32() >= 2, "Audit trail should record the history and contact changes.");

        // 9. A different patient cannot see any of this patient's data.
        var otherEmail = "other@workflow.test";
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var other = new ApplicationUser { UserName = otherEmail, Email = otherEmail, FullName = "Other Patient" };
            Assert.True((await users.CreateAsync(other, Password)).Succeeded);
            await users.AddToRoleAsync(other, "Patient");
        }
        using var stranger = await LoginAsync(otherEmail);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/patients/{patientId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await stranger.GetAsync($"/api/v1/patients/{patientId}/ai-plan")).StatusCode);
    }
}
