using Microsoft.AspNetCore.Identity;
using OAuth.AuthServer.DB;

namespace OAuth.AuthServer.WebAPI.Infrastructure;

public class AdminUserSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (!await roleManager.RoleExistsAsync("admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("admin"));
        }

        const string userName = "admin";
        const string email    = "admin@localhost";
        const string password = "Admin@123456";

        if (await userManager.FindByNameAsync(userName) is null)
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email    = email,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"建立管理員帳號失敗：{errors}");
            }

            await userManager.AddToRoleAsync(user, "admin");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
