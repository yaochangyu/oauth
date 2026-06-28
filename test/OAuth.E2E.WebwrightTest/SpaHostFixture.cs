using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

/// <summary>
/// Vue SPA（SpaHost）OIDC 登入 Fixture。
/// 測試目標：https://localhost:5200（ASP.NET Core 靜態主機，服務 Vue 打包產出）
/// </summary>
public sealed class SpaHostFixture : IAsyncLifetime
{
    private static string SpaBase      => TestSettings.SpaHostBase;
    private static string AdminUser    => TestSettings.AdminUserName;
    private static string AdminPass    => TestSettings.AdminPassword;

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
            BaseURL           = SpaBase,
        });

        Page = await Context.NewPageAsync();
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        // 開啟 Vue SPA 首頁
        await Page.GotoAsync(SpaBase);

        // 等待 Vue 渲染完成：「登入」按鈕出現
        await Page.WaitForSelectorAsync("button:has-text('登入')", new PageWaitForSelectorOptions { Timeout = 15_000 });
        await Page.ClickAsync("button:has-text('登入')");

        // 等待 AuthServer 登入表單
        await Page.WaitForSelectorAsync("input[name='userName']", new PageWaitForSelectorOptions { Timeout = 20_000 });
        await Page.FillAsync("input[name='userName']", AdminUser);
        await Page.FillAsync("input[type='password']", AdminPass);
        await Page.ClickAsync("button[type='submit']");

        // 等待 callback 處理完成後跳轉到 /profile
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new PageWaitForSelectorOptions { Timeout = 30_000 });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
