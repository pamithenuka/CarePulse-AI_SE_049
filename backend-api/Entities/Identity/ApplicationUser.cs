using Microsoft.AspNetCore.Identity;

namespace CarePulse.Api.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
