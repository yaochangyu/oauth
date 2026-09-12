using OAuth.AuthServer.Admin.WebUI.ViewModels;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.Admin.WebUI.Services;

public class ApplicationAdminService(IOpenIddictApplicationManager manager)
{
    public async Task<(IEnumerable<ApplicationViewModel> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? filter = null)
    {
        var all = new List<ApplicationViewModel>();
        await foreach (var app in manager.ListAsync(cancellationToken: default))
        {
            var clientId = await manager.GetClientIdAsync(app);
            var displayName = await manager.GetDisplayNameAsync(app);
            if (!string.IsNullOrEmpty(filter) &&
                !(clientId?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                  displayName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true))
                continue;

            all.Add(new ApplicationViewModel
            {
                Id = await manager.GetIdAsync(app) ?? string.Empty,
                ClientId = clientId ?? string.Empty,
                DisplayName = displayName ?? string.Empty,
                ConsentType = await manager.GetConsentTypeAsync(app) ?? string.Empty,
                ClientType = await manager.GetClientTypeAsync(app) ?? string.Empty,
            });
        }
        return (all.Skip((page - 1) * pageSize).Take(pageSize), all.Count);
    }

    public async Task<ApplicationViewModel?> GetByClientIdAsync(string clientId)
    {
        var app = await manager.FindByClientIdAsync(clientId);
        if (app is null) return null;

        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, app);

        return new ApplicationViewModel
        {
            Id = await manager.GetIdAsync(app) ?? string.Empty,
            ClientId = descriptor.ClientId ?? string.Empty,
            DisplayName = descriptor.DisplayName ?? string.Empty,
            ClientType = descriptor.ClientType ?? string.Empty,
            ConsentType = descriptor.ConsentType ?? string.Empty,
            ApplicationType = descriptor.ApplicationType ?? ApplicationTypes.Web,
            RedirectUris = descriptor.RedirectUris.ToList(),
            PostLogoutRedirectUris = descriptor.PostLogoutRedirectUris.ToList(),
            Permissions = descriptor.Permissions.ToList(),
            Requirements = descriptor.Requirements.ToList(),
        };
    }

    public async Task<string?> CreateAsync(ApplicationViewModel vm)
    {
        var descriptor = ToDescriptor(vm);
        if (!string.IsNullOrEmpty(vm.ClientSecret))
            descriptor.ClientSecret = vm.ClientSecret;

        try
        {
            var result = await manager.CreateAsync(descriptor);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> UpdateAsync(ApplicationViewModel vm)
    {
        var app = await manager.FindByClientIdAsync(vm.ClientId);
        if (app is null) return "Application not found.";

        var descriptor = new OpenIddictApplicationDescriptor();
        await manager.PopulateAsync(descriptor, app);

        var updated = ToDescriptor(vm);
        descriptor.ClientId = updated.ClientId;
        descriptor.DisplayName = updated.DisplayName;
        descriptor.ClientType = updated.ClientType;
        descriptor.ConsentType = updated.ConsentType;
        descriptor.RedirectUris.Clear();
        foreach (var uri in updated.RedirectUris) descriptor.RedirectUris.Add(uri);
        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in updated.PostLogoutRedirectUris) descriptor.PostLogoutRedirectUris.Add(uri);
        descriptor.Permissions.Clear();
        foreach (var p in updated.Permissions) descriptor.Permissions.Add(p);
        descriptor.Requirements.Clear();
        foreach (var r in updated.Requirements) descriptor.Requirements.Add(r);

        if (!string.IsNullOrEmpty(vm.ClientSecret))
            descriptor.ClientSecret = vm.ClientSecret;

        try
        {
            await manager.UpdateAsync(app, descriptor);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public async Task<string?> DeleteAsync(string clientId)
    {
        var app = await manager.FindByClientIdAsync(clientId);
        if (app is null) return "Application not found.";
        try
        {
            await manager.DeleteAsync(app);
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static OpenIddictApplicationDescriptor ToDescriptor(ApplicationViewModel vm)
    {
        var d = new OpenIddictApplicationDescriptor
        {
            ClientId = vm.ClientId,
            DisplayName = vm.DisplayName,
            ClientType = vm.ClientType,
            ConsentType = vm.ConsentType,
            ApplicationType = vm.ApplicationType,
        };
        foreach (var uri in vm.RedirectUris) d.RedirectUris.Add(uri);
        foreach (var uri in vm.PostLogoutRedirectUris) d.PostLogoutRedirectUris.Add(uri);
        foreach (var p in vm.Permissions) d.Permissions.Add(p);
        foreach (var r in vm.Requirements) d.Requirements.Add(r);
        return d;
    }
}
