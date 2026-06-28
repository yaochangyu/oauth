using Microsoft.AspNetCore.Identity;

namespace OAuth.AuthServer.DB;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
