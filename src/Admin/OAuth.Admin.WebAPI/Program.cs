using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
