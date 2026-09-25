using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Entities.Patients;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Data;

public static class DbSeeder
{
    public static readonly string[] Roles = { "Admin", "Doctor", "Nurse", "Patient" };

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, "admin@carepulse.dev", "Admin@12345", "System Administrator", "Admin");
        await EnsureUserAsync(userManager, "doctor@carepulse.dev", "Doctor@12345", "Dr. Amara Silva", "Doctor");

        await SeedDummyPatientsAsync(services, userManager);
    }

    private static async Task<ApplicationUser?> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return existing;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    /// <summary>
    /// Demo/dummy patients so the web app has realistic data to show without needing
    /// the mobile app or a manual walkthrough first. Idempotent — skipped if any
    /// patient profile already exists.
    /// </summary>
    private static async Task SeedDummyPatientsAsync(IServiceProvider services, UserManager<ApplicationUser> userManager)
    {
        var db = services.GetRequiredService<CarePulseDbContext>();
        if (await db.PatientProfiles.IgnoreQueryFilters().AnyAsync(p => p.NationalId == "197907001234"))
        {
            return;
        }

        var kasun = await EnsureUserAsync(userManager, "kasun.fernando@patient.carepulse.dev", "Patient@12345", "Kasun Fernando", "Patient");
        var priya = await EnsureUserAsync(userManager, "priya.jayasinghe@patient.carepulse.dev", "Patient@12345", "Priya Jayasinghe", "Patient");
        var sunil = await EnsureUserAsync(userManager, "sunil.perera@patient.carepulse.dev", "Patient@12345", "Sunil Perera", "Patient");

        if (kasun is null || priya is null || sunil is null)
        {
            return;
        }

        var kasunProfile = new PatientProfile
        {
            UserId = kasun.Id,
            FullName = "Kasun Fernando",
            DateOfBirth = new DateOnly(1979, 3, 12),
            Gender = "Male",
            BloodType = "O+",
            PhoneNumber = "0771112233",
            Address = "45 Kandy Road, Kurunegala",
            NationalId = "197907001234",
            EmergencyContacts = new List<EmergencyContact>
            {
                new() { FullName = "Malani Fernando", RelationshipToPatient = "Wife", PhoneNumber = "0777778888", IsPrimary = true }
            },
            MedicalHistories = new List<MedicalHistory>
            {
                new()
                {
                    ConditionName = "Hypertension",
                    DiagnosedOn = new DateOnly(2015, 6, 10),
                    IsChronic = true,
                    CurrentMedications = "Losartan 50mg once daily",
                    RecordedByUserId = "system"
                }
            }
        };

        var priyaProfile = new PatientProfile
        {
            UserId = priya.Id,
            FullName = "Priya Jayasinghe",
            DateOfBirth = new DateOnly(1993, 11, 5),
            Gender = "Female",
            BloodType = "A+",
            PhoneNumber = "0712223344",
            Address = "12 Lake Road, Colombo 05",
            NationalId = "935671234V",
            Allergies = "Penicillin",
            EmergencyContacts = new List<EmergencyContact>
            {
                new() { FullName = "Sriyani Jayasinghe", RelationshipToPatient = "Mother", PhoneNumber = "0713334455", IsPrimary = true }
            },
            MedicalHistories = new List<MedicalHistory>
            {
                new()
                {
                    ConditionName = "Asthma",
                    DiagnosedOn = new DateOnly(2010, 4, 1),
                    IsChronic = true,
                    CurrentMedications = "Salbutamol inhaler as needed",
                    RecordedByUserId = "system"
                }
            }
        };

        var sunilProfile = new PatientProfile
        {
            UserId = sunil.Id,
            FullName = "Sunil Perera",
            DateOfBirth = new DateOnly(1958, 7, 20),
            Gender = "Male",
            BloodType = "B-",
            PhoneNumber = "0765556677",
            Address = "8 Temple Lane, Galle",
            NationalId = "580201234V",
            Allergies = "Sulfa drugs",
            EmergencyContacts = new List<EmergencyContact>
            {
                new() { FullName = "Chaminda Perera", RelationshipToPatient = "Son", PhoneNumber = "0761112233", IsPrimary = true }
            },
            MedicalHistories = new List<MedicalHistory>
            {
                new()
                {
                    ConditionName = "Type 2 Diabetes",
                    DiagnosedOn = new DateOnly(2005, 9, 15),
                    IsChronic = true,
                    CurrentMedications = "Metformin 500mg twice daily",
                    RecordedByUserId = "system"
                },
                new()
                {
                    ConditionName = "Fractured wrist",
                    DiagnosedOn = new DateOnly(2022, 1, 10),
                    IsChronic = false,
                    IsResolved = true,
                    ResolvedOn = new DateOnly(2022, 3, 1),
                    RecordedByUserId = "system"
                }
            }
        };

        db.PatientProfiles.AddRange(kasunProfile, priyaProfile, sunilProfile);
        await db.SaveChangesAsync();

        // One example emergency alert so the Emergency Alert Log page isn't empty on first run.
        var alertLog = new EmergencyAlertLog
        {
            PatientProfileId = sunilProfile.Id,
            TriggeredByUserId = sunil.Id,
            TriggeredAt = DateTime.UtcNow.AddDays(-3),
            BloodGroupSnapshot = sunilProfile.BloodType,
            AllergiesSnapshot = sunilProfile.Allergies,
            ActiveMedicationsSnapshot = "Metformin 500mg twice daily",
            ChronicConditionsSnapshot = "Type 2 Diabetes",
            Notifications = new List<EmergencyAlertNotification>
            {
                new()
                {
                    ContactName = "Chaminda Perera",
                    PhoneNumber = "0761112233",
                    DeliveryStatus = NotificationDeliveryStatus.Sent,
                    DeliveredAt = DateTime.UtcNow.AddDays(-3)
                }
            }
        };
        sunilProfile.LastEmergencyBroadcastAt = alertLog.TriggeredAt;

        db.EmergencyAlertLogs.Add(alertLog);
        await db.SaveChangesAsync();
    }
}
