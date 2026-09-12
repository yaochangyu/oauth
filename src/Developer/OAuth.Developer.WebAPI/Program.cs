using Microsoft.EntityFrameworkCore;
using OAuth.AuthServer.DB;
using OAuth.Developer.WebAPI.Services;

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

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("dev_portal_cors");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
