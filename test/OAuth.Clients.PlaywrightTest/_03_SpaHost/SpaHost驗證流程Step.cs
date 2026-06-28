using Microsoft.Playwright;
using Reqnroll;

namespace OAuth.Clients.PlaywrightTest;

[Binding]
public class SpaHost驗證流程Step(ScenarioContext ctx)
{
    private IPage Page => (IPage)ctx["page"];

    [When(@"使用者前往 SPA Host 首頁")]
    public async Task When使用者前往SpaHost首頁()
    {
        await Page.GotoAsync(TestSettings.SpaHostBase);
        await Page.WaitForSelectorAsync("button:has-text('登入')", new() { Timeout = 15_000 });
    }

    [When(@"使用者點擊 SPA 登入按鈕")]
    public async Task When使用者點擊SPA登入按鈕()
    {
        await Page.ClickAsync("button:has-text('登入')");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 10_000 });
    }

    [Then(@"應停在 SPA Host 頁面")]
    public async Task Then應停在SpaHost頁面()
    {
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 30_000 });
        Assert.Contains("5200", Page.Url);
        Assert.DoesNotContain("7001", Page.Url);
    }

    [Then(@"首頁應顯示已登入文字")]
    public async Task Then首頁應顯示已登入文字()
    {
        await Page.WaitForSelectorAsync("text=已登入", new() { Timeout = 30_000 });
        await Assertions.Expect(Page.GetByText("已登入")).ToBeVisibleAsync();
    }

    [When(@"使用者前往 SPA 個人資料頁")]
    public async Task When使用者前往SPA個人資料頁()
    {
        await Page.GotoAsync($"{TestSettings.SpaHostBase}/profile");
        await Page.WaitForSelectorAsync("h1:has-text('個人資料')", new() { Timeout = 10_000 });
    }

    [Then(@"SPA 個人資料頁應顯示 ""(.*)""")]
    public async Task ThenSPA個人資料頁應顯示(string text)
    {
        await Assertions.Expect(Page.GetByText(text).First).ToBeVisibleAsync();
    }

    [Then(@"SPA 個人資料頁應顯示 Access Token 有效標記")]
    public async Task ThenSPA個人資料頁應顯示AccessToken有效標記()
    {
        await Assertions.Expect(Page.Locator("span.tag-green").First).ToBeVisibleAsync();
    }

    [Then(@"SPA Claims 表格應包含 ""(.*)"" 和 ""(.*)""")]
    public async Task ThenSPAClaims表格應包含(string claimType, string claimValue)
    {
        await Page.WaitForSelectorAsync("table", new() { Timeout = 10_000 });
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = claimType, Exact = true })
        ).ToBeVisibleAsync();
        await Assertions.Expect(
            Page.GetByRole(AriaRole.Cell, new() { Name = claimValue, Exact = true })
        ).ToBeVisibleAsync();
    }
}
