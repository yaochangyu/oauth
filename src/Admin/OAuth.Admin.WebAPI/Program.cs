using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OAuth.Admin.WebAPI;
using OAuth.Admin.WebAPI.Services;
using OAuth.AuthServer.DB;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=oauth;Username=oauth;Password=oauth_pass";

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
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// 測試與開發環境支援 TestAuthHandler 模擬授權驗證
if (builder.Environment.IsEnvironment("Test") || builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = TestAuthHandler.SchemeName;
        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
        options.DefaultForbidScheme = TestAuthHandler.SchemeName;
    })
    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireRole("Administrator", "admin"));
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    });

builder.Services.AddSingleton<IAuditLogService, AuditLogService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("admin-spa", policy =>
        policy.WithOrigins(
                  "http://localhost:5173", "https://localhost:5173",
                  "http://localhost:5174", "https://localhost:5174",
                  "http://localhost:7002", "https://localhost:7002",
                  "http://localhost:5279", "https://localhost:5279")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("admin-spa");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
