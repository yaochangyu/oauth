using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OAuth.AuthServer.DB;
using OAuth.Developer.WebAPI.Data;

namespace OAuth.Developer.Tests;

public class DeveloperTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            var devDbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<DeveloperDbContext>));
            if (devDbContextDescriptor is not null)
                services.Remove(devDbContextDescriptor);

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseOpenIddict();
            });

            services.AddDbContext<DeveloperDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .UseOpenIddict()
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.MigrateAsync();

        var devOptions = new DbContextOptionsBuilder<DeveloperDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var devDbContext = new DeveloperDbContext(devOptions);
        await devDbContext.Database.EnsureCreatedAsync();
    }
}
