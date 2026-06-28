using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OAuth.AuthServer.DB;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OAuth.AuthServer.WebAPI.Pages.Connect;

public class AuthorizeModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager) : PageModel
{
    public string? ErrorMessage { get; private set; }
    public string? ReturnUrl { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string email,
        string password,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ReturnUrl = returnUrl;

        var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            ErrorMessage = "帳號或密碼錯誤";
            return Page();
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            ErrorMessage = "找不到使用者";
            return Page();
        }

        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("找不到 OpenIddict 授權請求");

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var subClaim = new Claim(Claims.Subject, user.Id);
        subClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        identity.AddClaim(subClaim);

        var emailClaim = new Claim(Claims.Email, user.Email!);
        if (request.HasScope(Scopes.Email))
            emailClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        else
            emailClaim.SetDestinations(Destinations.AccessToken);
        identity.AddClaim(emailClaim);

        var nameClaim = new Claim(Claims.Name, user.DisplayName ?? user.Email!);
        if (request.HasScope(Scopes.Profile))
            nameClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
        else
            nameClaim.SetDestinations(Destinations.AccessToken);
        identity.AddClaim(nameClaim);

        if (request.HasScope(Scopes.Roles))
        {
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                var roleClaim = new Claim(Claims.Role, role);
                roleClaim.SetDestinations(Destinations.AccessToken, Destinations.IdentityToken);
                identity.AddClaim(roleClaim);
            }
        }

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
