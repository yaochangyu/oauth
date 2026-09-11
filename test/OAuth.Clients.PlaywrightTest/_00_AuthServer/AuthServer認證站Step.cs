using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.Clients.PlaywrightTest;

[Binding]
public class AuthServer認證站Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [When(@"使用者前往 AuthServer 登入頁")]
    public async Task When使用者前往AuthServer登入頁()
    {
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/login");
        await Page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
    }

    [When(@"使用者前往帶有非法 returnUrl 的 AuthServer 登入頁")]
    public async Task When使用者前往帶有非法ReturnUrl的AuthServer登入頁()
    {
        var illegalReturnUrl = Uri.EscapeDataString("https://evil.example.com/phishing");
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/login?returnUrl={illegalReturnUrl}");
        await Page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
    }

    [Then(@"登入頁應顯示錯誤訊息")]
    public async Task Then登入頁應顯示錯誤訊息()
    {
        await Assertions.Expect(Page.Locator("[data-testid='error-message']")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Then(@"頁面不應跳轉離開登入頁")]
    public void Then頁面不應跳轉離開登入頁()
    {
        Assert.Contains("/login", Page.Url);
    }

    [Then(@"同意頁應顯示應用程式名稱 ""(.*)""")]
    public async Task Then同意頁應顯示應用程式名稱(string clientDisplayName)
    {
        await Assertions.Expect(Page.GetByText(clientDisplayName)).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [When(@"使用者前往 AuthServer 註冊頁")]
    public async Task When使用者前往AuthServer註冊頁()
    {
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/register");
        await Page.WaitForSelectorAsync("input[name='email']", new() { Timeout = 15_000 });
    }

    [When(@"使用者填寫註冊表單並送出")]
    public async Task When使用者填寫註冊表單並送出()
    {
        var uniqueEmail = $"e2e-{Guid.NewGuid():N}@example.com";
        await Page.FillAsync("input[name='email']", uniqueEmail);
        await Page.FillAsync("input[name='password']", "Test1234");
        await Page.ClickAsync("button[type='submit']");
    }

    [Then(@"應顯示註冊成功訊息")]
    public async Task Then應顯示註冊成功訊息()
    {
        await Assertions.Expect(Page.Locator("[data-testid='success-message']")).ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [When(@"使用者前往 AuthServer 錯誤頁 ""(.*)""")]
    public async Task When使用者前往AuthServer錯誤頁(string errorCode)
    {
        await Page.GotoAsync($"{TestSettings.AuthServerBase}/error?code={errorCode}");
        await Page.WaitForSelectorAsync("[data-testid='error-code']", new() { Timeout = 15_000 });
    }

    [Then(@"Error 頁面應顯示錯誤代碼 ""(.*)""")]
    public async Task ThenError頁面應顯示錯誤代碼(string errorCode)
    {
        await Assertions.Expect(Page.Locator("[data-testid='error-code']")).ToHaveTextAsync(errorCode, new() { Timeout = 10_000 });
    }
}
