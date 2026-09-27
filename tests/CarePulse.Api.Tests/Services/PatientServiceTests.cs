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

public class PatientServiceTests
{
    private static CarePulseDbContext CreateInMemoryContext(string? databaseName = null, string? actingUserId = "test-actor")
    {
        var options = new DbContextOptionsBuilder<CarePulseDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new CarePulseDbContext(options, new FakeCurrentUserService(actingUserId));
    }

    private static UserManager<ApplicationUser> CreateUserManager(CarePulseDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddLogging();
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
        }).AddRoles<IdentityRole>().AddEntityFrameworkStores<CarePulseDbContext>();

        return services.BuildServiceProvider().GetRequiredService<UserManager<ApplicationUser>>();
    }

    private static PatientService CreateService(CarePulseDbContext db, FakeNotificationService? notificationService = null) =>
        new(db, new FakeWebHostEnvironment(), notificationService ?? new FakeNotificationService(), CreateUserManager(db));

    private static async Task EnsurePatientRoleExistsAsync(CarePulseDbContext db)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == "Patient"))
        {
            db.Roles.Add(new IdentityRole("Patient") { NormalizedName = "PATIENT" });
            await db.SaveChangesAsync();
        }
    }

    private static readonly List<string> PatientRole = new() { "Patient" };
    private static readonly List<string> DoctorRole = new() { "Doctor" };
    private static readonly List<string> AdminRole = new() { "Admin" };

    private static CreatePatientProfileDto BuildCreateDto(string nationalId = "199012345V") => new()
    {
        FullName = "Jane Perera",
        DateOfBirth = new DateOnly(1990, 5, 1),
        Gender = "Female",
        BloodType = "O+",
        PhoneNumber = "0771234567",
        NationalId = nationalId,
        Allergies = "Penicillin, Peanuts",
        EmergencyContacts = new List<CreateEmergencyContactDto>
        {
            new() { FullName = "Sam Perera", RelationshipToPatient = "Spouse", PhoneNumber = "0779876543", IsPrimary = true }
        }
    };

    // ---------- Profile CRUD ----------

    [Fact]
    public async Task CreateProfileAsync_CreatesProfileWithEmergencyContacts()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.CreateProfileAsync("user-1", BuildCreateDto());

        Assert.True(result.Succeeded);
        Assert.Equal("Jane Perera", result.Value!.FullName);
        Assert.Single(result.Value.EmergencyContacts);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task CreateProfileAsync_RejectsFutureDateOfBirth()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var dto = BuildCreateDto();
        dto.DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        var result = await service.CreateProfileAsync("user-1", dto);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Fact]
    public async Task CreateProfileAsync_RejectsDuplicateProfileForSameUser()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        await service.CreateProfileAsync("user-1", BuildCreateDto("199012345V"));
        var second = await service.CreateProfileAsync("user-1", BuildCreateDto("200099999V"));

        Assert.False(second.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, second.ErrorType);
    }

    // ---------- Admin web-only onboarding ----------

    private static RegisterPatientDto BuildRegisterDto(string email = "new.patient@carepulse.dev", string nationalId = "198501012345") => new()
    {
        Email = email,
        Password = "Patient@12345",
        FullName = "Nadeesha Kumari",
        DateOfBirth = new DateOnly(1985, 1, 1),
        Gender = "Female",
        BloodType = "A+",
        PhoneNumber = "0712223333",
        NationalId = nationalId,
        EmergencyContacts = new List<CreateEmergencyContactDto>
        {
            new() { FullName = "Ruwan Kumara", RelationshipToPatient = "Husband", PhoneNumber = "0714445555", IsPrimary = true }
        }
    };

    [Fact]
    public async Task RegisterPatientAsync_CreatesLoginAccountAndProfileTogether()
    {
        await using var db = CreateInMemoryContext();
        await EnsurePatientRoleExistsAsync(db);
        var service = CreateService(db);

        var result = await service.RegisterPatientAsync(BuildRegisterDto());

        Assert.True(result.Succeeded);
        Assert.Equal("Nadeesha Kumari", result.Value!.FullName);
        Assert.Single(result.Value.EmergencyContacts);
        Assert.NotEqual(Guid.Empty, result.Value.Id);

        var createdUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "new.patient@carepulse.dev");
        Assert.NotNull(createdUser);
        Assert.Equal(createdUser!.Id, result.Value.UserId);
    }

    [Fact]
    public async Task RegisterPatientAsync_RejectsDuplicateEmail()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var firstDb = CreateInMemoryContext(databaseName))
        {
            await EnsurePatientRoleExistsAsync(firstDb);
            await CreateService(firstDb).RegisterPatientAsync(BuildRegisterDto());
        }

        await using var db = CreateInMemoryContext(databaseName);
        var second = await CreateService(db).RegisterPatientAsync(BuildRegisterDto(nationalId: "199911112222"));

        Assert.False(second.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, second.ErrorType);
    }

    [Fact]
    public async Task UploadDocumentAsync_AllowsAdminToUploadOnBehalfOfPatient()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());

        var content = "admin-uploaded-file"u8.ToArray();
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.Length, "file", "scan.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await service.UploadDocumentAsync(created.Value!.Id, "admin-user", AdminRole, file, MedicalDocumentType.ImagingScan);

        Assert.True(result.Succeeded);
        Assert.Equal("scan.pdf", result.Value!.FileName);
    }

    [Fact]
    public async Task GetProfileDetailAsync_ForbidsAccessFromAnotherPatient()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());

        var result = await service.GetProfileDetailAsync(created.Value!.Id, "different-user", PatientRole);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task GetProfileDetailAsync_AllowsDoctorToViewAnyProfile()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());

        var result = await service.GetProfileDetailAsync(created.Value!.Id, "doctor-user", DoctorRole);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task GetMyProfileAsync_ResolvesTheCallingUsersOwnProfile()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);
        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());

        var result = await service.GetMyProfileAsync("owner-user");

        Assert.True(result.Succeeded);
        Assert.Equal(created.Value!.Id, result.Value!.Id);
    }

    [Fact]
    public async Task GetMyProfileAsync_NotFoundWhenNoProfileExistsYet()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.GetMyProfileAsync("brand-new-user");

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateProfileAsync_PatientCanOnlyChangePhoneAndAddress()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.UpdateProfileAsync(profileId, "owner-user", PatientRole, new UpdatePatientProfileDto
        {
            PhoneNumber = "0709999999",
            Address = "42 New Lane",
            FullName = "Someone Else", // should be ignored — not an Admin
            BloodType = "AB-"
        });

        Assert.True(result.Succeeded);
        Assert.Equal("0709999999", result.Value!.PhoneNumber);
        Assert.Equal("42 New Lane", result.Value.Address);
        Assert.Equal("Jane Perera", result.Value.FullName); // unchanged
        Assert.Equal("O+", result.Value.BloodType); // unchanged
    }

    [Fact]
    public async Task UpdateProfileAsync_AdminCanCorrectAnyField()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.UpdateProfileAsync(profileId, "admin-user", AdminRole, new UpdatePatientProfileDto
        {
            FullName = "Jane P. Corrected",
            BloodType = "AB-"
        });

        Assert.True(result.Succeeded);
        Assert.Equal("Jane P. Corrected", result.Value!.FullName);
        Assert.Equal("AB-", result.Value.BloodType);
    }

    [Fact]
    public async Task DeactivateProfileAsync_SoftDeletesAndHidesFromDefaultQueries()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
        }

        await using var db = CreateInMemoryContext(databaseName, "admin-user");
        var service = CreateService(db);

        var deactivateResult = await service.DeactivateProfileAsync(profileId, "admin-user");
        Assert.True(deactivateResult.Succeeded);

        var activeList = await service.GetPatientsAsync(null, null, null, null, null, "active", "fullName", false, 1, 10);
        Assert.Empty(activeList.Items);

        var inactiveList = await service.GetPatientsAsync(null, null, null, null, null, "inactive", "fullName", false, 1, 10);
        Assert.Single(inactiveList.Items);
        Assert.Equal("Inactive", inactiveList.Items[0].Status);
    }

    // ---------- Medical history ----------

    [Fact]
    public async Task AddMedicalHistoryAsync_AppendsEntryForOwningPatient()
    {
        var databaseName = Guid.NewGuid().ToString();

        Guid profileId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
        }

        // A fresh context mirrors the scoped DbContext each real HTTP request gets.
        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.AddMedicalHistoryAsync(
            profileId,
            "owner-user",
            PatientRole,
            new AddMedicalHistoryDto { ConditionName = "Hypertension", DiagnosedOn = new DateOnly(2024, 1, 1), IsChronic = true, CurrentMedications = "Amlodipine 5mg" });

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!);
        Assert.Equal("Hypertension", result.Value![0].ConditionName);
    }

    [Fact]
    public async Task UpdateMedicalHistoryAsync_PatientCannotResolveOwnCondition()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        Guid historyId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var svc = CreateService(creationDb);
            var created = await svc.CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
            var history = await svc.AddMedicalHistoryAsync(profileId, "owner-user", PatientRole,
                new AddMedicalHistoryDto { ConditionName = "Flu", DiagnosedOn = new DateOnly(2026, 1, 1) });
            historyId = history.Value![0].Id;
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.UpdateMedicalHistoryAsync(profileId, historyId, "owner-user", PatientRole,
            new UpdateMedicalHistoryDto { ConditionName = "Flu", DiagnosedOn = new DateOnly(2026, 1, 1), IsResolved = true });

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task UpdateMedicalHistoryAsync_DoctorCanResolveCondition()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        Guid historyId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var svc = CreateService(creationDb);
            var created = await svc.CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
            var history = await svc.AddMedicalHistoryAsync(profileId, "owner-user", PatientRole,
                new AddMedicalHistoryDto { ConditionName = "Flu", DiagnosedOn = new DateOnly(2026, 1, 1) });
            historyId = history.Value![0].Id;
        }

        await using var db = CreateInMemoryContext(databaseName, "doctor-user");
        var service = CreateService(db);

        var result = await service.UpdateMedicalHistoryAsync(profileId, historyId, "doctor-user", DoctorRole,
            new UpdateMedicalHistoryDto { ConditionName = "Flu", DiagnosedOn = new DateOnly(2026, 1, 1), IsResolved = true, ResolvedOn = new DateOnly(2026, 1, 10) });

        Assert.True(result.Succeeded);
        Assert.True(result.Value!.IsResolved);
        Assert.Equal(new DateOnly(2026, 1, 10), result.Value.ResolvedOn);
    }

    // ---------- Emergency contacts ----------

    [Fact]
    public async Task DeleteEmergencyContactAsync_RefusesToRemoveLastContact()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        Guid contactId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
            contactId = created.Value.EmergencyContacts[0].Id;
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.DeleteEmergencyContactAsync(profileId, contactId, "owner-user", PatientRole);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Fact]
    public async Task DeleteEmergencyContactAsync_SucceedsWhenAnotherContactRemains()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        Guid firstContactId;
        await using (var creationDb = CreateInMemoryContext(databaseName))
        {
            var svc = CreateService(creationDb);
            var created = await svc.CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
            firstContactId = created.Value.EmergencyContacts[0].Id;
            await svc.AddEmergencyContactAsync(profileId, "owner-user", PatientRole,
                new CreateEmergencyContactDto { FullName = "Backup Contact", RelationshipToPatient = "Friend", PhoneNumber = "0711111111" });
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);

        var result = await service.DeleteEmergencyContactAsync(profileId, firstContactId, "owner-user", PatientRole);

        Assert.True(result.Succeeded);
    }

    // ---------- Emergency alert ----------

    [Fact]
    public async Task TriggerEmergencyAlertAsync_FailsValidationWithoutContacts()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var dto = BuildCreateDto();
        dto.EmergencyContacts.Clear();
        var created = await service.CreateProfileAsync("owner-user", dto);

        var result = await service.TriggerEmergencyAlertAsync(created.Value!.Id, "owner-user", PatientRole);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.ValidationFailed, result.ErrorType);
    }

    [Fact]
    public async Task TriggerEmergencyAlertAsync_NotifiesContactsAndSnapshotsKeyDetails()
    {
        await using var db = CreateInMemoryContext();
        var notificationService = new FakeNotificationService();
        var service = CreateService(db, notificationService);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());
        await service.AddMedicalHistoryAsync(created.Value!.Id, "owner-user", PatientRole,
            new AddMedicalHistoryDto { ConditionName = "Type 2 Diabetes", DiagnosedOn = new DateOnly(2020, 1, 1), IsChronic = true, CurrentMedications = "Metformin" });

        var result = await service.TriggerEmergencyAlertAsync(created.Value.Id, "owner-user", PatientRole);

        Assert.True(result.Succeeded);
        Assert.Single(result.Value!.NotifiedContacts);
        Assert.Contains("Type 2 Diabetes", result.Value.ChronicConditions);
        Assert.Contains("Metformin", result.Value.ActiveMedications);
        Assert.Contains("Penicillin", result.Value.Allergies);
        Assert.Single(notificationService.SentMessages);
    }

    // ---------- Registry filtering ----------

    [Fact]
    public async Task GetPatientsAsync_FiltersBySearchTerm()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var secondPatient = BuildCreateDto("200011111V");
        secondPatient.FullName = "Kasun Fernando";

        await service.CreateProfileAsync("user-1", BuildCreateDto("199012345V"));
        await service.CreateProfileAsync("user-2", secondPatient);

        var page = await service.GetPatientsAsync("Jane", null, null, null, null, "active", "fullName", false, 1, 10);

        Assert.Single(page.Items);
        Assert.Equal("Jane Perera", page.Items[0].FullName);
    }

    [Fact]
    public async Task GetPatientsAsync_FiltersByCondition()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var withCondition = await service.CreateProfileAsync("user-1", BuildCreateDto("199012345V"));
        await service.AddMedicalHistoryAsync(withCondition.Value!.Id, "user-1", PatientRole,
            new AddMedicalHistoryDto { ConditionName = "Asthma", DiagnosedOn = new DateOnly(2020, 1, 1) });

        var secondPatient = BuildCreateDto("200011111V");
        secondPatient.FullName = "Kasun Fernando";
        await service.CreateProfileAsync("user-2", secondPatient);

        var page = await service.GetPatientsAsync(null, null, "asthma", null, null, "active", "fullName", false, 1, 10);

        Assert.Single(page.Items);
        Assert.Equal("Jane Perera", page.Items[0].FullName);
        Assert.Contains("Asthma", page.Items[0].MainConditions);
    }

    // ---------- Audit log ----------

    [Fact]
    public async Task SaveChanges_WritesAuditLogEntryOnProfileUpdate()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid profileId;
        await using (var creationDb = CreateInMemoryContext(databaseName, "owner-user"))
        {
            var created = await CreateService(creationDb).CreateProfileAsync("owner-user", BuildCreateDto());
            profileId = created.Value!.Id;
        }

        await using (var updateDb = CreateInMemoryContext(databaseName, "admin-user"))
        {
            await CreateService(updateDb).UpdateProfileAsync(profileId, "admin-user", AdminRole,
                new UpdatePatientProfileDto { BloodType = "AB-" });
        }

        await using var db = CreateInMemoryContext(databaseName);
        var service = CreateService(db);
        var auditResult = await service.GetAuditLogAsync(profileId, null, null, null, 1, 50);

        Assert.Contains(auditResult.Items, log => log.EntityName == nameof(PatientProfile) && log.Action == "Updated" && log.ChangedByUserId == "admin-user");
    }

    // ---------- Agent 1 context ----------

    [Fact]
    public async Task GetPatientContextAsync_ReturnsSummaryForAgentConsumption()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());
        await service.AddMedicalHistoryAsync(created.Value!.Id, "owner-user", PatientRole,
            new AddMedicalHistoryDto { ConditionName = "Type 2 Diabetes", DiagnosedOn = new DateOnly(2020, 1, 1), IsChronic = true, CurrentMedications = "Metformin" });

        var result = await service.GetPatientContextAsync(created.Value.Id);

        Assert.True(result.Succeeded);
        Assert.Equal("Jane Perera", result.Value!.FullName);
        Assert.Contains("Type 2 Diabetes", result.Value.ChronicConditions);
        Assert.Contains("Metformin", result.Value.ActiveMedications);
        Assert.Contains("Penicillin", result.Value.Allergies);
        Assert.Single(result.Value.EmergencyContacts);
    }

    // ---------- Documents ----------

    [Fact]
    public async Task UploadDocumentAsync_StoresFileAndReturnsAccessibleUrl()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var created = await service.CreateProfileAsync("owner-user", BuildCreateDto());

        var content = "test-file-content"u8.ToArray();
        var stream = new MemoryStream(content);
        var file = new FormFile(stream, 0, content.Length, "file", "report.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await service.UploadDocumentAsync(created.Value!.Id, "owner-user", PatientRole, file, MedicalDocumentType.LabReport);

        Assert.True(result.Succeeded);
        Assert.Equal("report.pdf", result.Value!.FileName);
        Assert.StartsWith("/uploads/", result.Value.FileUrl);
    }
}
