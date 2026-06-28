using Microsoft.AspNetCore.Identity;
using OAuth.Admin.WebUI.ViewModels;

namespace OAuth.Admin.WebUI.Services;

public class RoleAdminService(RoleManager<IdentityRole> roleManager)
{
    public (IEnumerable<UserRoleViewModel> Items, int Total) GetPaged(
        int page, int pageSize, string? filter = null)
    {
        var query = roleManager.Roles.AsQueryable();
        if (!string.IsNullOrEmpty(filter))
            query = query.Where(r => r.Name != null && r.Name.Contains(filter));

        var total = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new UserRoleViewModel
            {
                RoleId = r.Id,
                RoleName = r.Name ?? string.Empty,
            }).ToList();

        return (items, total);
    }

    public async Task<string?> CreateAsync(string roleName)
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return $"Role '{roleName}' already exists.";
        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }

    public async Task<string?> DeleteAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null) return "Role not found.";
        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded ? null : string.Join(", ", result.Errors.Select(e => e.Description));
    }
}
