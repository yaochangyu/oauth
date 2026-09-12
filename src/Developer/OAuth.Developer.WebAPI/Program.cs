using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OAuth.AuthServer.DB;
using OAuth.Developer.WebAPI.Data;
using OAuth.Developer.WebAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=oauth_dev;Username=oauth;Password=oauth_pass";

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.UseOpenIddict();
});

builder.Services.AddDbContext<DeveloperDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var signingKey = builder.Configuration["Jwt:SigningKey"] ?? "DeveloperPortalSecretKeyForJwtAuthenticationTest2026!";
var authority = builder.Configuration["AuthServer:Authority"] ?? "https://localhost:7001";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = authority;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
        NameClaimType = "name",
        RoleClaimType = "role",
    };
});

builder.Services.AddAuthorization();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddValidation(options =>
    {
        options.SetIssuer(new Uri(authority));
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });

builder.Services.AddScoped<SecretRotationManager>();
builder.Services.AddScoped<DeveloperApplicationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev_portal_cors", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173", "https://localhost:5173",
                  "http://localhost:5174", "https://localhost:5174",
                  "http://localhost:5300", "https://localhost:5300",
                  "http://localhost:5400", "https://localhost:5400")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure Developer database schema is created
using (var scope = app.Services.CreateScope())
{
    var devDbContext = scope.ServiceProvider.GetRequiredService<DeveloperDbContext>();
    devDbContext.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""DeveloperProfiles"" (
            ""UserId"" text PRIMARY KEY,
            ""IsDeveloperEnabled"" boolean NOT NULL,
            ""OrganizationName"" text NULL,
            ""ContactEmail"" text NULL,
            ""RegisteredAt"" timestamp with time zone NOT NULL
        );");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("dev_portal_cors");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
