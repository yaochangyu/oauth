namespace OAuth.Admin.WebAPI.Models;

public class UserDetailResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public List<string> Roles { get; set; } = [];
    public List<UserClaimModel> Claims { get; set; } = [];
}

public class UserSummaryResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsLocked => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;
    public DateTimeOffset? LockoutEnd { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class UserClaimModel
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class AddRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}
