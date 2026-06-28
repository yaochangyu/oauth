using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var oidcConfig = builder.Configuration.GetSection("OpenIdConnect");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.SlidingExpiration = true;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = oidcConfig["Authority"];
        options.ClientId = oidcConfig["ClientId"];
        options.ClientSecret = oidcConfig["ClientSecret"];
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("roles");
        options.Scope.Add("offline_access");
        options.Scope.Add("api");

        // ID token 中的 "role" claim 被識別為角色
        options.TokenValidationParameters.RoleClaimType = "role";

        options.Events = new OpenIdConnectEvents
        {
            OnUserInformationReceived = ctx =>
            {
                // UserInfo endpoint 回傳的 "role" 手動加到 ClaimsPrincipal
                if (ctx.User.RootElement.TryGetProperty("role", out var roleElement)
                    && ctx.Principal?.Identity is ClaimsIdentity identity)
                {
                    if (roleElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roleElement.EnumerateArray())
                            identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                    }
                    else if (roleElement.ValueKind == JsonValueKind.String)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleElement.GetString() ?? ""));
                    }
                }
                return Task.CompletedTask;
            },
        };

        options.RequireHttpsMetadata = false; // 開發環境
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
