using System.Net;
using System.Net.Http.Headers;
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

namespace CarePulse.Api.Tests;

/// <summary>POST /api/v1/patients/{id}/documents through the real HTTP pipeline (JWT, role attribute, service rule).</summary>
public class DocumentUploadApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ApplicationUser _owner = NewUser("Owner");
    private readonly ApplicationUser _otherPatient = NewUser("Other");
    private readonly ApplicationUser _doctor = NewUser("Doctor");
    private readonly ApplicationUser _nurse = NewUser("Nurse");
    private readonly ApplicationUser _admin = NewUser("Admin");
    private readonly Guid _patientId = Guid.NewGuid();

    public DocumentUploadApiTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "doc-upload-" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "doc-upload-tests-signing-secret-1234567890-abcdef",
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
        db.Users.AddRange(_owner, _otherPatient, _doctor, _nurse, _admin);
        db.PatientProfiles.Add(new PatientProfile { Id = _patientId, UserId = _owner.Id, FullName = "Owner", NationalId = "199011111111" });
        db.PatientProfiles.Add(new PatientProfile { UserId = _otherPatient.Id, FullName = "Other", NationalId = "199022222222" });
        db.DoctorProfiles.Add(new DoctorProfile { UserId = _doctor.Id, FullName = "Dr Test", Specialty = "CARDIOLOGY" });
        db.NurseProfiles.Add(new NurseProfiles { UserId = _nurse.Id, FullName = "Nurse Test" });
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

    private static MultipartFormDataContent PdfUpload(string name = "lab-result.pdf") => new()
    {
        { new ByteArrayContent("%PDF-1.4 test document"u8.ToArray()), "file", name },
        { new StringContent("LabReport"), "documentType" },
    };

    [Theory]
    [InlineData("Doctor")]
    [InlineData("Admin")]
    public async Task Staff_CanUploadToAPatientsRecord(string role)
    {
        using var client = ClientFor(role == "Doctor" ? _doctor : _admin, role);

        var response = await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OwningPatient_CanUpload()
    {
        using var client = ClientFor(_owner, "Patient");

        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload())).StatusCode);
    }

    [Fact]
    public async Task AnotherPatient_IsForbidden()
    {
        using var client = ClientFor(_otherPatient, "Patient");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload())).StatusCode);
    }

    [Fact]
    public async Task Nurse_IsForbidden()
    {
        using var client = ClientFor(_nurse, "Nurse");

        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload())).StatusCode);
    }

    [Fact]
    public async Task NoToken_IsUnauthorized()
    {
        using var client = ClientFor(null, null);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload())).StatusCode);
    }

    [Fact]
    public async Task Doctor_UploadOfADisallowedFileTypeIsRejected()
    {
        using var client = ClientFor(_doctor, "Doctor");

        var response = await client.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload("malware.exe"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Doctor_CannotRemoveADocument()
    {
        using var doctor = ClientFor(_doctor, "Doctor");
        var uploaded = await doctor.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload());
        var id = System.Text.Json.JsonDocument.Parse(await uploaded.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

        var response = await doctor.DeleteAsync($"/api/v1/patients/{_patientId}/documents/{id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OwnerAndDoctor_CanBothSeeTheUploadedDocument()
    {
        using var doctor = ClientFor(_doctor, "Doctor");
        await doctor.PostAsync($"/api/v1/patients/{_patientId}/documents", PdfUpload());
        using var owner = ClientFor(_owner, "Patient");

        var asOwner = await owner.GetAsync($"/api/v1/patients/{_patientId}/documents");
        var asDoctor = await doctor.GetAsync($"/api/v1/patients/{_patientId}/documents");

        Assert.Contains("lab-result.pdf", await asOwner.Content.ReadAsStringAsync());
        Assert.Contains("lab-result.pdf", await asDoctor.Content.ReadAsStringAsync());
    }
}
