using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuth.AuthServer.DB;
using OAuth.AuthServer.WebAPI.Account;
using OAuth.AuthServer.WebAPI.Infrastructure;
using OAuth.AuthServer.WebAPI.Infrastructure.Threads;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
});

var config = builder.Configuration;

var authBuilder = builder.Services.AddAuthentication();

var googleClientId = config["Authentication:Google:ClientId"];
var googleClientSecret = config["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });
}

var microsoftClientId = config["Authentication:Microsoft:ClientId"];
var microsoftClientSecret = config["Authentication:Microsoft:ClientSecret"];
if (!string.IsNullOrWhiteSpace(microsoftClientId) && !string.IsNullOrWhiteSpace(microsoftClientSecret))
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = microsoftClientId;
        options.ClientSecret = microsoftClientSecret;
    });
}

var facebookAppId = config["Authentication:Facebook:AppId"];
var facebookAppSecret = config["Authentication:Facebook:AppSecret"];
if (!string.IsNullOrWhiteSpace(facebookAppId) && !string.IsNullOrWhiteSpace(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
    });
}

var lineClientId = config["Authentication:Line:ClientId"];
var lineClientSecret = config["Authentication:Line:ClientSecret"];
if (!string.IsNullOrWhiteSpace(lineClientId) && !string.IsNullOrWhiteSpace(lineClientSecret))
{
    authBuilder.AddLine(options =>
    {
        options.ClientId = lineClientId;
        options.ClientSecret = lineClientSecret;
    });
}

var threadsClientId = config["Authentication:Threads:ClientId"];
var threadsClientSecret = config["Authentication:Threads:ClientSecret"];
if (!string.IsNullOrWhiteSpace(threadsClientId) && !string.IsNullOrWhiteSpace(threadsClientSecret))
{
    authBuilder.AddThreads(options =>
    {
        options.ClientId = threadsClientId;
        options.ClientSecret = threadsClientSecret;
    });
}

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
               .SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo")
               .SetEndSessionEndpointUris("/connect/endsession");

        options.RegisterScopes(
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.OpenId,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Email,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Profile,
            OpenIddict.Abstractions.OpenIddictConstants.Scopes.Roles,
            "offline_access",
            "api");

        // Authorization Code + PKCE
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // Refresh Token Rotation
        options.AllowRefreshTokenFlow();

        // Client Credentials（Server-to-Server 備用）
        options.AllowClientCredentialsFlow();

        // Token 有效期
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));

        // 開發環境使用開發憑證
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddScoped<AccountHandler>();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddHostedService<OpenIddictDataSeeder>();
builder.Services.AddHostedService<AdminUserSeeder>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
        policy.WithOrigins(
                  "http://localhost:5173", "https://localhost:5173",
                  "https://localhost:3000",
                  "http://localhost:5200", "https://localhost:5200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
