using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.AuthServer.WebUI.E2E._05_RealWorldScenarios;

[Binding]
public class 跨模組真實場景Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [When(@"使用者前往註冊頁")]
    public async Task When使用者前往註冊頁()
    {
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/register");
        await Page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 15_000 });
    }

    [When(@"使用者填寫隨機會員註冊表單密碼 ""(.*)""")]
    public async Task When使用者填寫隨機會員註冊表單密碼(string password)
    {
        var uniqueEmail = $"user_{Guid.NewGuid():N}@example.com";
        ctx["CreatedUserEmail"] = uniqueEmail;
        ctx["CreatedUserPassword"] = password;

        await Page.FillAsync("input[name='email']", uniqueEmail);
        await Page.FillAsync("input[name='password']", password);
        await Page.ClickAsync("button[type='submit']");
    }

    [When(@"使用者以新註冊的帳號登入")]
    public async Task When使用者以新註冊的帳號登入()
    {
        var email = (string)ctx["CreatedUserEmail"];
        var password = (string)ctx["CreatedUserPassword"];
        var page = Page;

        await page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
        await page.FillAsync("input[name='userName']", email);
        await page.FillAsync("input[type='password']", password);

        var navigationTask = page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20_000 });
        await page.ClickAsync("button[type='submit']");

        try { await navigationTask; }
        catch (TimeoutException) { }

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 20_000 });
    }
}
