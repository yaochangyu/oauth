using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OAuth.AuthServer.DB;

namespace OAuth.AuthServer.WebAPI.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    public string? ErrorMessage { get; private set; }
    public string? ReturnUrl { get; private set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(
        string userName,
        string password,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        ReturnUrl = returnUrl;

        var result = await signInManager.PasswordSignInAsync(
            userName, password, isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ErrorMessage = "帳號或密碼錯誤";
            return Page();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect("/");
    }
}
