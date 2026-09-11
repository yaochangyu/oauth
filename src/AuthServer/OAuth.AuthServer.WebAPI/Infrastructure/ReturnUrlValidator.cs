using Microsoft.AspNetCore.Mvc;

namespace OAuth.AuthServer.WebAPI.Infrastructure;

/// <summary>
/// 防範 Open Redirect：所有涉及 returnUrl 的端點皆須透過此驗證，
/// 只允許本站相對路徑（<see cref="IUrlHelper.IsLocalUrl"/>）或指向 /connect/authorize 的路徑。
/// </summary>
public static class ReturnUrlValidator
{
    public static bool IsAllowed(IUrlHelper urlHelper, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return false;

        return urlHelper.IsLocalUrl(returnUrl) ||
               returnUrl.StartsWith("/connect/authorize", StringComparison.Ordinal);
    }
}
