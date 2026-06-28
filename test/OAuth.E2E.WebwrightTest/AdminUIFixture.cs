using Microsoft.Playwright;

namespace OAuth.E2E.WebwrightTest;

/// <summary>
/// 共用的 Admin UI 測試 Fixture，處理 OIDC 登入流程並複用瀏覽器 context。
/// </summary>
public sealed class AdminUIFixture : IAsyncLifetime
{
    private const string AdminUIBase   = "https://localhost:7002";
    private const string AdminUserName = "admin";
    private const string AdminPassword = "Admin@123456";

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
            BaseURL           = AdminUIBase,
        });

        Page = await Context.NewPageAsync();
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        await Page.GotoAsync(AdminUIBase);

        // 等待 AuthServer 登入表單出現（URL 是 /Account/Login，大小寫敏感）
        await Page.WaitForSelectorAsync("input[name='userName']", new PageWaitForSelectorOptions { Timeout = 20_000 });

        // 確認不是已登入狀態
        if (!Page.Url.Contains(AdminUIBase))
        {
            await Page.FillAsync("input[name='userName']", AdminUserName);
            await Page.FillAsync("input[type='password']", AdminPassword);
            await Page.ClickAsync("button[type='submit']");

            // 等待登入後 Admin UI 頁面的 Dashboard 內容出現
            await Page.WaitForSelectorAsync("text=Dashboard", new PageWaitForSelectorOptions { Timeout = 30_000 });
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }
}
