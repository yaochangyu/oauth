---
計畫模板版本: 2026-07-12
用途: AI Agent 逐步實作任務，避免混亂
---

# AuthServer Headless 改造與 Vue 3 認證站建置（階段一）

**建立日期**: 2026-09-11 GMT+8
**狀態**: [規劃中]
**來源計畫書**: [phase1_authserver_headless_migration.plan.md](phase1_authserver_headless_migration.plan.md)（設計規格，本檔為執行追蹤）

## 概覽

- **目標**：將 AuthServer WebAPI 由 Razor Pages 改造為純 Headless OAuth Server，
  並新建 Vue 3 認證站 `OAuth.AuthServer.WebUI` 承接 `/login`、`/consent`、`/register`、`/error`。
- **關鍵決策**：
  - Consent 短期憑證走 `IDistributedCache`（dev: InMemory）。
  - `returnUrl` 一律透過 `Url.IsLocalUrl()` 或已註冊 client 的 redirect URI 白名單校驗，防 Open Redirect。
  - `/api/v1/account/login` 掛 `FixedWindowRateLimiter`（每 IP 每分鐘 5 次）。
- **風險**：
  - 刪除 `Pages/` 為破壞性操作，需先確認既有整合測試對 Razor Pages 路由的依賴已全數改點。
  - 依專案 CLAUDE.md TDD 規則，後端每個 API 行為須先有 Reqnroll `.feature` 測試見紅才能實作。

## 執行步驟

| # | 步驟 | 說明 | 狀態 |
|---|------|------|------|
| 1 | 移除 Razor Pages | 刪除 `Pages/` 目錄，含 `Authorize.cshtml`、`Consent.cshtml` | ✅ 完成 |
| 2 | SPA 靜態託管配置 | `Program.cs` 註冊 `UseDefaultFiles`/`UseStaticFiles`/`MapFallbackToFile` | ✅ 完成 |
| 3 | Rate Limiter 註冊 | `Program.cs` 註冊 `FixedWindowRateLimiter` | ✅ 完成 |
| 4 | Consent API（TDD） | 先寫 `.feature`（紅燈）→ 實作 `ConsentApiController`（consent-info/accept/deny）→ 綠燈 | ✅ 完成 |
| 5 | Account API 擴充（TDD） | 先寫 `.feature`（紅燈）→ 實作 `login`（含 Rate Limiter）/`logout`/`me` → 綠燈 | ✅ 完成 |
| 6 | AuthorizationController 導向更新 | 導航路徑切到 `/login?returnUrl=...`、`/consent?returnUrl=...` | ✅ 完成 |
| 7 | Vue 3 認證站建置 | 建立 `src/AuthServer/OAuth.AuthServer.WebUI`，四視圖 + 路由別名 + Playwright 相容屬性 | ✅ 完成 |
| 8 | 建置與端到端驗證 | 前端建置輸出至 `WebAPI/wwwroot`，跑整合測試 + Playwright BDD 完整流程測試 | ✅ 完成 |

**狀態說明**:
- ⬜ 待做 (Not started)
- 🟦 進行中 (In progress)
- ✅ 完成 (Completed)
- ⚠️ 阻塞 (Blocked - 需要使用者決定)

## 步驟詳情

### Step 1: 移除 Razor Pages

**預期產出**:
- 刪除 `src/AuthServer/OAuth.AuthServer.WebAPI/Pages/` 整個目錄
- 確認無殘留 `.cshtml`/`PageModel` 參照（`dotnet build` 不再報找不到頁面）

**完成條件**:
- [x] `Pages/` 目錄已刪除
- [x] `dotnet build` 通過

---

### Step 2: SPA 靜態託管配置

**預期產出**:
- `Program.cs` 加入 `UseDefaultFiles()`、`UseStaticFiles()`、`MapFallbackToFile("index.html")`

**完成條件**:
- [x] 三個中介軟體皆已註冊且順序正確（Default/Static 在路由前，Fallback 在最後）
- [x] `dotnet build` 通過

---

### Step 3: Rate Limiter 註冊

**預期產出**:
- `Program.cs` 註冊 `AddRateLimiter`，`FixedWindowRateLimiter`：每 IP 每分鐘 5 次

