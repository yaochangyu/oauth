using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;

namespace OAuth.AuthServer.WebAPI.Pages.Connect;

public class ConsentModel(
    IOpenIddictApplicationManager appManager,
    IMemoryCache cache) : PageModel
{
    public string ClientDisplayName { get; private set; } = string.Empty;
    public IReadOnlyList<string> Scopes { get; private set; } = [];

    [BindProperty]
    public string ReturnUrl { get; set; } = string.Empty;

    [BindProperty]
    public string ClientId { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string returnUrl)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded)
        {
            var self = Request.Path + Request.QueryString;
            return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(self)}");
        }

        ReturnUrl = returnUrl;

        var query = QueryHelpers.ParseQuery(new Uri("https://localhost" + returnUrl).Query);
        ClientId = query.TryGetValue("client_id", out var cid) ? cid.ToString() : string.Empty;
        var scope = query.TryGetValue("scope", out var s) ? s.ToString() : string.Empty;

        var app = await appManager.FindByClientIdAsync(ClientId);
        ClientDisplayName = app is not null
            ? (await appManager.GetDisplayNameAsync(app) ?? ClientId)
            : ClientId;

        Scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        return Page();
    }

    public IActionResult OnPostAccept()
    {
        return RedirectWithToken("granted");
    }

    public IActionResult OnPostDeny()
    {
        return RedirectWithToken("denied");
    }

    private IActionResult RedirectWithToken(string decision)
    {
        var clientId = ResolveClientId();
        var token = Guid.NewGuid().ToString("N");
        cache.Set($"consent:{token}", $"{decision}:{clientId}", TimeSpan.FromSeconds(30));

        var separator = ReturnUrl.Contains('?') ? "&" : "?";
        return Redirect($"{ReturnUrl}{separator}__ct={token}");
    }

    private string ResolveClientId()
    {
        if (!string.IsNullOrEmpty(ClientId)) return ClientId;
        try
        {
            var q = QueryHelpers.ParseQuery(new Uri("https://localhost" + ReturnUrl).Query);
            return q.TryGetValue("client_id", out var v) ? v.ToString() : string.Empty;
        }
        catch { return string.Empty; }
    }
}
