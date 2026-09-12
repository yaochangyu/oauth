using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OAuth.AuthServer.DB;
using Reqnroll;

namespace OAuth.Admin.WebAPI.IntegrationTest._02_UserManage;

[Binding]
public class 使用者狀態管理Step : Steps
{
    [Given(@"資料庫已存在一般用戶 ""(.*)"" 帳號正常")]
    public async Task Given資料庫已存在一般用戶帳號正常(string username)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }

        user = new ApplicationUser
        {
            Id = username,
            UserName = username,
            Email = $"{username}@example.com",
            EmailConfirmed = true,
            LockoutEnabled = true,
            LockoutEnd = null,
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await userManager.CreateAsync(user, "User@123456");
        result.Succeeded.Should().BeTrue();
        this.ScenarioContext[$"SecurityStamp_{username}"] = user.SecurityStamp;
    }

    [Given(@"資料庫已存在被凍結用戶 ""(.*)""")]
    public async Task Given資料庫已存在被凍結用戶(string username)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            await userManager.DeleteAsync(user);
        }

        user = new ApplicationUser
        {
            Id = username,
            UserName = username,
            Email = $"{username}@example.com",
            EmailConfirmed = true,
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddYears(100),
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var result = await userManager.CreateAsync(user, "User@123456");
        result.Succeeded.Should().BeTrue();
    }

    [Then(@"用戶 ""(.*)"" 的安全戳記已被更新")]
    public async Task Then用戶的安全戳記已被更新(string username)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(username);
        user.Should().NotBeNull();

        var oldStamp = (string)this.ScenarioContext[$"SecurityStamp_{username}"];
        user!.SecurityStamp.Should().NotBe(oldStamp, "強制登出所有工作階段應更新 SecurityStamp");
    }
}