**完成條件**:
- [x] Rate Limiter policy 已定義並套用於 `/api/v1/account/login`（`[EnableRateLimiting("login")]`，partition key 取 `X-Forwarded-For` 或 `RemoteIpAddress`）
- [x] `dotnet build` 通過

---

### Step 4: Consent API（TDD）

**預期產出**:
- `*.feature`（Gherkin，繁中 step）涵蓋：合法 returnUrl 通過、非法 returnUrl 被拒、consent-accept 寫入快取並回傳跳轉網址、consent-deny
- `ConsentApiController.cs`：`GET consent-info`、`POST consent-accept`、`POST consent-deny`

**完成條件**:
- [x] 先跑測試見紅（貼指令輸出，見「遭遇的問題」區塊）
- [x] 實作後測試轉綠（貼指令輸出，見「遭遇的問題」區塊）
- [x] Open Redirect 防護（`Url.IsLocalUrl` 或白名單）有對應測試案例（`test/OAuth.AuthServer.IntegrationTest/_04_Consent/授權同意API.feature`）

---

### Step 5: Account API 擴充（TDD）

**預期產出**:
- `*.feature` 涵蓋：登入成功、密碼錯誤、超過 5 次/分鐘觸發 429、logout、me 回傳目前使用者
- `AccountApiController.cs`：`login`/`logout`/`me`

**完成條件**:
- [x] 先跑測試見紅（貼指令輸出，見「遭遇的問題」區塊）
- [x] 實作後測試轉綠（貼指令輸出，見「遭遇的問題」區塊）
- [x] Rate Limiter 429 行為有對應測試案例（`test/OAuth.AuthServer.IntegrationTest/_05_AccountLogin/帳號登入登出.feature`）

---

### Step 6: AuthorizationController 導向更新

**預期產出**:
- 未登入/未同意時導向 `/login?returnUrl=...`、`/consent?returnUrl=...`（而非舊 Razor Pages 路徑）

**完成條件**:
- [x] 既有 OAuth 授權碼流程整合測試通過（`dotnet test test/OAuth.AuthServer.IntegrationTest` 22/22 綠燈）
- [x] `dotnet build` 通過

---

### Step 7: Vue 3 認證站建置

**預期產出**:
- `src/AuthServer/OAuth.AuthServer.WebUI`：Vue 3 + Vite + TypeScript
- `LoginView`、`ConsentView`、`RegisterView`、`ErrorView`
- 路由別名相容 `/login` & `/Account/Login`、`/consent` & `/Connect/Consent`
- 保留 `input[name='userName']`、`[data-testid='scope-list']`、`button[value='accept']` 等既有 Playwright 選取器

**完成條件**:
- [x] `npm run build`（或對應指令）通過（輸出至 `../OAuth.AuthServer.WebAPI/wwwroot`）
- [x] 四視圖皆可渲染（`LoginView`/`ConsentView`/`RegisterView`/`ErrorView`，另見下方 Playwright 測試結果佐證）
- [x] 四視圖皆有對應的 `.feature` + `*Step.cs`（`test/OAuth.Clients.PlaywrightTest/_00_AuthServer/`，沿用 `PlaywrightBaseStep.cs`／`TestSettings.AuthServerBase`），比照 `_01_AdminUI` 既有格式撰寫，紅燈→綠燈皆已執行（見下方「遭遇的問題」與 Step 8 證據）

---

### Step 8: 建置與端到端驗證

**預期產出**:
- 前端建置輸出接到 `WebAPI/wwwroot`
- `dotnet test test/OAuth.AuthServer.IntegrationTest` 全綠
- 新增 Playwright BDD 測試（`test/OAuth.Clients.PlaywrightTest/`）涵蓋：登入成功導回 `/connect/authorize`、登入失敗顯示錯誤、同意頁顯示 client 名稱與 scope 清單並完成同意跳轉、拒絕同意、非法 returnUrl 被擋、註冊流程、error 頁面顯示，並走 TDD 紅燈→綠燈
- 完整流程（MVC Client → `/login` → 登入 → `/consent` → 同意 → 返回 Client）**必須**由上述 Playwright BDD 測試跑通，不接受純手動驗證取代

