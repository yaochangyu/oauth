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

    // ── Users ─────────────────────────────────────────────────────────────────

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

    [Then(@"使用者列表應為空")]
    public async Task Then使用者列表應為空()
    {
        await Assertions.Expect(
            Page.Locator("tbody tr:not(.mud-table-empty-row):not(.mud-table-loading-progress-row)")
        ).ToHaveCountAsync(0, new() { Timeout = 5_000 });
    }

    [When(@"搜尋使用者名稱 ""(.*)""")]
    public async Task When搜尋使用者名稱(string keyword)
    {
        await Page.FillAsync("input[placeholder*='Search']", keyword);
        await Page.PressAsync("input[placeholder*='Search']", "Enter");
        await Page.WaitForTimeoutAsync(1_200);
    }

    [When(@"點擊第一筆使用者的編輯按鈕")]
    public async Task When點擊第一筆使用者的編輯按鈕()
    {
        await Page.Locator("table .mud-icon-button").First.ClickAsync();
    }

    [Then(@"頁面應跳轉至使用者編輯頁")]
    public async Task Then頁面應跳轉至使用者編輯頁()
    {
        // Blazor Server 以 SignalR 更新 URL（無 full page load），改用 ToHaveURLAsync 輪詢
        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/users/edit/"),
            new PageAssertionsToHaveURLOptions { Timeout = 10_000 });
        Assert.Contains("/users/edit/", Page.Url);
    }

    [Then(@"編輯頁應顯示使用者名稱 ""(.*)""")]
    public async Task Then編輯頁應顯示使用者名稱(string username)
    {
        await Page.WaitForSelectorAsync("input:not([type='hidden'])", new() { Timeout = 10_000 });
        var value = await Page.GetByLabel("Username").InputValueAsync();
        Assert.Equal(username, value);
    }

    [Then(@"編輯頁 ""(.*)"" 角色核取方塊應為勾選")]
    public async Task Then編輯頁角色核取方塊應為勾選(string roleName)
    {
        var checkbox = Page.Locator(".mud-checkbox")
            .Filter(new LocatorFilterOptions { HasText = roleName })
            .Locator("input[type='checkbox']");
        await Assertions.Expect(checkbox).ToBeCheckedAsync(new() { Timeout = 10_000 });
    }

    // ── Roles ─────────────────────────────────────────────────────────────────

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

    [Then(@"角色列表應不包含 ""(.*)"" 角色")]
    public async Task Then角色列表應不包含角色(string roleName)
    {
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = roleName, Exact = true })
        ).Not.ToBeVisibleAsync();
    }

    [When(@"新增角色 ""(.*)""")]
    public async Task When新增角色(string roleName)
    {
        await Page.GetByLabel("New Role Name").FillAsync(roleName);
        await Page.ClickAsync("button:has-text('Add Role')");
        await Page.WaitForTimeoutAsync(1_000);
        await Page.WaitForSelectorAsync("td", new() { Timeout = 5_000 });
    }

    [When(@"刪除角色 ""(.*)""")]
    public async Task When刪除角色(string roleName)
    {
        var row = Page.Locator($"tr:has(td:has-text('{roleName}'))");
        await row.Locator(".mud-icon-button").ClickAsync();
        await Page.WaitForSelectorAsync(".mud-message-box", new() { Timeout = 5_000 });
        await Page.Locator(".mud-message-box").GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await Page.WaitForTimeoutAsync(1_000);
    }

    [Then(@"應顯示操作失敗的錯誤訊息")]
    public async Task Then應顯示操作失敗的錯誤訊息()
    {
        await Assertions.Expect(
            Page.Locator(".mud-snackbar.mud-alert-filled-error")
        ).ToBeVisibleAsync(new() { Timeout = 5_000 });
    }

    // ── Applications ──────────────────────────────────────────────────────────

    [When(@"開啟 Applications 管理頁面")]
    public async Task When開啟Applications管理頁面()
    {
        await Page.GotoAsync($"{TestSettings.AdminUIBase}/applications");
        await Page.WaitForSelectorAsync("text=OAuth Applications", new() { Timeout = 10_000 });
        await Page.WaitForSelectorAsync("td", new() { Timeout = 10_000 });
    }

    [Then(@"應顯示 OAuth Applications 標題")]
    public async Task Then應顯示OAuthApplications標題()
    {
        await Assertions.Expect(Page.Locator("h5:has-text('OAuth Applications')")).ToBeVisibleAsync();
    }

    [Then(@"Application 列表應包含 ""(.*)""")]
    public async Task ThenApplication列表應包含(string clientId)
    {
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = clientId, Exact = true })
        ).ToBeVisibleAsync();
    }

    [When(@"點擊 ""(.*)"" 的 Application 編輯按鈕")]
    public async Task When點擊Application編輯按鈕(string clientId)
    {
        var row = Page.Locator($"tr:has(td:has-text('{clientId}'))");
        await row.Locator(".mud-icon-button").First.ClickAsync();
    }

    [Then(@"頁面應跳轉至 Application 編輯頁")]
    public async Task Then頁面應跳轉至Application編輯頁()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/applications/edit/"),
            new PageAssertionsToHaveURLOptions { Timeout = 10_000 });
    }

    [Then(@"編輯頁應顯示 Client Id ""(.*)""")]
    public async Task Then編輯頁應顯示ClientId(string clientId)
    {
        await Page.WaitForSelectorAsync("input:not([type='hidden'])", new() { Timeout = 10_000 });
        var value = await Page.GetByLabel("Client Id").InputValueAsync();
        Assert.Equal(clientId, value);
    }

    [When(@"搜尋 Application ""(.*)""")]
    public async Task When搜尋Application(string keyword)
    {
        await Page.FillAsync("input[placeholder*='Search']", keyword);
        await Page.PressAsync("input[placeholder*='Search']", "Enter");
        await Page.WaitForTimeoutAsync(2_000);
    }

    [Then(@"Application 列表應為空")]
    public async Task ThenApplication列表應為空()
    {
        await Assertions.Expect(
            Page.Locator("tbody tr:not(.mud-table-empty-row):not(.mud-table-loading-progress-row)")
        ).ToHaveCountAsync(0, new() { Timeout = 5_000 });
    }

    // ── Scopes ────────────────────────────────────────────────────────────────

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

    [When(@"點擊 ""(.*)"" Scope 的編輯按鈕")]
    public async Task When點擊Scope編輯按鈕(string scopeName)
    {
        var row = Page.Locator($"tr:has(td:has-text('{scopeName}'))");
        await row.Locator(".mud-icon-button").First.ClickAsync();
    }

    [Then(@"頁面應跳轉至 Scope 編輯頁")]
    public async Task Then頁面應跳轉至Scope編輯頁()
    {
        await Assertions.Expect(Page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex("/scopes/edit/"),
            new PageAssertionsToHaveURLOptions { Timeout = 10_000 });
    }

    [Then(@"編輯頁應顯示 Scope Name ""(.*)""")]
    public async Task Then編輯頁應顯示ScopeName(string scopeName)
    {
        await Page.WaitForSelectorAsync("input:not([type='hidden'])", new() { Timeout = 10_000 });
        var value = await Page.GetByLabel("Name", new() { Exact = true }).InputValueAsync();
        Assert.Equal(scopeName, value);
    }
}
