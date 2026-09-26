using CarePulse.Api.Data;
using CarePulse.Api.DTOs.Auth;
using CarePulse.Api.Entities.Identity;
using CarePulse.Api.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarePulse.Api.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly CarePulseDbContext _db;

    public AuthController(UserManager<ApplicationUser> userManager, ITokenService tokenService, CarePulseDbContext db)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (!DbSeeder.Roles.Contains(dto.Role))
        {
            return BadRequest(new { message = $"Role must be one of: {string.Join(", ", DbSeeder.Roles)}" });
        }

        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Registration failed.", errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok(await BuildAuthResponseAsync(user));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Doctor/Nurse accounts can be soft-deleted by an Admin (StaffRegistrationService)
        // without erasing them - block login here rather than at the profile lookup, so a
        // deleted account can't even obtain a token. IgnoreQueryFilters: the global
        // soft-delete filter would otherwise hide the very row we need to check.
        if (roles.Contains("Doctor"))
        {
            var doctor = await _db.DoctorProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.UserId == user.Id);
            if (doctor is not null && doctor.IsDeleted)
            {
                return Unauthorized(new { message = "This account has been deleted. Contact your administrator." });
            }
        }
        else if (roles.Contains("Nurse"))
        {
            var nurse = await _db.NurseProfiles.IgnoreQueryFilters().FirstOrDefaultAsync(n => n.UserId == user.Id);
            if (nurse is not null && nurse.IsDeleted)
            {
                return Unauthorized(new { message = "This account has been deleted. Contact your administrator." });
            }
        }

        return Ok(await BuildAuthResponseAsync(user));
    }

    private async Task<AuthResponseDto> BuildAuthResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = _tokenService.GenerateToken(user, roles);

        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles,
            Token = token,
            ExpiresAt = expiresAt
        };
    }
}
