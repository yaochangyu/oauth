using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;

namespace OAuth.Admin.WebAPI.IntegrationTest._04_RoleManage;

[Binding]
public class 角色管理Step : Steps
{
    [Given(@"資料庫已存在角色 ""(.*)""")]
    public async Task Given資料庫已存在角色(string roleName)
    {
        using var scope = BaseStep.Factory!.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            result.Succeeded.Should().BeTrue();
        }
    }
}
