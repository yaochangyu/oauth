using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

/// <summary>
/// MVC Client OIDC 登入 Fixture，驗證不會發生無限迴圈。
/// </summary>
public sealed class MvcClientFixture : IAsyncLifetime
{
    private static string MvcClientBase  => TestSettings.MvcClientBase;
    private static string AdminUserName  => TestSettings.AdminUserName;
    private static string AdminPassword  => TestSettings.AdminPassword;

    private IPlaywright? _playwright;
    private IBrowser?    _browser;

    public IBrowserContext Context { get; private set; } = null!;
    public IPage           Page    { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });

        Context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            BaseURL           = MvcClientBase,
        });

        Page = await Context.NewPageAsync();
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        // 直接進入需要登入的頁面，觸發 OIDC 流程
        await Page.GotoAsync($"{MvcClientBase}/Profile");

        // 等待 AuthServer 登入表單
        await Page.WaitForSelectorAsync("input[name='userName']", new PageWaitForSelectorOptions { Timeout = 20_000 });

        await Page.FillAsync("input[name='userName']", AdminUserName);
        await Page.FillAsync("input[type='password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");

        // 等待回到 MVC Client Profile 頁面
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
