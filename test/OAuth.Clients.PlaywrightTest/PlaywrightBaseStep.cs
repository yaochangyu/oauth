using DotNet.Testcontainers.Containers;
using Microsoft.Playwright;
using Reqnroll;
using System.Diagnostics;
using Testcontainers.PostgreSql;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OAuth.Clients.PlaywrightTest;

/// <summary>
/// 全域服務生命週期（[BeforeTestRun]/[AfterTestRun]）+ 每個 Scenario 的瀏覽器建立/釋放。
/// 共用 Step（開啟瀏覽器、填寫登入表單）。
///
/// 預設模式（E2E_USE_TESTCONTAINERS 未設定或 false）：
///   假設 dev 環境服務已啟動（task docker-up + 各 task *-dev 已執行）。
///
/// 自動模式（E2E_USE_TESTCONTAINERS=true）：
///   自動啟動 PostgreSQL TestContainer 並以 dotnet run 啟動所有服務。
/// </summary>
[Binding]
public class PlaywrightBaseStep(ScenarioContext ctx)
{
    private static readonly List<IContainer> _containers = [];
    private static readonly List<Process>    _services   = [];
    private static bool _started;

    // ── 全域服務生命週期 ────────────────────────────────────────────────────

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        if (!UseTestContainers) return;

        var repoRoot = FindRepoRoot();

        var pg = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("oauth_e2e")
            .WithUsername("oauth")
            .WithPassword("oauth_pass")
            .Build();
        await pg.StartAsync();
        _containers.Add(pg);
        var connStr = pg.GetConnectionString();

        await RunMigrationsAsync(repoRoot, connStr);

        _services.Add(StartService(repoRoot, "src/AuthServer/OAuth.AuthServer.WebAPI",
            "https://localhost:7001;http://localhost:5265", connStr));
        _services.Add(StartService(repoRoot, "src/Admin/OAuth.AuthServer.Admin.WebUI",
            "https://localhost:7002;http://localhost:5279", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.Mvc",
            "https://localhost:5101;http://localhost:5256", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.WebAPI",
            "https://localhost:5102;http://localhost:5160", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.SpaHost",
            "https://localhost:5200", connStr));

        await WaitForReadyAsync(TestSettings.AuthServerBase, timeoutSeconds: 120);
        await WaitForReadyAsync(TestSettings.AdminUIBase,    timeoutSeconds:  60);
        await WaitForReadyAsync(TestSettings.MvcClientBase,  timeoutSeconds:  30);
        await WaitForReadyAsync(TestSettings.WebApiBase,     timeoutSeconds:  30);
        await WaitForReadyAsync(TestSettings.SpaHostBase,    timeoutSeconds:  30);

        _started = true;
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        if (!_started) return;

        foreach (var p in _services)
        {
            try { p.Kill(entireProcessTree: true); } catch { }
            p.Dispose();
        }
        _services.Clear();

        for (var i = _containers.Count - 1; i >= 0; i--)
            await _containers[i].StopAsync();
        _containers.Clear();
    }

    // ── Scenario 瀏覽器生命週期 ─────────────────────────────────────────────

    [BeforeScenario]
    public async Task BeforeScenario()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
        });
        var page = await context.NewPageAsync();

        ctx["playwright"] = playwright;
        ctx["browser"]    = browser;
        ctx["page"]       = page;
    }

    [AfterScenario]
    public async Task AfterScenario()
    {
        if (ctx.TryGetValue("browser", out var b) && b is IBrowser browser)
            await browser.DisposeAsync();
        if (ctx.TryGetValue("playwright", out var p) && p is IPlaywright playwright)
            playwright.Dispose();
    }

    // ── 共用 Steps ──────────────────────────────────────────────────────────

    [Given(@"開啟全新的瀏覽器視窗")]
    public void Given開啟全新的瀏覽器視窗()
    {
        // BeforeScenario 已建立乾淨 context，此 step 為語意佔位
    }

    /// <summary>填寫 AuthServer 登入表單並送出，等待 DOMContentLoaded。</summary>
    [When(@"使用者輸入帳號 ""(.*)"" 密碼 ""(.*)"" 登入")]
    public async Task When使用者輸入帳號密碼登入(string username, string password)
    {
        var page = Page;
        await page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
        await page.FillAsync("input[name='userName']", username);
        await page.FillAsync("input[type='password']", password);
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 20_000 });
    }

    /// <summary>若登入後跳到同意頁面，自動點擊同意並等待跳轉完成。</summary>
    [When(@"若顯示同意頁面則同意授權")]
    public async Task When若顯示同意頁面則同意授權()
    {
        var page = Page;
        if (!page.Url.Contains("/Connect/Consent")) return;

        var navigationTask = page.WaitForURLAsync($"{TestSettings.MvcClientBase}/**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout   = 20_000,
        });
        await page.ClickAsync("button[value='accept']");
        await navigationTask;
    }

    protected IPage Page => (IPage)ctx["page"];

    // ── 內部工具 ────────────────────────────────────────────────────────────

    private static bool UseTestContainers =>
        string.Equals(Environment.GetEnvironmentVariable("E2E_USE_TESTCONTAINERS"), "true",
            StringComparison.OrdinalIgnoreCase);

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("找不到 Git Repo 根目錄（.git 資料夾不存在於任何父目錄）");
    }

    private static async Task RunMigrationsAsync(string repoRoot, string connStr)
    {
        var authServerPath = Path.Combine(repoRoot, "src", "AuthServer", "OAuth.AuthServer.WebAPI");
        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments        = $"ef database update --project \"{authServerPath}\"",
            WorkingDirectory = repoRoot,
            UseShellExecute  = false,
        };
        psi.Environment["ConnectionStrings__DefaultConnection"] = connStr;

        var p = Process.Start(psi)
            ?? throw new InvalidOperationException("dotnet ef 工具啟動失敗（請確認 dotnet-ef 已安裝：dotnet tool install -g dotnet-ef）");
        await p.WaitForExitAsync();

        if (p.ExitCode != 0)
            throw new InvalidOperationException($"DB Migration 失敗（exit code: {p.ExitCode}）");
    }

    private static Process StartService(string repoRoot, string relPath, string urls, string connStr)
    {
        var projectPath = Path.Combine(repoRoot, relPath);
        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments        = $"run --project \"{projectPath}\"",
            WorkingDirectory = repoRoot,
            UseShellExecute  = false,
        };
        psi.Environment["ASPNETCORE_URLS"]                     = urls;
        psi.Environment["ASPNETCORE_ENVIRONMENT"]              = "Development";
        psi.Environment["ConnectionStrings__DefaultConnection"] = connStr;

        return Process.Start(psi)
            ?? throw new InvalidOperationException($"服務啟動失敗: {relPath}");
    }

    private static async Task WaitForReadyAsync(string baseUrl, int timeoutSeconds)
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = await client.GetAsync(baseUrl);
                if ((int)resp.StatusCode is >= 200 and < 500) return;
            }
            catch { }
            await Task.Delay(1_000);
        }
        throw new TimeoutException($"服務 {baseUrl} 在 {timeoutSeconds}s 內未就緒");
    }
}
