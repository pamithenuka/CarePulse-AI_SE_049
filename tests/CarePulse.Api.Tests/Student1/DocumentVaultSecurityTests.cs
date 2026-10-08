using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Patients;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarePulse.Api.Tests.Student1;

/// <summary>
/// Student 1 (QM): boundary, invalid and authorization tests for the medical document vault.
/// Covers size limits, extension allow-list, magic-byte checks and cross-patient access.
/// </summary>
public class DocumentVaultSecurityTests
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private static readonly List<string> PatientRole = new() { "Patient" };
    private static readonly List<string> DoctorRole = new() { "Doctor" };

    private static readonly byte[] PdfHeader = "%PDF-1.4"u8.ToArray();
    private static readonly byte[] PngHeader = { 137, 80, 78, 71, 13, 10, 26, 10 };
    private static readonly byte[] JpegHeader = { 255, 216, 255, 224 };

    private static CarePulseDbContext CreateDb(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<CarePulseDbContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options,
            new FakeCurrentUserService("test-actor"));

    private static PatientService CreateService(CarePulseDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<CarePulseDbContext>();
        var users = services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
        return new PatientService(db, new FakeWebHostEnvironment(), new FakeNotificationService(), users);
    }

    private static async Task<Guid> CreateProfileAsync(PatientService service, string userId, string nationalId)
    {
        var created = await service.CreateProfileAsync(userId, new CreatePatientProfileDto
        {
            FullName = "Vault Tester",
            DateOfBirth = new DateOnly(1990, 5, 1),
            Gender = "Female",
            BloodType = "O+",
            PhoneNumber = "0771234567",
            NationalId = nationalId,
            EmergencyContacts = new List<CreateEmergencyContactDto>
            {
                new() { FullName = "Kin", RelationshipToPatient = "Spouse", PhoneNumber = "0779876543", IsPrimary = true }
            }
        });
        return created.Value!.Id;
    }

    /// <summary>Builds an upload of <paramref name="length"/> bytes whose first bytes are <paramref name="header"/>.</summary>
    private static FormFile BuildFile(string fileName, byte[] header, int length)
    {
        var bytes = new byte[length];
        Array.Copy(header, bytes, Math.Min(header.Length, length));
        return new FormFile(new MemoryStream(bytes), 0, length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };
    }

    [Fact]
    public async Task Upload_EmptyFile_IsRejected()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile("empty.pdf", PdfHeader, 0), MedicalDocumentType.LabReport);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Fact]
    public async Task Upload_ExactlyAtSizeLimit_IsAccepted()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile("max.pdf", PdfHeader, MaxBytes), MedicalDocumentType.LabReport);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Upload_OneByteOverSizeLimit_IsRejected()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile("big.pdf", PdfHeader, MaxBytes + 1), MedicalDocumentType.LabReport);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Theory]
    [InlineData("malware.exe")]
    [InlineData("script.js")]
    [InlineData("page.html")]
    [InlineData("noextension")]
    public async Task Upload_DisallowedExtension_IsRejected(string fileName)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile(fileName, PdfHeader, 64), MedicalDocumentType.LabReport);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Theory]
    [InlineData("fake.pdf")]
    [InlineData("fake.png")]
    [InlineData("fake.jpg")]
    public async Task Upload_ContentDoesNotMatchExtension_IsRejected(string fileName)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");
        var notAnImage = "MZ-this-is-an-executable"u8.ToArray();

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile(fileName, notAnImage, 64), MedicalDocumentType.LabReport);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Theory]
    [InlineData("scan.PDF", "application/pdf")]
    [InlineData("scan.png", "image/png")]
    [InlineData("scan.jpeg", "image/jpeg")]
    public async Task Upload_ValidTypes_StoreTheServerChosenContentType(string fileName, string expectedContentType)
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "owner", "199012345V");
        var header = expectedContentType switch
        {
            "image/png" => PngHeader,
            "image/jpeg" => JpegHeader,
            _ => PdfHeader
        };

        var result = await service.UploadDocumentAsync(id, "owner", PatientRole, BuildFile(fileName, header, 64), MedicalDocumentType.LabReport);

        Assert.True(result.Succeeded);
        Assert.Equal(expectedContentType, (await db.MedicalDocuments.SingleAsync()).ContentType);
    }

    [Fact]
    public async Task Upload_AnotherPatient_IsForbidden()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "patient-a", "199012345V");

        var result = await service.UploadDocumentAsync(id, "patient-b", PatientRole, BuildFile("a.pdf", PdfHeader, 64), MedicalDocumentType.LabReport);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        Assert.Empty(db.MedicalDocuments);
    }

    [Fact]
    public async Task Upload_Doctor_IsForbidden_BecauseOnlyOwnerOrAdminMayUpload()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "patient-a", "199012345V");

        var result = await service.UploadDocumentAsync(id, "doctor-1", DoctorRole, BuildFile("a.pdf", PdfHeader, 64), MedicalDocumentType.LabReport);

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Download_AnotherPatientIsForbidden_ButOwnerAndDoctorCanDownload()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "patient-a", "199012345V");
        var uploaded = await service.UploadDocumentAsync(id, "patient-a", PatientRole, BuildFile("a.pdf", PdfHeader, 64), MedicalDocumentType.LabReport);
        var docId = uploaded.Value!.Id;

        var intruder = await service.GetDocumentFileAsync(id, docId, "patient-b", PatientRole);
        var owner = await service.GetDocumentFileAsync(id, docId, "patient-a", PatientRole);
        var doctor = await service.GetDocumentFileAsync(id, docId, "doctor-1", DoctorRole);

        Assert.Equal(ServiceErrorType.Forbidden, intruder.ErrorType);
        Assert.True(owner.Succeeded);
        Assert.True(doctor.Succeeded);
    }

    [Fact]
    public async Task Download_AfterSoftDelete_ReturnsNotFound()
    {
        // Each HTTP request gets a fresh DbContext, so the download runs on a second context over
        // the same database; reusing one context would keep the deleted entity tracked.
        var dbName = Guid.NewGuid().ToString();
        Guid id, docId;
        await using (var db = CreateDb(dbName))
        {
            var service = CreateService(db);
            id = await CreateProfileAsync(service, "patient-a", "199012345V");
            var uploaded = await service.UploadDocumentAsync(id, "patient-a", PatientRole, BuildFile("a.pdf", PdfHeader, 64), MedicalDocumentType.LabReport);
            docId = uploaded.Value!.Id;
            Assert.True((await service.DeleteDocumentAsync(id, docId, "patient-a", PatientRole)).Succeeded);
        }

        await using var freshDb = CreateDb(dbName);
        var download = await CreateService(freshDb).GetDocumentFileAsync(id, docId, "patient-a", PatientRole);

        Assert.Equal(ServiceErrorType.NotFound, download.ErrorType);
    }

    [Fact]
    public async Task Upload_PathTraversalInFileName_DoesNotEscapeThePatientFolder()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var id = await CreateProfileAsync(service, "patient-a", "199012345V");

        var result = await service.UploadDocumentAsync(id, "patient-a", PatientRole,
            BuildFile("../../../evil.pdf", PdfHeader, 64), MedicalDocumentType.LabReport);

        Assert.True(result.Succeeded);
        var stored = (await db.MedicalDocuments.SingleAsync()).FilePath;
        Assert.StartsWith($"uploads/patients/{id}/", stored);
        Assert.DoesNotContain("..", stored);
    }
}