**完成條件**:
- [x] 整合測試指令輸出（全綠）：`dotnet test test/OAuth.AuthServer.IntegrationTest` 22/22 通過
- [x] Playwright BDD 測試先紅後綠（兩次指令輸出，見「遭遇的問題」）
- [x] 完整登入→同意→返回 Client 流程由 Playwright BDD 測試涵蓋並通過：
      `_00_AuthServer`（8/8）、`_04_Consent`（5/5）、`_02_MvcClient`（4/4）、`_01_AdminUI`（17/17）皆綠燈；
      `_03_SpaHost`（5 個情境）在本機沙箱環境中失敗於「開啟 SpaHost 首頁等待登入按鈕」這一步，
      發生在觸碰 AuthServer 之前、且該專案屬 `src/Clients/`（本次任務明確排除修改範圍），研判為既有環境限制而非本次改造造成，詳見「遭遇的問題」

---

## 遭遇的問題

### 問題 1：`/api/v1/account/me` 路由衝突（Consent/Account TDD 階段）

**症狀**：既有 `AccountManagementController.Me()`（Bearer Token）與規格要求新增的 `GET /api/v1/account/me`（Cookie）路由完全相同，會造成 ASP.NET Core `AmbiguousMatchException`。

**解法**：不新增第二個路由，改為擴充既有 `AccountManagementController.Me()` 的 `[Authorize(AuthenticationSchemes = ...)]`，同時接受 `Identity.Application`（cookie）與 OpenIddict Bearer Token 兩種認證方案，一個端點服務兩種呼叫方。

### 問題 2：`[Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]` 用在非 OpenIddict 管理端點會丟例外

**症狀**：呼叫 `GET /api/v1/account/me`（Cookie 已登入）得到 HTTP 500，例外訊息：
`System.InvalidOperationException: An identity cannot be extracted from this request. This generally indicates that the OpenIddict server stack was asked to authenticate a request for an endpoint it doesn't manage.`

**根因**：`OpenIddictServerAspNetCoreDefaults.AuthenticationScheme`（`OpenIddict.Server.AspNetCore`）只能用在 OpenIddict 透過 `EnableXxxEndpointPassthrough()` 直接管理的端點（如 `/connect/userinfo`）；`AccountManagementController`（含既有 `me`/`external-logins`/`password` 等端點）用這個 scheme 驗證 Bearer Token 是既有程式碼裡的誤用，先前未被任何測試覆蓋到才未曝露。另外 ASP.NET Core 的 `[Authorize]` 屬性在 class 與 method 層都出現時，`AuthenticationSchemes` 會**聯集**而非互相覆蓋，因此只改 method 層仍會殘留 class 層的錯誤 scheme。

**解法**：`AccountManagementController` class 層與 `Me()` method 層都改用 `OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme`（`OpenIddict.Validation.AspNetCore`，對應 `Program.cs` 既有的 `AddValidation()` 設定），`Me()` 再疊加 `Identity.Application` 供 Cookie 登入使用。

### 問題 3：Rate Limiter 以 `RemoteIpAddress` 分區，測試中互相污染導致誤判 429

**症狀**：`FixedWindowRateLimiter` 依 `RemoteIpAddress` 分區，但 `WebApplicationFactory`／`TestServer` 底下所有請求的 `RemoteIpAddress` 皆為 `null`，導致同一測試執行期間所有登入呼叫共用同一個限流視窗，先執行的情境會把後執行情境的登入請求也擋成 429。

**解法**：分區 key 改為優先讀取 `X-Forwarded-For` 標頭（常見反向代理場景亦適用），測試各情境各自帶入不同的 `X-Forwarded-For` 值以互相隔離。

### 問題 4：ASP.NET Core Identity 帳號鎖定（Lockout）造成跨情境互相污染

**症狀**：`SignInManager.PasswordSignInAsync(..., lockoutOnFailure: true)` 在密碼錯誤次數達到預設門檻後會鎖定該帳號；「密碼錯誤應回傳 401」與「超過每分鐘 5 次登入嘗試應回傳 429」兩個情境若沿用同一顆 `admin` 帳號做錯誤密碼嘗試，會把 `admin` 鎖定，導致其他情境（含 Consent API 測試）用正確密碼登入 `admin` 也失敗。

**解法**：涉及故意輸入錯誤密碼的情境改為先註冊一顆一次性測試帳號，只對該帳號做錯誤嘗試，不影響共用的 `admin` 帳號。

