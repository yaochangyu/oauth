using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OAuth.AuthServer.DB;

namespace OAuth.AuthServer.IntegrationTest;

public class AuthServerTestFactory : WebApplicationFactory<Program>
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
    }
}
