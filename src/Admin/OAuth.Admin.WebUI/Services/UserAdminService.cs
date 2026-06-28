using Microsoft.AspNetCore.Identity;
using OAuth.Admin.WebUI.ViewModels;
using OAuth.AuthServer.DB;

namespace OAuth.Admin.WebUI.Services;

public class UserAdminService(UserManager<ApplicationUser> userManager)
{
    public (IEnumerable<UserDetailsViewModel> Items, int Total) GetPaged(
        int page, int pageSize, string? filter = null)
    {
        var query = userManager.Users.AsQueryable();
        if (!string.IsNullOrEmpty(filter))
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(filter)) ||
                (u.Email != null && u.Email.Contains(filter)));

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserDetailsViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEnabled = u.LockoutEnabled,
                LockoutEnd = u.LockoutEnd,
            }).ToList();

        return (items, total);
    }

    public async Task<UserDetailsViewModel?> GetByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return null;

        var roles = await userManager.GetRolesAsync(user);
        var claims = await userManager.GetClaimsAsync(user);

        return new UserDetailsViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            UserRoles = roles.Select(r => new UserRoleViewModel { RoleName = r }).ToList(),
            UserClaims = claims.Select(c => new ClaimViewModel { Type = c.Type, Value = c.Value }).ToList(),
        };
    }

    public async Task<string?> SetLockoutAsync(string id, bool locked)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return "User not found.";
        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, locked ? DateTimeOffset.MaxValue : null);
        return null;
    }

    public async Task<string?> DeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return "User not found.";
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null ? [] : await userManager.GetRolesAsync(user);
    }

    public async Task<string?> AddToRoleAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return "User not found.";
        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task<string?> RemoveFromRoleAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return "User not found.";
        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }
}
