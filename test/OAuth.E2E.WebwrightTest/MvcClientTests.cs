using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

/// <summary>
/// 驗證 MVC Client 的 OIDC 流程：
/// 1. 登入後不發生無限迴圈
/// 2. JWT 中包含 admin role claim（集中管理生效）
/// </summary>
public class MvcClientTests : IClassFixture<MvcClientFixture>
{
    private readonly IPage _page;

    public MvcClientTests(MvcClientFixture fixture)
    {
        _page = fixture.Page;
    }

    [Fact]
    public async Task Login_成功後無OIDC迴圈_停在MvcClient()
    {
        // Fixture 已完成登入，驗證目前 URL 在 MVC Client（5101），而非 AuthServer（7001）
        await _page.GotoAsync("/Profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        Assert.Contains("5101", _page.Url);
        Assert.DoesNotContain("7001", _page.Url);
    }

    [Fact]
    public async Task Profile_顯示使用者名稱()
    {
        await _page.GotoAsync("/Profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        // 頁面顯示 "歡迎，{Name}"
        await Assertions.Expect(_page.GetByText("歡迎")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Profile_JWT包含role_admin_claim()
    {
        await _page.GotoAsync("/Profile");
        await _page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        // Profile 頁面的 main 區塊列出所有 claims；roles scope → AuthServer 嵌入 role claim
        var mainText = await _page.InnerTextAsync("main");
        Assert.Contains("role", mainText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("admin", mainText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Profile_AccessToken_有值()
    {
        await _page.GotoAsync("/Profile");
        await _page.WaitForSelectorAsync("p:has-text('Access Token')", new() { Timeout = 10_000 });
        await Assertions.Expect(_page.GetByText("Access Token 有值：True")).ToBeVisibleAsync();
    }
}
