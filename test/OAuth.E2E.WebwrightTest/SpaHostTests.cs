using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

/// <summary>
/// 驗證 Vue SPA（SpaHost）的 OIDC 流程：
/// 1. 登入後不發生無限迴圈
/// 2. Profile 顯示 user 資訊與 role claim（集中管理生效）
/// </summary>
public class SpaHostTests : IClassFixture<SpaHostFixture>
{
    private readonly IPage _page;

    public SpaHostTests(SpaHostFixture fixture)
    {
        _page = fixture.Page;
    }

    [Fact]
    public async Task Login_成功後無OIDC迴圈_停在SpaHost()
    {
        // Fixture 已完成登入，驗證 URL 在 SpaHost (5200)，非 AuthServer (7001)
        await _page.GotoAsync("/profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        Assert.Contains("5200", _page.Url);
        Assert.DoesNotContain("7001", _page.Url);
    }

    [Fact]
    public async Task Home_登入後顯示已登入狀態()
    {
        await _page.GotoAsync("/");
        // Vue SPA 首頁：已登入時顯示「已登入為 {name}」
        await _page.WaitForSelectorAsync("text=已登入", new() { Timeout = 10_000 });
        await Assertions.Expect(_page.GetByText("已登入")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Profile_顯示Email_和名稱()
    {
        await _page.GotoAsync("/profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        // ProfileView 顯示 email — 用 First 避免 strict mode（email 欄位 + claims 表格共有多個元素）
        await Assertions.Expect(_page.GetByText("admin@localhost").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Profile_AccessToken_有效()
    {
        await _page.GotoAsync("/profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        // tag-green span 顯示「有效（N 分鐘）」
        await Assertions.Expect(_page.Locator("span.tag-green").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Profile_Claims表格包含role_claim()
    {
        await _page.GotoAsync("/profile");
        await _page.WaitForSelectorAsync("table", new() { Timeout = 10_000 });
        // Claims 表格的 Type 欄位有 "role"，Value 欄位有 "admin"
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "role", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Cell, new() { Name = "admin", Exact = true })).ToBeVisibleAsync();
    }
}
