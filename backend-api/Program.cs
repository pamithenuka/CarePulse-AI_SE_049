using System.Text;
using CarePulse.Api.Data;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Middleware;
using CarePulse.Api.Services.Ai;
using CarePulse.Api.Services.Auth;
using CarePulse.Api.Services.Common;
using CarePulse.Api.Services.Notifications;
using CarePulse.Api.Services.Patients;
using CarePulse.Api.Services.Staff;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;
using CarePulse.Api.Services.Dispatch;
using CarePulse.Api.DTOs.Dispatch;
using CarePulse.Api.Services.Agents;

var builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.IsEnvironment("Testing"))
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);

// 1. Serilog Setup
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// 2. Database Connection (PostgreSQL)
builder.Services.AddDbContext<CarePulseDbContext>((sp, options) =>
    options.UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();

builder.Services.AddScoped<ReadOnlyCarePulseDbContext>(sp => new ReadOnlyCarePulseDbContext(
    new DbContextOptionsBuilder<CarePulseDbContext>().UseNpgsql(sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")).Options,
    sp.GetRequiredService<ICurrentUserService>()));

builder.Services.AddScoped<CarePulse.Api.Services.GeminiAgentService>();
builder.Services.AddScoped<CarePulse.Api.Services.ISchedulingAgent>(sp => sp.GetRequiredService<CarePulse.Api.Services.GeminiAgentService>());
builder.Services.AddScoped<TriageWorkflowRunner>();
if (!builder.Environment.IsEnvironment("Testing")) builder.Services.AddHostedService<WorkflowRecoveryService>();

// 3. ASP.NET Core Identity Setup
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<CarePulseDbContext>()
.AddDefaultTokenProviders();

// 4. JWT Authentication Setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var secret = jwtSettings["Secret"];
    if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        throw new InvalidOperationException("Set Jwt__Secret (at least 32 characters). See README.md.");
    var key = Encoding.UTF8.GetBytes(secret);
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(15),
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<CarePulseDbContext>();
            var id = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var principal = context.Principal!;
            var active = id != null && await db.Users.AnyAsync(u => u.Id == id);
            if (principal.IsInRole("Doctor")) active &= await db.DoctorProfiles.AnyAsync(d => d.UserId == id);
            if (principal.IsInRole("Nurse")) active &= await db.NurseProfiles.AnyAsync(n => n.UserId == id);
            if (principal.IsInRole("Patient")) active &= !await db.PatientProfiles.IgnoreQueryFilters().AnyAsync(p => p.UserId == id && p.IsDeleted);
            if (!active) context.Fail("Account is no longer active.");
        }
    };
});

builder.Services.AddAuthorization(options => options.FallbackPolicy =
    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// 5. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>()).AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation()
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining<AssignDispatchDtoValidator>();
builder.Services.AddEndpointsApiExplorer();

// Register Triage service
builder.Services.AddScoped<CarePulse.Api.Services.ITriageAiAgent, CarePulse.Api.Services.TriageAiAgent>();
builder.Services.AddScoped<CarePulse.Api.Services.ITriageService, CarePulse.Api.Services.TriageService>();

// Student 4
builder.Services.AddScoped<IGoogleMapsService, GoogleMapsService>();
builder.Services.AddScoped<IValidationAgent, ValidationAgent>();

// 6. Swagger / OpenAPI Configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CarePulse REST API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 7. Health Checks
builder.Services.AddHealthChecks();

// 8. Application Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<INotificationService, SimulatedSmsNotificationService>();
builder.Services.AddScoped<IStaffRegistrationService, StaffRegistrationService>();

// 9. Agent 1 (Planner/Coordinator) — calls Gemini API
builder.Services.AddHttpClient<IAiPlannerClient, GeminiAiPlannerClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<IAgentPlannerService, AgentPlannerService>();

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:SeedOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

// Configure HTTP Request Pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CarePulse API v1"));
}

// Medical files are served exclusively through authorized controller actions.
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();

public partial class Program { }
