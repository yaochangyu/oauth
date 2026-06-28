using OAuth.AuthServer.Admin.WebUI.ViewModels;
using OpenIddict.Abstractions;

namespace OAuth.AuthServer.Admin.WebUI.Services;

public class ScopeAdminService(IOpenIddictScopeManager manager)
{
    public async Task<(IEnumerable<ScopeViewModel> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? filter = null)
    {
        var all = new List<ScopeViewModel>();
        await foreach (var scope in manager.ListAsync(cancellationToken: default))
        {
            var name = await manager.GetNameAsync(scope);
            var displayName = await manager.GetDisplayNameAsync(scope);
            if (!string.IsNullOrEmpty(filter) &&
                !(name?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                  displayName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true))
                continue;

            all.Add(new ScopeViewModel
            {
                Id = await manager.GetIdAsync(scope) ?? string.Empty,
                Name = name ?? string.Empty,
                DisplayName = displayName ?? string.Empty,
                Description = await manager.GetDescriptionAsync(scope) ?? string.Empty,
                Resources = (await manager.GetResourcesAsync(scope)).ToList(),
            });
        }
        return (all.Skip((page - 1) * pageSize).Take(pageSize), all.Count);
    }

    public async Task<ScopeViewModel?> GetByIdAsync(string id)
    {
        var scope = await manager.FindByIdAsync(id);
        if (scope is null) return null;

        var descriptor = new OpenIddictScopeDescriptor();
        await manager.PopulateAsync(descriptor, scope);

        return new ScopeViewModel
        {
            Id = id,
            Name = descriptor.Name ?? string.Empty,
            DisplayName = descriptor.DisplayName ?? string.Empty,
            Description = descriptor.Description ?? string.Empty,
            Resources = descriptor.Resources.ToList(),
        };
    }

    public async Task<string?> CreateAsync(ScopeViewModel vm)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = vm.Name,
            DisplayName = vm.DisplayName,
            Description = vm.Description,
        };
        foreach (var r in vm.Resources) descriptor.Resources.Add(r);
        try
        {
            await manager.CreateAsync(descriptor);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> UpdateAsync(ScopeViewModel vm)
    {
        var scope = await manager.FindByIdAsync(vm.Id);
        if (scope is null) return "Scope not found.";

        var descriptor = new OpenIddictScopeDescriptor();
        await manager.PopulateAsync(descriptor, scope);
        descriptor.Name = vm.Name;
        descriptor.DisplayName = vm.DisplayName;
        descriptor.Description = vm.Description;
        descriptor.Resources.Clear();
        foreach (var r in vm.Resources) descriptor.Resources.Add(r);

        try
        {
            await manager.UpdateAsync(scope, descriptor);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> DeleteAsync(string id)
    {
        var scope = await manager.FindByIdAsync(id);
        if (scope is null) return "Scope not found.";
        try
        {
            await manager.DeleteAsync(scope);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
