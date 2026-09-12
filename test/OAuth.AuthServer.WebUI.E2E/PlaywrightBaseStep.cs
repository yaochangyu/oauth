using DotNet.Testcontainers.Containers;
using Microsoft.Playwright;
using Reqnroll;
using System.Diagnostics;
using Testcontainers.PostgreSql;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace OAuth.AuthServer.WebUI.E2E;

/// <summary>
/// 全域服務生命週期（[BeforeTestRun]/[AfterTestRun]）+ 每個 Scenario 的瀏覽器建立/釋放。
/// 共用 Step（開啟瀏覽器、填寫登入表單）。
///
/// 預設模式（E2E_USE_TESTCONTAINERS 未設定或 true）：
///   自動啟動 PostgreSQL TestContainer 並以 dotnet run 啟動所有服務，測試結束後清除，
///   確保 E2E 測試不會污染或依賴開發環境的真實資料庫。
///
/// 手動模式（E2E_USE_TESTCONTAINERS=false）：
///   假設 dev 環境服務已啟動（task docker-up + 各 task *-dev 已執行），供本機快速迭代測試用。
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

        // 整個測試執行過程中，多個情境會透過同一個 loopback IP 反覆呼叫登入端點，
        // 為避免觸發正式環境用的「每 IP 每分鐘 5 次」限流（進而讓 E2E 測試產生偽陽性失敗），
        // 僅在此 E2E 測試進程啟動的 AuthServer 放寬門檻；正式/一般開發環境不受影響。
        _services.Add(StartService(repoRoot, "src/AuthServer/OAuth.AuthServer.WebAPI",
            "https://localhost:7001;http://localhost:5265", connStr,
            extraEnv: new Dictionary<string, string>
            {
                ["RateLimiting__Login__PermitLimit"] = "1000",
                ["RateLimiting__Login__WindowSeconds"] = "60",
            }));
        _services.Add(StartService(repoRoot, "src/Admin/OAuth.Admin.WebAPI",
            "https://localhost:7002;http://localhost:5279", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.Mvc",
            "https://localhost:5101;http://localhost:5256", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.WebAPI",
            "https://localhost:5102;http://localhost:5160", connStr));
        _services.Add(StartService(repoRoot, "src/Clients/OAuth.Client.SpaHost",
            "https://localhost:5200", connStr));

        try
        {
            await WaitForReadyAsync(TestSettings.AuthServerBase, timeoutSeconds: 120);
            await WaitForReadyAsync(TestSettings.AdminUIBase,    timeoutSeconds:  60);
            await WaitForReadyAsync(TestSettings.MvcClientBase,  timeoutSeconds:  30);
            await WaitForReadyAsync(TestSettings.WebApiBase,     timeoutSeconds:  30);
            await WaitForReadyAsync(TestSettings.SpaHostBase,    timeoutSeconds:  30);

            _started = true;
        }
        catch
        {
            _started = true;
            await AfterTestRun();
            throw;
        }
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
    public Task BeforeScenario() => Task.CompletedTask;

    [AfterScenario]
    public async Task AfterScenario()
    {
        if (ctx.TryGetValue("browser", out var b) && b is IBrowser browser)
            await browser.DisposeAsync();
        if (ctx.TryGetValue("playwright", out var p) && p is IPlaywright playwright)
            playwright.Dispose();
    }

    // ── 共用 Steps ──────────────────────────────────────────────────────────

    [Given(@"初始化 Auth 伺服器")]
    public async Task Given初始化Auth伺服器()
    {
        await EnsureServiceReadyAsync(TestSettings.AuthServerBase);
    }

    [Given(@"初始化 MVC Client 測試環境")]
    public async Task Given初始化MvcClient測試環境()
    {
        await EnsureServiceReadyAsync(TestSettings.MvcClientBase);
    }

    [Given(@"初始化 SPA Host 測試環境")]
    public async Task Given初始化SpaHost測試環境()
    {
        await EnsureServiceReadyAsync(TestSettings.SpaHostBase);
    }

    [Given(@"初始化 Admin UI 測試環境")]
    public async Task Given初始化AdminUI測試環境()
    {
        await EnsureServiceReadyAsync(TestSettings.AdminUIBase);
    }

    [Given(@"開啟全新的瀏覽器視窗")]
    public async Task Given開啟全新的瀏覽器視窗()
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

    /// <summary>
    /// 填寫 AuthServer（Vue 3 Headless 認證站）登入表單並送出。
    /// 登入表單以 fetch 呼叫 API、成功後才用 window.location.href 做客戶端導頁（非傳統 form POST），
    /// 屬非同步流程，click 完成當下瀏覽器可能尚未開始導頁，因此不能只做一次性的
    /// WaitForLoadState 檢查（會在導頁真正發生前就提早返回）。改為主動等待 URL 離開 /login。
    /// </summary>
    [When(@"使用者輸入帳號 ""(.*)"" 密碼 ""(.*)"" 登入")]
    public async Task When使用者輸入帳號密碼登入(string username, string password)
    {
        var page = Page;
        await page.WaitForSelectorAsync("input[name='userName']", new() { Timeout = 15_000 });
        await page.FillAsync("input[name='userName']", username);
        await page.FillAsync("input[type='password']", password);

        var navigationTask = page.WaitForURLAsync(
            url => !url.Contains("/login", StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20_000 });
        await page.ClickAsync("button[type='submit']");

        try { await navigationTask; }
        catch (TimeoutException) { /* 登入失敗等情境會停留在 /login，交由呼叫端自行斷言 */ }

        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded, new() { Timeout = 20_000 });
    }

    /// <summary>若登入後跳到同意頁面，自動點擊同意並等待跳轉完成。</summary>
    [When(@"若顯示同意頁面則同意授權")]
    public async Task When若顯示同意頁面則同意授權()
    {
        var page = Page;
        if (!page.Url.Contains("/consent", StringComparison.OrdinalIgnoreCase)) return;

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
        !string.Equals(Environment.GetEnvironmentVariable("E2E_USE_TESTCONTAINERS"), "false",
            StringComparison.OrdinalIgnoreCase);

    private static string FindRepoRoot()
    {
        // .git 在一般 clone 是資料夾，但在 git worktree（例如本 Agent 的隔離工作目錄）是指向
        // 主 repo `.git/worktrees/<name>` 的檔案。只檢查 Directory.Exists 會在 worktree 內跳過
        // worktree 自己的根目錄，誤判到父層真正的主 checkout，導致背景服務跑的是「別的」程式碼。
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var gitPath = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath)) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("找不到 Git Repo 根目錄（.git 資料夾/檔案不存在於任何父目錄）");
    }

    private static async Task RunMigrationsAsync(string repoRoot, string connStr)
    {
        // Migrations 定義在 OAuth.AuthServer.DB，--project 需指向該專案；
        // --startup-project 才是 WebAPI（提供 DbContext 的執行期 DI/組態）。
        // 過去只傳 --project=WebAPI 會導致 dotnet ef 在 WebAPI 組件裡找不到 DbContext 而失敗。
        var dbProjectPath = Path.Combine(repoRoot, "src", "AuthServer", "OAuth.AuthServer.DB");
        var authServerPath = Path.Combine(repoRoot, "src", "AuthServer", "OAuth.AuthServer.WebAPI");
        var psi = new ProcessStartInfo("dotnet")
        {
            Arguments        = $"ef database update --project \"{dbProjectPath}\" --startup-project \"{authServerPath}\"",
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

    private static Process StartService(
        string repoRoot, string relPath, string urls, string connStr,
        IReadOnlyDictionary<string, string>? extraEnv = null)
    {
        var projectPath = Path.Combine(repoRoot, relPath);
        var psi = new ProcessStartInfo("dotnet")
        {
            // --no-launch-profile：launchSettings.json 的預設 profile（例如 AuthServer 的 "http" profile）
            // 會用自己的 applicationUrl 覆蓋掉這裡設定的 ASPNETCORE_URLS，導致 HTTPS 端點沒有被綁定。
            Arguments        = $"run --project \"{projectPath}\" --no-launch-profile",
            WorkingDirectory = repoRoot,
            UseShellExecute  = false,
        };
        psi.Environment["ASPNETCORE_URLS"]                     = urls;
        psi.Environment["ASPNETCORE_ENVIRONMENT"]              = "Development";
        psi.Environment["ConnectionStrings__DefaultConnection"] = connStr;

        if (extraEnv is not null)
            foreach (var (key, value) in extraEnv)
                psi.Environment[key] = value;

        return Process.Start(psi)
            ?? throw new InvalidOperationException($"服務啟動失敗: {relPath}");
    }

    private static Task EnsureServiceReadyAsync(string baseUrl) =>
        WaitForReadyAsync(baseUrl, timeoutSeconds: 10);

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
