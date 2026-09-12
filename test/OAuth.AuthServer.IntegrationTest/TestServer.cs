using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OAuth.AuthServer.DB;

namespace OAuth.AuthServer.IntegrationTest;

public class AuthServerTestFactory : WebApplicationFactory<OAuth.AuthServer.WebAPI.Connect.AuthorizationController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // 移除原本的 DbContextFactory，換成測試用（由環境變數注入 connection string）
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(IDbContextFactory<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            var dbContextDescriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseOpenIddict();
            });
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        // 直接建立 DbContext 執行 migration，避免透過 Services 啟動 host（會觸發 hosted services
        // 在 migration 完成前存取 OpenIddict 資料表，導致 42P01 relation does not exist）
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .UseOpenIddict()
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        await dbContext.Database.MigrateAsync();

        var devOptions = new DbContextOptionsBuilder<OAuth.Developer.WebAPI.Data.DeveloperDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var devDbContext = new OAuth.Developer.WebAPI.Data.DeveloperDbContext(devOptions);
        await devDbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""DeveloperProfiles"" (
                ""UserId"" text PRIMARY KEY,
                ""IsDeveloperEnabled"" boolean NOT NULL,
                ""OrganizationName"" text NULL,
                ""ContactEmail"" text NULL,
                ""RegisteredAt"" timestamp with time zone NOT NULL
            );");
    }
}

public class DeveloperTestFactory : WebApplicationFactory<OAuth.Developer.WebAPI.Controllers.ApplicationsController>
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
                d.ServiceType == typeof(DbContextOptions<OAuth.Developer.WebAPI.Data.DeveloperDbContext>));
            if (devDbContextDescriptor is not null)
                services.Remove(devDbContextDescriptor);

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseOpenIddict();
            });

            services.AddDbContext<OAuth.Developer.WebAPI.Data.DeveloperDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });

            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey = TestAssistant.AuthServerRsaSigningKey;
                });
        });
    }
}

public class AccountTestFactory : WebApplicationFactory<OAuth.Account.WebAPI.Controllers.ConsentsController>
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

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? throw new InvalidOperationException("測試用 PostgreSQL Connection String 未設定");

            services.AddDbContextFactory<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.UseOpenIddict();
            });

            services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey = TestAssistant.AuthServerRsaSigningKey;
                });
        });
    }
}

