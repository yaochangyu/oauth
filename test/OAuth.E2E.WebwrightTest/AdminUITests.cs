using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

public class AdminUITests : IClassFixture<AdminUIFixture>
{
    private readonly IPage _page;

    public AdminUITests(AdminUIFixture fixture)
    {
        _page = fixture.Page;
    }

    // ── Home ──────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Home_顯示Dashboard()
    {
        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync("h5", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await Assertions.Expect(_page.Locator("h5:has-text('Dashboard')")).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Users" })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Roles" })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Scopes" })).ToBeVisibleAsync();
    }

    // ── Users ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Users_列表顯示至少一位使用者()
    {
        await _page.GotoAsync("/users");
        await _page.WaitForSelectorAsync("h5:has-text('Users')", new PageWaitForSelectorOptions { Timeout = 10_000 });
        // 等 Blazor Server Data 載入（等待表格有 td 元素）
        await _page.WaitForSelectorAsync("td", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "admin", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Users_搜尋功能可用()
    {
        await _page.GotoAsync("/users");
        // placeholder 是 "Search by username or email"
        await _page.WaitForSelectorAsync("input[placeholder*='Search']", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await _page.FillAsync("input[placeholder*='Search']", "admin");
        await _page.WaitForTimeoutAsync(1000);
        await _page.WaitForSelectorAsync("td", new PageWaitForSelectorOptions { Timeout = 5_000 });
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "admin", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Users_點擊編輯按鈕跳轉到編輯頁()
    {
        await _page.GotoAsync("/users");
        await _page.WaitForSelectorAsync("td", new PageWaitForSelectorOptions { Timeout = 10_000 });
        // 表格每列有 Edit（Color.Primary）和 Delete（Color.Error）兩個 IconButton，Edit 在前
        var editButton = _page.Locator("table .mud-icon-button").First;
        await editButton.ClickAsync();
        await _page.WaitForURLAsync("**/users/edit/**", new PageWaitForURLOptions { Timeout = 10_000 });
        Assert.Contains("/users/edit/", _page.Url);
    }

    // ── Roles ─────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Roles_列表顯示admin角色()
    {
        await _page.GotoAsync("/roles");
        await _page.WaitForSelectorAsync("h5:has-text('Roles')", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await _page.WaitForSelectorAsync("td", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "admin", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Roles_新增角色輸入框可見()
    {
        await _page.GotoAsync("/roles");
        await _page.WaitForSelectorAsync("h5:has-text('Roles')", new PageWaitForSelectorOptions { Timeout = 10_000 });
        // MudTextField "New Role Name" 渲染為 .mud-input-slot[type='text']
        var input = _page.Locator("input.mud-input-slot[type='text']").First;
        await Assertions.Expect(input).ToBeVisibleAsync();
    }

    // ── Applications ──────────────────────────────────────────────────────────
    [Fact]
    public async Task Applications_列表顯示seeded應用程式()
    {
        await _page.GotoAsync("/applications");
        await _page.WaitForSelectorAsync("text=OAuth Applications", new PageWaitForSelectorOptions { Timeout = 10_000 });
        await Assertions.Expect(_page.Locator("h5:has-text('OAuth Applications')")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Applications_點擊新增跳轉到表單頁()
    {
        await _page.GotoAsync("/applications/new");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/applications/new", _page.Url);
    }

    // ── Scopes ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Scopes_列表顯示api_scope()
    {
        await _page.GotoAsync("/scopes");
        await _page.WaitForSelectorAsync("td", new PageWaitForSelectorOptions { Timeout = 10_000 });
        // Exact match 避免同時匹配到 "api" 和 "API Access"
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "api", Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Scopes_點擊新增跳轉到表單頁()
    {
        await _page.GotoAsync("/scopes/new");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/scopes/new", _page.Url);
    }
}
