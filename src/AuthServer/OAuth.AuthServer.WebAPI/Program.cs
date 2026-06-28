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

var config = builder.Configuration;

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = config["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = config["Authentication:Google:ClientSecret"] ?? string.Empty;
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = config["Authentication:Microsoft:ClientId"] ?? string.Empty;
        options.ClientSecret = config["Authentication:Microsoft:ClientSecret"] ?? string.Empty;
    })
    .AddFacebook(options =>
    {
        options.AppId = config["Authentication:Facebook:AppId"] ?? string.Empty;
        options.AppSecret = config["Authentication:Facebook:AppSecret"] ?? string.Empty;
        options.Scope.Add("email");
        options.Scope.Add("public_profile");
    })
    .AddLine(options =>
    {
        options.ClientId = config["Authentication:Line:ClientId"] ?? string.Empty;
        options.ClientSecret = config["Authentication:Line:ClientSecret"] ?? string.Empty;
    })
    .AddThreads(options =>
    {
        options.ClientId = config["Authentication:Threads:ClientId"] ?? string.Empty;
        options.ClientSecret = config["Authentication:Threads:ClientSecret"] ?? string.Empty;
    });

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
