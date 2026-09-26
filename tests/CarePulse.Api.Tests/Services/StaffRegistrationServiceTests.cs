using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Staff;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Services.Staff;
using CarePulse.Api.Tests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarePulse.Api.Tests.Services;

public class StaffRegistrationServiceTests
{
    private static CarePulseDbContext CreateInMemoryContext(string? databaseName = null) =>
        new(new DbContextOptionsBuilder<CarePulseDbContext>().UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString()).Options,
            new FakeCurrentUserService("test-actor"));

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

    private static StaffRegistrationService CreateService(CarePulseDbContext db) => new(db, CreateUserManager(db));

    private static async Task EnsureRolesExistAsync(CarePulseDbContext db)
    {
        foreach (var role in new[] { "Doctor", "Nurse" })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role))
            {
                db.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpperInvariant() });
            }
        }
        await db.SaveChangesAsync();
    }

    private static RegisterDoctorDto BuildDoctorDto() => new()
    {
        Email = "doctor.new@carepulse.dev",
        Password = "Doctor@12345",
        FullName = "Dr. Test Doctor",
        Specialty = "Cardiology",
        PhoneNumber = "0771234567"
    };

    private static RegisterNurseDto BuildNurseDto() => new()
    {
        Email = "nurse.new@carepulse.dev",
        Password = "Nurse@12345",
        FullName = "Test Nurse",
        LicenseNumber = "RN-12345",
        Specialization = "Emergency Care"
    };

    [Fact]
    public async Task RegisterDoctorAsync_CreatesLoginAccountAndProfile()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);

        var result = await service.RegisterDoctorAsync(BuildDoctorDto());

        Assert.True(result.Succeeded);
        Assert.Equal("Dr. Test Doctor", result.Value!.FullName);
        Assert.Equal("Cardiology", result.Value.Specialty);
        Assert.NotEmpty(result.Value.UserId);
        Assert.Equal("Active", result.Value.Status);
    }

    [Fact]
    public async Task RegisterDoctorAsync_AssignsDoctorRole()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var userManager = CreateUserManager(db);

        var result = await service.RegisterDoctorAsync(BuildDoctorDto());

        var user = await userManager.FindByIdAsync(result.Value!.UserId);
        var roles = await userManager.GetRolesAsync(user!);
        Assert.Contains("Doctor", roles);
    }

    [Fact]
    public async Task RegisterDoctorAsync_RejectsDuplicateEmail()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        await service.RegisterDoctorAsync(BuildDoctorDto());

        var result = await service.RegisterDoctorAsync(BuildDoctorDto());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task RegisterNurseAsync_CreatesLoginAccountAndProfile()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);

        var result = await service.RegisterNurseAsync(BuildNurseDto());

        Assert.True(result.Succeeded);
        Assert.Equal("Test Nurse", result.Value!.FullName);
        Assert.Equal("RN-12345", result.Value.LicenseNumber);
        Assert.NotEmpty(result.Value.UserId);
        Assert.False(result.Value.IsAvailable);
    }

    [Fact]
    public async Task RegisterNurseAsync_AssignsNurseRole()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var userManager = CreateUserManager(db);

        var result = await service.RegisterNurseAsync(BuildNurseDto());

        var user = await userManager.FindByIdAsync(result.Value!.UserId);
        var roles = await userManager.GetRolesAsync(user!);
        Assert.Contains("Nurse", roles);
    }

    [Fact]
    public async Task RegisterNurseAsync_RejectsDuplicateEmail()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        await service.RegisterNurseAsync(BuildNurseDto());

        var result = await service.RegisterNurseAsync(BuildNurseDto());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task GetDoctorsAsync_ReturnsRegisteredDoctorsSortedByName()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        await service.RegisterDoctorAsync(BuildDoctorDto());
        var second = BuildDoctorDto();
        second.Email = "aaron.doctor@carepulse.dev";
        second.FullName = "Dr. Aaron Early";
        await service.RegisterDoctorAsync(second);

        var doctors = await service.GetDoctorsAsync();

        Assert.Equal(2, doctors.Count);
        Assert.Equal("Dr. Aaron Early", doctors[0].FullName);
    }

    [Fact]
    public async Task GetDoctorByIdAsync_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.GetDoctorByIdAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task GetNursesAsync_ReturnsRegisteredNurses()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        await service.RegisterNurseAsync(BuildNurseDto());

        var nurses = await service.GetNursesAsync();

        Assert.Single(nurses);
        Assert.Equal("Test Nurse", nurses[0].FullName);
    }

    [Fact]
    public async Task GetNurseByIdAsync_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.GetNurseByIdAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteDoctorAsync_HidesDoctorFromActiveList()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var registered = await service.RegisterDoctorAsync(BuildDoctorDto());

        var result = await service.DeleteDoctorAsync(registered.Value!.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(await service.GetDoctorsAsync());
        var doctor = await service.GetDoctorByIdAsync(registered.Value.Id);
        Assert.Equal("Inactive", doctor.Value!.Status);
    }

    [Fact]
    public async Task DeleteDoctorAsync_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.DeleteDoctorAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task RestoreDoctorAsync_ReturnsDoctorToActiveList()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var registered = await service.RegisterDoctorAsync(BuildDoctorDto());
        await service.DeleteDoctorAsync(registered.Value!.Id);

        var result = await service.RestoreDoctorAsync(registered.Value.Id);

        Assert.True(result.Succeeded);
        Assert.Single(await service.GetDoctorsAsync());
        var doctor = await service.GetDoctorByIdAsync(registered.Value.Id);
        Assert.Equal("Active", doctor.Value!.Status);
    }

    [Fact]
    public async Task GetDoctorsAsync_StatusInactive_ReturnsOnlyDeletedDoctors()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var kept = await service.RegisterDoctorAsync(BuildDoctorDto());
        var deleted = await service.RegisterDoctorAsync(new RegisterDoctorDto
        {
            Email = "deleted.doctor@carepulse.dev",
            Password = "Doctor@12345",
            FullName = "Dr. Deleted",
            Specialty = "Neurology",
            PhoneNumber = "0779999999"
        });
        await service.DeleteDoctorAsync(deleted.Value!.Id);

        var active = await service.GetDoctorsAsync("active");
        var inactive = await service.GetDoctorsAsync("inactive");
        var all = await service.GetDoctorsAsync("all");

        Assert.Single(active);
        Assert.Equal(kept.Value!.Id, active[0].Id);
        Assert.Single(inactive);
        Assert.Equal(deleted.Value.Id, inactive[0].Id);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task DeleteNurseAsync_HidesNurseFromActiveList()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var registered = await service.RegisterNurseAsync(BuildNurseDto());

        var result = await service.DeleteNurseAsync(registered.Value!.Id);

        Assert.True(result.Succeeded);
        Assert.Empty(await service.GetNursesAsync());
        var nurse = await service.GetNurseByIdAsync(registered.Value.Id);
        Assert.Equal("Inactive", nurse.Value!.Status);
    }

    [Fact]
    public async Task DeleteNurseAsync_ReturnsNotFoundForUnknownId()
    {
        await using var db = CreateInMemoryContext();
        var service = CreateService(db);

        var result = await service.DeleteNurseAsync(Guid.NewGuid());

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task RestoreNurseAsync_ReturnsNurseToActiveList()
    {
        await using var db = CreateInMemoryContext();
        await EnsureRolesExistAsync(db);
        var service = CreateService(db);
        var registered = await service.RegisterNurseAsync(BuildNurseDto());
        await service.DeleteNurseAsync(registered.Value!.Id);

        var result = await service.RestoreNurseAsync(registered.Value.Id);

        Assert.True(result.Succeeded);
        Assert.Single(await service.GetNursesAsync());
        var nurse = await service.GetNurseByIdAsync(registered.Value.Id);
        Assert.Equal("Active", nurse.Value!.Status);
    }
}
