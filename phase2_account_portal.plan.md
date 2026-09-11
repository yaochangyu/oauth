# 階段二詳細計畫書：會員自服務中心 (Account Portal) - 修訂版

## Goal Description
建置業務需求模組 **`src/Account/`**，包含專屬後端 **`OAuth.Account.WebAPI`** 與前端 **`OAuth.Account.WebUI`**（Vue 3 SPA）。
提供終端會員專屬的自服務儀表板，涵蓋個人基本資料維護、帳號安全管理（密碼/2FA），以及 OAuth 核心隱私功能：**利用 OpenIddict 原生授權引擎，檢視目前已授權的所有第三方 App，並可主動一鍵撤銷授權（自動級聯作廢關聯 Tokens）**。

---

## 審查修正重點

> [!IMPORTANT]
> **揚棄自建資料表，全面改用 OpenIddict 原生授權體系**：
> 審查報告嚴正指出：自建 `UserConsent` 資料表會導致狀態不同步的嚴重漏洞。
> OpenIddict 本身原生即具備 `OpenIddictEntityFrameworkCoreAuthorization`（Authorizations 表）。
> 本計畫全面採用 **`IOpenIddictAuthorizationManager`** 進行授權紀錄管理，查詢用戶已授權的 App，且在使用者點擊「解除授權」時，由 OpenIddict 自動級聯撤銷（Revoke）底下的所有 Access Token 與 Refresh Token！

> [!IMPORTANT]
> **身分驗證拓撲定調 (First-Party PKCE SPA)**：
> 為避免獨立子網域（`account.domain.com`）遭現代瀏覽器阻擋第三方 Cookie（ITP）與 CSRF 威脅，會員中心前端作為**官方第一方授權客戶端（First-Party PKCE SPA）**，透過標準 PKCE 授權碼流程取得 Bearer Token，後端 API 統一透過 OpenIddict Validation 本地驗證 Bearer Token。

---

## 目錄結構與架構邊界

```
src/Account/
├── OAuth.Account.WebAPI/                     # 後端專用 API
│   ├── Controllers/
│   │   ├── ProfileController.cs              # 個人資料維護 API (Bearer 驗證)
│   │   ├── SecurityController.cs             # 密碼修改、2FA TOTP 啟用與驗證
│   │   └── ConsentsController.cs             # ★ 利用 IOpenIddictAuthorizationManager 管理授權
│   ├── Models/
│   │   ├── ProfileDtos.cs
│   │   ├── SecurityDtos.cs
│   │   └── ConsentDtos.cs
│   ├── Program.cs                            # 註冊 OpenIddict Validation (Local Server 驗證)
│   └── OAuth.Account.WebAPI.csproj
│
└── OAuth.Account.WebUI/                      # 前端 Vue 3 + Vite SPA (PKCE 驗證，獨立部署 account.domain.com)
    ├── package.json
    ├── vite.config.ts
    ├── src/
    │   ├── auth/oidc.ts                      # OIDC Client TS 配置 (向 AuthServer 索取 Bearer Token)
    │   ├── api/
    │   │   ├── profile.ts
    │   │   ├── security.ts
    │   │   └── consents.ts
    │   ├── router/index.ts
    │   └── views/
    │       ├── ProfileView.vue               # 個人資料檢視與修改 (頭像、暱稱)
    │       ├── SecurityView.vue              # 密碼修改、2FA Authenticator 綁定
    │       └── AuthorizedAppsView.vue        # ★ 已授權第三方應用程式管理卡片清單
    └── index.html
```

---

## 核心 API 介面規格 (利用 OpenIddict 原生機制)

### 1. 原生已授權應用程式管理 (`ConsentsController.cs`)

```csharp
[ApiController]
[Route("api/v1/account/consents")]
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public class ConsentsController(
    IOpenIddictAuthorizationManager authorizationManager,
    IOpenIddictApplicationManager applicationManager,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAuthorizedApps()
    {
        var userId = User.GetClaim(Claims.Subject)!;

        // 利用 OpenIddict 原生查詢該用戶所有的永久授權 (Permanent Authorizations)
        var authorizations = await authorizationManager.FindAsync(
            subject: userId,
            client: null,
            status: OpenIddictConstants.Statuses.Valid,
            type: OpenIddictConstants.AuthorizationTypes.Permanent).ToListAsync();

        var result = new List<AuthorizedAppDto>();
        foreach (var auth in authorizations)
        {
            var appId = await authorizationManager.GetApplicationIdAsync(auth);
            var app = await applicationManager.FindByIdAsync(appId!);
            var scopes = await authorizationManager.GetScopesAsync(auth);
            var createdAt = await authorizationManager.GetCreationDateAsync(auth);

            result.Add(new AuthorizedAppDto(
                AuthorizationId: await authorizationManager.GetIdAsync(auth)!,
                ClientId: await applicationManager.GetClientIdAsync(app!)!,
                ClientDisplayName: await applicationManager.GetDisplayNameAsync(app!) ?? "未知應用",
                Scopes: scopes.ToList(),
                AuthorizedAt: createdAt?.UtcDateTime ?? DateTime.UtcNow
            ));
        }

        return Ok(result);
    }

    [HttpDelete("{authorizationId}")]
    public async Task<IActionResult> RevokeAppConsent(string authorizationId)
    {
        var userId = User.GetClaim(Claims.Subject)!;
        var auth = await authorizationManager.FindByIdAsync(authorizationId);
        
        if (auth is null) return NotFound();

        // 確保只能撤銷自己的授權
        if (await authorizationManager.GetSubjectAsync(auth) != userId)
            return Forbid();

        // 原生撤銷：自動連帶作廢該授權底下的所有 Access Token 與 Refresh Token！
        await authorizationManager.RevokeAsync(auth);

        return NoContent();
    }
}
```

---

## 前端視圖與互動體驗設計 (`OAuth.Account.WebUI`)

### `AuthorizedAppsView.vue`
- 使用者進入會員中心時，透過 `oidc-client-ts` 取得的 Bearer Token 呼叫 `/api/v1/account/consents`。
- 呈顯已授權應用程式卡片：
  - 應用程式名稱、授權日期。
  - 擁有的權限標籤（如 `[個人資訊 (profile)]`、`[電子信箱 (email)]`、`[離線存取 (offline_access)]`）。
  - 「**解除授權**」按鈕。
- 點擊解除授權時，確認對話框提示：「解除後，該應用程式將無法再存取您的帳號，且現有的連線將立即中斷。」
- 呼叫 `DELETE /api/v1/account/consents/{authorizationId}`，成功後立即將卡片移除。

---

## 驗證計畫

1. **整合測試 (BDD)**：
   - 建立客戶端完成授權流程，產生有效 Authorization。
   - 呼叫 `GET /api/v1/account/consents` 驗證回傳清單包含該客戶端。
   - 呼叫 `DELETE` 撤銷授權。
   - 驗證原客戶端使用 Refresh Token 換發新 Token 時收到 `invalid_grant` 失敗。
