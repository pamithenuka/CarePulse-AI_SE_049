using CarePulse.Api.Entities.Identity;

namespace CarePulse.Api.Services.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IList<string> roles);
}
