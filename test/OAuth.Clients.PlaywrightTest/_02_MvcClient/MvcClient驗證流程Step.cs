using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.Clients.PlaywrightTest;

[Binding]
public class MvcClient驗證流程Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [When(@"使用者前往 MVC Client 個人資料頁")]
    public async Task When使用者前往MvcClient個人資料頁()
    {
        await Page.GotoAsync($"{TestSettings.MvcClientBase}/Profile");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 15_000 });
    }

    [Then(@"應停在 MVC Client 頁面")]
    public async Task Then應停在MvcClient頁面()
    {
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        Assert.Contains("5101", Page.Url);
    }

    [Then(@"URL 不應包含 AuthServer 位址")]
    public void Then_URL_不應包含AuthServer位址()
    {
        Assert.DoesNotContain("7001", Page.Url);
    }

    [Then(@"個人資料頁應顯示歡迎文字")]
    public async Task Then個人資料頁應顯示歡迎文字()
    {
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        await Assertions.Expect(Page.GetByText("歡迎")).ToBeVisibleAsync();
    }

    [Then(@"個人資料頁應包含 ""(.*)"" 及 ""(.*)"" claim")]
    public async Task Then個人資料頁應包含Claim(string claimType, string claimValue)
    {
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
        var mainText = await Page.InnerTextAsync("main");
        Assert.Contains(claimType,  mainText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(claimValue, mainText, StringComparison.OrdinalIgnoreCase);
    }

    [Then(@"個人資料頁應顯示 Access Token 有值")]
    public async Task Then個人資料頁應顯示AccessToken有值()
    {
        await Page.WaitForSelectorAsync("p:has-text('Access Token')", new() { Timeout = 10_000 });
        await Assertions.Expect(Page.GetByText("Access Token 有值：True")).ToBeVisibleAsync();
    }
}
