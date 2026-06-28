using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.Clients.PlaywrightTest;

[Binding]
public class AdminUI管理介面Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [Given(@"已登入 Admin UI 管理介面")]
    public async Task Given已登入AdminUI管理介面()
    {
        await Page.GotoAsync(TestSettings.AdminUIBase);
        await Page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 20_000 });
        await Page.FillAsync("input[name='userName']", TestSettings.AdminUserName);
        await Page.FillAsync("input[type='password']", TestSettings.AdminPassword);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForSelectorAsync("text=Dashboard", new() { Timeout = 30_000 });
    }

    [When(@"開啟 Admin UI 首頁")]
    public async Task When開啟AdminUI首頁()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/");
        await Page.WaitForSelectorAsync("h5", new() { Timeout = 10_000 });
    }

    [Then(@"應顯示 Dashboard 標題")]
    public async Task Then應顯示Dashboard標題()
    {
        await Assertions.Expect(Page.Locator("h5:has-text('Dashboard')")).ToBeVisibleAsync();
    }

    [Then(@"應顯示 Users、Roles、Scopes 導覽項目")]
    public async Task Then應顯示導覽項目()
    {
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Users" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Roles" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Scopes" })).ToBeVisibleAsync();
    }

    [When(@"開啟 Users 管理頁面")]
    public async Task When開啟Users管理頁面()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/users");
        await Page.WaitForSelectorAsync("h5:has-text('Users')", new() { Timeout = 10_000 });
        await Page.WaitForSelectorAsync("td", new() { Timeout = 10_000 });
    }

    [Then(@"應顯示至少一位使用者")]
    public async Task Then應顯示至少一位使用者()
    {
        Assert.True(await Page.GetByRole(AriaRole.Cell).CountAsync() > 0);
    }

    [Then(@"使用者列表應包含 ""(.*)"" 帳號")]
    public async Task Then使用者列表應包含帳號(string username)
    {
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = username, Exact = true })
        ).ToBeVisibleAsync();
    }

    [When(@"搜尋使用者名稱 ""(.*)""")]
    public async Task When搜尋使用者名稱(string keyword)
    {
        await Page.FillAsync("input[placeholder*='Search']", keyword);
        await Page.WaitForTimeoutAsync(1_000);
        await Page.WaitForSelectorAsync("td", new() { Timeout = 5_000 });
    }

    [When(@"點擊第一筆使用者的編輯按鈕")]
    public async Task When點擊第一筆使用者的編輯按鈕()
    {
        await Page.Locator("table .mud-icon-button").First.ClickAsync();
    }

    [Then(@"頁面應跳轉至使用者編輯頁")]
    public async Task Then頁面應跳轉至使用者編輯頁()
    {
        await Page.WaitForURLAsync("**/users/edit/**", new() { Timeout = 10_000 });
        Assert.Contains("/users/edit/", Page.Url);
    }

    [When(@"開啟 Roles 管理頁面")]
    public async Task When開啟Roles管理頁面()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/roles");
        await Page.WaitForSelectorAsync("h5:has-text('Roles')", new() { Timeout = 10_000 });
        await Page.WaitForSelectorAsync("td", new() { Timeout = 10_000 });
    }

    [Then(@"角色列表應包含 ""(.*)"" 角色")]
    public async Task Then角色列表應包含角色(string roleName)
    {
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = roleName, Exact = true })
        ).ToBeVisibleAsync();
    }

    [When(@"開啟 Applications 管理頁面")]
    public async Task When開啟Applications管理頁面()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/applications");
        await Page.WaitForSelectorAsync("text=OAuth Applications", new() { Timeout = 10_000 });
    }

    [Then(@"應顯示 OAuth Applications 標題")]
    public async Task Then應顯示OAuthApplications標題()
    {
        await Assertions.Expect(Page.Locator("h5:has-text('OAuth Applications')")).ToBeVisibleAsync();
    }

    [When(@"開啟 Scopes 管理頁面")]
    public async Task When開啟Scopes管理頁面()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/scopes");
        await Page.WaitForSelectorAsync("td", new() { Timeout = 10_000 });
    }

    [Then(@"Scope 列表應包含 ""(.*)"" scope")]
    public async Task ThenScope列表應包含Scope(string scopeName)
    {
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = scopeName, Exact = true })
        ).ToBeVisibleAsync();
    }
}
