using Microsoft.Extensions.Configuration;

namespace OAuth.Client.WebAPI.IntegrationTest;

/// <summary>
/// 測試夾具：啟動 WebAPI 伺服器
/// </summary>
public class TestFixture : IAsyncLifetime
{
    private WebApiTestFactory? _webApiFactory;
    private HttpClient? _webApiClient;
    private IConfiguration? _config;

    public HttpClient WebApiClient => _webApiClient ?? throw new InvalidOperationException("TestFixture 未初始化");
    public IConfiguration Config => _config ?? throw new InvalidOperationException("TestFixture 未初始化");
    public WebApiTestFactory TestFactory => _webApiFactory ?? throw new InvalidOperationException("TestFactory 未初始化");

    /// <summary>
    /// 初始化：啟動 WebAPI 伺服器、載入測試設定
    /// </summary>
    public async Task InitializeAsync()
    {
        // 1. 啟動 WebAPI 伺服器（WebApplicationFactory 自動管理）
        _webApiFactory = new WebApiTestFactory();
        
        // 2. 建立 HttpClient，這會觸發 ConfigureWebHost 執行
        _webApiClient = _webApiFactory.CreateClient();

        // 3. 載入 appsettings.json
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

        _config = configBuilder.Build();
    }

    /// <summary>
    /// 清理：停止 WebAPI 伺服器
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_webApiFactory is not null)
        {
            await _webApiFactory.DisposeAsync();
        }
        _webApiClient?.Dispose();
    }
}
