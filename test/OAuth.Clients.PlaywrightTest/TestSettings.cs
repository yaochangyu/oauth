namespace OAuth.Clients.PlaywrightTest;

public static class TestSettings
{
    public static string AuthServerBase => Env("E2E_AUTH_SERVER_URL", "https://localhost:7001");
    public static string AdminUIBase    => Env("E2E_ADMIN_UI_URL",    "https://localhost:7002");
    public static string MvcClientBase  => Env("E2E_MVC_CLIENT_URL",  "https://localhost:5101");
    public static string WebApiBase     => Env("E2E_WEBAPI_URL",       "https://localhost:5102");
    public static string SpaHostBase    => Env("E2E_SPA_HOST_URL",     "https://localhost:5200");

    public static string AdminUserName  => Env("E2E_ADMIN_USERNAME",  "admin");
    public static string AdminPassword  => Env("E2E_ADMIN_PASSWORD",  "Admin@123456");

    private static string Env(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) ?? fallback;
}
