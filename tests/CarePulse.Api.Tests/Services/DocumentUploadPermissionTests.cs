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

namespace CarePulse.Api.Tests.Services;

/// <summary>Who may upload, rename and remove a patient's medical documents.</summary>
public class DocumentUploadPermissionTests
{
    private static readonly List<string> PatientRole = new() { "Patient" };
    private static readonly List<string> DoctorRole = new() { "Doctor" };
    private static readonly List<string> AdminRole = new() { "Admin" };
    private static readonly List<string> NurseRole = new() { "Nurse" };

    private static (CarePulseDbContext Db, PatientService Service) Create()
    {
        var db = new CarePulseDbContext(
            new DbContextOptionsBuilder<CarePulseDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            new FakeCurrentUserService("test-actor"));
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>().AddRoles<IdentityRole>().AddEntityFrameworkStores<CarePulseDbContext>();
        var users = services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
        return (db, new PatientService(db, new FakeWebHostEnvironment(), new FakeNotificationService(), users));
    }

    private static async Task<Guid> CreatePatientAsync(PatientService service, string ownerUserId)
    {
        var created = await service.CreateProfileAsync(ownerUserId, new CreatePatientProfileDto
        {
            FullName = "Document Tester",
            DateOfBirth = new DateOnly(1990, 5, 1),
            Gender = "Female",
            BloodType = "O+",
            PhoneNumber = "0771234567",
            NationalId = "199012345V",
            EmergencyContacts = new List<CreateEmergencyContactDto>
            {
                new() { FullName = "Kin", RelationshipToPatient = "Spouse", PhoneNumber = "0779876543", IsPrimary = true }
            }
        });
        return created.Value!.Id;
    }

    private static FormFile Pdf(string name = "lab-result.pdf", int length = 64)
    {
        var bytes = new byte[length];
        var header = "%PDF-1.4"u8.ToArray();
        Array.Copy(header, bytes, Math.Min(header.Length, length));
        return new FormFile(new MemoryStream(bytes), 0, length, "file", name)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    [Fact]
    public async Task Doctor_CanUploadADocumentToAnyPatientsRecord()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");

        var result = await service.UploadDocumentAsync(patientId, "doctor-1", DoctorRole, Pdf(), MedicalDocumentType.LabReport);

        Assert.True(result.Succeeded);
        Assert.Equal("lab-result.pdf", result.Value!.FileName);
        Assert.Single(db.MedicalDocuments);
    }

    [Fact]
    public async Task Doctor_UploadStillGoesThroughTheSameFileValidation()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");

        var empty = await service.UploadDocumentAsync(patientId, "doctor-1", DoctorRole, Pdf(length: 0), MedicalDocumentType.LabReport);
        var wrongType = await service.UploadDocumentAsync(patientId, "doctor-1", DoctorRole, Pdf("virus.exe"), MedicalDocumentType.LabReport);

        Assert.Equal(ServiceErrorType.ValidationFailed, empty.ErrorType);
        Assert.Equal(ServiceErrorType.ValidationFailed, wrongType.ErrorType);
        Assert.Empty(db.MedicalDocuments);
    }

    [Fact]
    public async Task Doctor_CannotUploadToAnUnknownPatient()
    {
        var (db, service) = Create();
        await using var _ = db;

        var result = await service.UploadDocumentAsync(Guid.NewGuid(), "doctor-1", DoctorRole, Pdf(), MedicalDocumentType.Other);

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task OwnerAndAdmin_CanStillUpload()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");

        Assert.True((await service.UploadDocumentAsync(patientId, "patient-owner", PatientRole, Pdf("own.pdf"), MedicalDocumentType.Other)).Succeeded);
        Assert.True((await service.UploadDocumentAsync(patientId, "admin-1", AdminRole, Pdf("admin.pdf"), MedicalDocumentType.Other)).Succeeded);
        Assert.Equal(2, db.MedicalDocuments.Count());
    }

    [Fact]
    public async Task AnotherPatient_CannotUpload()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");

        var result = await service.UploadDocumentAsync(patientId, "someone-else", PatientRole, Pdf(), MedicalDocumentType.Other);

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
        Assert.Empty(db.MedicalDocuments);
    }

    [Fact]
    public async Task Nurse_CannotUpload()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");

        var result = await service.UploadDocumentAsync(patientId, "nurse-1", NurseRole, Pdf(), MedicalDocumentType.Other);

        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Doctor_CannotRenameOrRemoveADocument_EvenOneTheyUploaded()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");
        var uploaded = await service.UploadDocumentAsync(patientId, "doctor-1", DoctorRole, Pdf(), MedicalDocumentType.LabReport);
        var docId = uploaded.Value!.Id;

        var rename = await service.UpdateDocumentAsync(patientId, docId, "doctor-1", DoctorRole,
            new UpdateMedicalDocumentDto { FileName = "renamed.pdf", DocumentType = MedicalDocumentType.Other });
        var delete = await service.DeleteDocumentAsync(patientId, docId, "doctor-1", DoctorRole);

        Assert.Equal(ServiceErrorType.Forbidden, rename.ErrorType);
        Assert.Equal(ServiceErrorType.Forbidden, delete.ErrorType);
        Assert.Equal("lab-result.pdf", (await db.MedicalDocuments.SingleAsync()).FileName);
    }

    [Fact]
    public async Task DocumentUploadedByADoctor_IsVisibleToThePatient()
    {
        var (db, service) = Create();
        await using var _ = db;
        var patientId = await CreatePatientAsync(service, "patient-owner");
        await service.UploadDocumentAsync(patientId, "doctor-1", DoctorRole, Pdf(), MedicalDocumentType.LabReport);

        var asPatient = await service.GetDocumentsAsync(patientId, "patient-owner", PatientRole);

        Assert.True(asPatient.Succeeded);
        Assert.Single(asPatient.Value!);
    }
}