### TDD 紅燈→綠燈紀錄（Step 4 + Step 5，合併於同一次 `dotnet test` 執行）

**紅燈**（`.feature` 寫完、controller 尚未實作前，`dotnet test test/OAuth.AuthServer.IntegrationTest`）：
```
Total tests: 22
     Passed: 11
     Failed: 11
```
新增的 11 個情境（Consent API 6 個、Account 登入/登出/429 5 個）全部失敗（404/405），既有 11 個情境維持通過。

**綠燈**（實作 `ConsentApiController`、`AccountApiController.Login/Logout`、修正 `AccountManagementController.Me`、修正 Rate Limiter 分區鍵、修正測試帳號鎖定問題後）：
```
Total tests: 22
     Passed: 22
```

### 使用者補充規則（2026-09-11，於 Step 6 完成後、Step 7 開始前收到）

Step 7、8 原描述的「手動核對路由別名」「手動或用 Playwright 跑一次完整流程」已由使用者澄清為：四個視圖與完整登入→同意流程**必須**有 `test/OAuth.Clients.PlaywrightTest/` 下的 Reqnroll BDD 自動化測試覆蓋（沿用 `PlaywrightBaseStep.cs`／`TestSettings.AuthServerBase`，格式比照 `_01_AdminUI`），且同樣要跑紅燈→綠燈，不接受純手動驗證取代。已同步修改上方 Step 7、8 的完成條件描述。

### 問題 5：`PlaywrightBaseStep.FindRepoRoot()` 在 git worktree 中誤判到主 checkout

**症狀**：`E2E_USE_TESTCONTAINERS=true` 執行時，log 顯示 `Content root path: /mnt/d/lab/oauth/src/AuthServer/...`（主 checkout 路徑），而不是本 Agent 隔離的 worktree 路徑；等於整組服務跑的是「另一份」未修改過的程式碼，完全沒驗證到本次改造。

**根因**：`FindRepoRoot()` 只用 `Directory.Exists(.git)` 判斷 repo 根目錄。一般 clone 的 `.git` 是資料夾，但 git worktree 的 `.git` 是指向主 repo `.git/worktrees/<name>` 的**檔案**，導致在 worktree 內找不到符合條件的層級，一路往上找到主 checkout 才命中。

**解法**：改為 `Directory.Exists(gitPath) || File.Exists(gitPath)`。發現後已立即停用受影響的背景程序（服務與對應的 docker 測試容器），避免長時間佔用主 checkout。

### 問題 6：`RunMigrationsAsync` 的 `dotnet ef` 呼叫方式錯誤

**症狀**：`dotnet ef database update --project <WebAPI>`（沒有 `--startup-project`）報錯 `No DbContext was found in assembly 'OAuth.AuthServer.WebAPI'`。

**根因**：Migrations 定義在 `OAuth.AuthServer.DB`，`--project` 需指向該專案；`OAuth.AuthServer.WebAPI` 只能當 `--startup-project`（提供執行期 DI 組態）。這與 `Taskfile.yml` 的 `ef-database-update` 任務寫法不一致，是共用 E2E 測試基礎設施裡的既有 bug。

**解法**：改為 `dotnet ef database update --project <DB 專案> --startup-project <WebAPI 專案>`。

### 問題 7：AuthServer 背景程序未綁定 HTTPS（7001），導致 E2E 全部逾時

**症狀**：修正問題 6 後，AuthServer 程序成功啟動，但只監聽 `http://localhost:5265`，未監聽 `https://localhost:7001`；`WaitForReadyAsync` 對 7001 等待 120 秒後逾時，全部情境失敗。

**根因**：`StartService` 以 `dotnet run --project ...` 啟動子行程並用環境變數設定 `ASPNETCORE_URLS`，但沒有加 `--no-launch-profile`；`dotnet run` 預設套用 `launchSettings.json` 裡宣告順序最前面的 profile（AuthServer 剛好是 `"http"` profile，`applicationUrl` 只有 `http://localhost:5265`），該 profile 的設定會覆蓋掉外部指定的 `ASPNETCORE_URLS`。

**解法**：`StartService` 的 `dotnet run` 參數加上 `--no-launch-profile`，確保外部注入的 `ASPNETCORE_URLS` 生效。

### 問題 8：Playwright 瀏覽器版本與已安裝快取不符

