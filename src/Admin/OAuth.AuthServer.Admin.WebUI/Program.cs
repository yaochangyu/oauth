using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;
using OAuth.AuthServer.Admin.WebUI.Components;
using OAuth.AuthServer.Admin.WebUI.Services;
using OAuth.AuthServer.DB;

var builder = WebApplication.CreateBuilder(args);

// ── Database (共用 AuthServer 的 DbContext + OpenIddict tables) ──────────────
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict();
});

// ── OpenIddict (只需要 Core + EF Core，不需 Server/Validation) ───────────────
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    });

// ── ASP.NET Core Identity（UserManager / RoleManager） ───────────────────────
builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ── Authentication：Cookie + OpenIdConnect 對接 AuthServer ───────────────────
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["OpenIdConnect:Authority"];
    options.ClientId = builder.Configuration["OpenIdConnect:ClientId"];
    options.ClientSecret = builder.Configuration["OpenIdConnect:ClientSecret"];
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles");
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;

    // 保留 JWT claim 原始名稱（不轉成 WS-Federation 長名稱）
    options.MapInboundClaims = false;
    options.TokenValidationParameters.NameClaimType = "name";
    options.TokenValidationParameters.RoleClaimType = "role";

    // 將 UserInfo 回應的 role claim 對應到 ClaimsPrincipal（單值情境）
    options.ClaimActions.MapJsonKey("role", "role");
});

// ── Authorization：admin policy + fallback（所有 Blazor 頁面預設需認證）────────
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("admin"));
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ── MudBlazor ─────────────────────────────────────────────────────────────────
builder.Services.AddMudServices();

// ── Blazor Server ─────────────────────────────────────────────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddHttpContextAccessor();

// ── Admin Services ────────────────────────────────────────────────────────────
builder.Services.AddScoped<ApplicationAdminService>();
builder.Services.AddScoped<ScopeAdminService>();
builder.Services.AddScoped<UserAdminService>();
builder.Services.AddScoped<RoleAdminService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
   .AddInteractiveServerRenderMode();

// ── Login / Logout 端點 ───────────────────────────────────────────────────────
app.MapGet("/login", () => Results.Challenge(
    new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = "/"
    }, [OpenIdConnectDefaults.AuthenticationScheme])).AllowAnonymous();

app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" });
});

app.Run();