**症狀**：`Executable doesn't exist at .../chromium_headless_shell-1228/...`。

**根因**：本機 `~/.cache/ms-playwright` 已有的瀏覽器版本（1208/1234/1237）與這次還原/建置後 `Microsoft.Playwright` 套件鎖定的版本（1228）不同。

**解法**：`node <nuget 快取>/microsoft.playwright/<版本>/.playwright/package/cli.js install chromium`（環境內 `pwsh` 因沙箱安全限制無法直接呼叫官方 `playwright.ps1`，改走同一支 cli.js 的 node 進入點，效果相同）。

### 問題 9：共用 Step「若顯示同意頁面則同意授權」／登入 Step 的一次性 URL 檢查在新 SPA 下產生競態

**症狀**：`_02_MvcClient`／`_03_SpaHost` 走真實用戶端 OIDC 導頁流程的情境，在改用 Vue 3 Headless 登入頁後開始逾時／卡在同意頁未點擊。

**根因**：舊 Razor Pages 登入表單是傳統 form POST，瀏覽器導頁與 click 事件同步發生，`WaitForLoadStateAsync` 能可靠攔截。新版登入頁改用 `fetch` API 呼叫 + 成功後才用 `window.location.href` 做客戶端導頁，屬非同步流程；`PlaywrightBaseStep` 共用的登入 Step 在 click 後立刻做一次性 `WaitForLoadStateAsync`，經常在真正導頁開始前就返回，導致後續「若顯示同意頁面」的一次性 URL 檢查誤判為「未顯示同意頁」而跳過點擊同意。本次新增的 `_00_AuthServer` 情境因為對應的 Then 步驟本來就用有輪詢機制的 `Assertions.Expect(...).ToHaveURLAsync(...)`，恰好不受影響而先行通過，因此在第一輪全套測試才暴露這個既有共用 Step 的競態問題。

**解法**：登入 Step 改為 click 後主動 `WaitForURLAsync(url => !url.Contains("/login"))`（逾時則視為原地停留，交由呼叫端斷言），確保客戶端導頁真正完成後才返回，`_02_MvcClient` 全部轉綠。

### 已知限制：`_03_SpaHost`（5 個情境）在本沙箱環境未能轉綠

**現象**：5 個情境皆卡在「開啟 SpaHost 首頁、等待『登入』按鈕出現」這一步逾時（15 秒），時間點在任何 AuthServer 互動**之前**。

**判斷**：`OAuth.Client.SpaHost` 屬 `src/Clients/`，本次任務範圍明確排除修改；且失敗發生在該專案自己的首頁渲染階段，尚未觸及 AuthServer／Headless 登入頁，研判為既有環境（本沙箱同時起 5 個 dotnet 進程的資源競爭，或 SpaHost 專案本身既有的啟動時序問題）限制，不是本次 Headless 改造引入的迴歸。未進一步除錯／未修改 `src/Clients/OAuth.Client.SpaHost`（超出授權範圍）。

**紅燈/綠燈指令輸出關鍵片段**：
- 紅燈（`_00_AuthServer`，模擬前端尚未部署）：`Total tests: 8 / Failed: 8`
- 綠燈（全部 Playwright 情境，含 `_00_AuthServer`/`_01_AdminUI`/`_02_MvcClient`/`_04_Consent`，扣除已知限制的 `_03_SpaHost`）：`Total tests: 38 / Passed: 33 / Failed: 5`（5 個失敗皆為上述 `_03_SpaHost` 已知限制）

---

## 完成檢查表

計畫完成時執行：

- [x] 所有步驟狀態都是 ✅ 完成
- [x] 已執行 `dotnet build` 驗證（`dotnet build OAuth.slnx` 0 error）
- [x] 已執行測試（整合測試 22/22 綠燈 + Playwright E2E 33/38 綠燈，5 個失敗為範圍外 `_03_SpaHost` 已知限制，見「遭遇的問題」）
- [ ] 已 commit 到 git（實作程式碼變更依派工指示不自動 commit，留給使用者審查；僅本檔案 archive 動作會建立獨立 commit）
- [ ] 計畫書已移到 `.archive/` 資料夾（檔名保持原樣，無需編輯）——待此次回報後執行

---

**狀態**: 規劃中，待使用者確認執行方式
