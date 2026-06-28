# OAuth2 + OIDC Authorization Server 實作計畫

## 目標

建立一套自架 Authorization Server，支援 Web（ASP.NET Core 10 MVC）、API（ASP.NET Core 10 Web API）、SPA（Vue/React）驗證，
整合 Google、Microsoft、Line、Facebook、Instagram（Meta）、Threads 社群登入，並有本地帳密系統。
使用 Playwright E2E 測試驗證 SSO 站台互動（登入、登出、跨站 SSO、Token 刷新）。

---

## 技術棧

| 層次 | 技術 |
|---|---|
| Authorization Server | ASP.NET Core 10 + OpenIddict 5.x |
| 使用者系統 | ASP.NET Core Identity + EF Core |
| 資料庫 | PostgreSQL |
| Social Login | Google、Microsoft、Line、Facebook/Instagram（Meta）、Threads（自訂 Handler） |
| SPA 客戶端 | Authorization Code + PKCE（oidc-client-ts） |
| Web 客戶端 | ASP.NET Core 10 MVC + Cookie（OpenIdConnect） |
| API 客戶端 | ASP.NET Core 10 Web API + Bearer Token |
| Refresh Token | Rotation 策略（每次用完換新） |
| API 整合測試 | Reqnroll（BDD）+ Testcontainers（PostgreSQL）+ WebApplicationFactory |
| E2E 測試 | Playwright（驗證 SSO 站台互動） |

---

## 編程規則（依 api.template CLAUDE.md）

- **分層架構**：Controller → Handler → Repository
- **Result Pattern**：使用 `CSharpFunctionalExtensions` 回傳 `Result<T, Failure>`，禁止 throw exception 作為流程控制
- **不可變物件**：TraceContext 使用 `record` + `init`
- **非同步**：所有 I/O 必須 async/await，支援 CancellationToken
- **DbContextFactory**：使用 `IDbContextFactory<T>`，禁止直接注入 DbContext
- **主建構函式注入**：使用 C# 12 Primary Constructor
- **AsNoTracking**：所有唯讀查詢加上 AsNoTracking()
- **命名規範**：Handler / Repository / Controller 各自獨立，依功能命名
- **禁止對 Controller 單元測試**：必須透過完整 Web API 管線測試
- **Cucumber 步驟用中文**，保留字（Feature、Background、Scenario、Given、When、Then）用英文

---

## 專案結構

```
/mnt/d/lab/oauth/
├── src/
│   ├── AuthServer/                          # Authorization Server（.NET 10）
│   │   ├── OAuth.AuthServer.WebAPI/         # 主程式（Controllers、Middleware）
│   │   ├── OAuth.AuthServer.DB/             # EF Core + PostgreSQL（Identity + OpenIddict）
│   │   └── OAuth.AuthServer.Contract/       # OpenAPI 產生的 DTO 合約
│   └── Clients/
│       ├── OAuth.Client.Mvc/               # ASP.NET Core 10 MVC 示範客戶端（Cookie SSO）
│       └── OAuth.Client.WebAPI/            # ASP.NET Core 10 Web API 示範客戶端（Bearer Token）
├── test/
│   ├── OAuth.AuthServer.IntegrationTest/   # BDD 整合測試（Reqnroll + Testcontainers）
│   └── OAuth.E2E.PlaywrightTest/           # Playwright E2E 測試（SSO 互動驗證）
├── doc/
│   └── openapi.yml                          # OpenAPI 規格
├── docker-compose.yml                       # PostgreSQL + Seq（本地開發）
├── Taskfile.yml                             # 開發指令集中管理
└── OAuth.sln
```

---

## 實作步驟

### Phase 1：基礎建設

- [x] **Step 1.1 - 建立 Solution 與專案結構**
  - 建立 `OAuth.sln`
  - 建立 `OAuth.AuthServer.WebAPI`（ASP.NET Core 10）
  - 建立 `OAuth.AuthServer.DB`（Class Library，EF Core）
  - 建立 `OAuth.AuthServer.IntegrationTest`（Reqnroll + Testcontainers）
  - 建立 `docker-compose.yml`（PostgreSQL 16、Seq）
  - 建立 `Taskfile.yml`（build、test、ef-migration-add、ef-database-update）
  - 更新 `tree.md`

- [x] **Step 1.2 - 設定 EF Core + ASP.NET Core Identity + PostgreSQL**
  - 安裝 `Npgsql.EntityFrameworkCore.PostgreSQL`
  - 安裝 `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - 建立 `ApplicationUser`（繼承 IdentityUser，加入 DisplayName、AvatarUrl 欄位）
  - 建立 `ApplicationDbContext`（繼承 IdentityDbContext）
  - 設定 EF Core Migration
  - 建立初始 Migration `InitialCreate`

- [x] **Step 1.3 - 整合 OpenIddict**
  - 安裝 `OpenIddict.AspNetCore`、`OpenIddict.EntityFrameworkCore`
  - 在 `ApplicationDbContext` 加入 OpenIddict Entity Sets
  - 設定 OpenIddict：
    - 啟用 Authorization Code Flow + PKCE
    - 啟用 Refresh Token + Rotation
    - 啟用 Client Credentials Flow（Server-to-Server 備用）
    - 設定 Token 有效期（Access Token 15min、Refresh Token 30 天 sliding）
  - Migration 加入 OpenIddict 資料表

- [x] **Step 1.4 - 建立 Seed Data（OpenIddict Applications）**
  - 建立 SPA Client（Authorization Code + PKCE，public client）
  - 建立 MVC Client（Authorization Code + PKCE，confidential client，Cookie SSO）
  - 建立 WebAPI Client（Client Credentials，confidential client，Bearer Token）
  - 建立測試用 Postman Client

---

### Phase 2：本地帳號系統

- [x] **Step 2.1 - 實作 Register（本地帳密註冊）**
  - `POST /api/v1/account/register`
  - Handler：`AccountHandler.RegisterAsync`
  - 驗證：Email 格式、密碼強度（FluentValidation）
  - Result Pattern 回傳
  - BDD Feature：`帳號註冊.feature`（成功、Email 重複、密碼不合規）

- [x] **Step 2.2 - 實作 Login Page（本地帳密登入）**
  - OpenIddict 需要 `/connect/authorize` 端點（Razor Page 實作登入 UI）
  - 建立 `AccountController`：`Login` GET/POST、`Logout`
  - 登入成功後由 OpenIddict 發放 Code
  - BDD Feature：`本地登入.feature`（成功、密碼錯誤、帳號不存在）

- [x] **Step 2.3 - 實作 Token Endpoint**
  - OpenIddict 自動處理 `/connect/token`
  - 設定 Refresh Token Rotation
  - BDD Feature：`token交換.feature`（code 換 token、refresh token rotation）

---

### Phase 3：Social Login 聯邦

- [x] **Step 3.1 - Google Login**
  - 安裝 `Microsoft.AspNetCore.Authentication.Google`
  - 設定 Google OAuth2（ClientId、ClientSecret 從環境變數讀取）
  - 建立 External Login Callback 邏輯：
    - 若本地已有綁定 → 直接登入
    - 若 Email 已存在 → 詢問綁定
    - 若全新使用者 → 建立帳號 + 綁定
  - BDD Feature：`google登入.feature`（新使用者、已存在使用者）

- [x] **Step 3.2 - Microsoft Login**
  - 安裝 `Microsoft.AspNetCore.Authentication.MicrosoftAccount`
  - 同 Step 3.1 流程
  - BDD Feature：`microsoft登入.feature`

- [x] **Step 3.3 - Line Login**
  - 安裝 `AspNet.Security.OAuth.Line`（aspnet-contrib）
  - 設定 Line OAuth2
  - BDD Feature：`line登入.feature`

- [x] **Step 3.4 - Facebook Login（含 Instagram）**
  - 安裝 `Microsoft.AspNetCore.Authentication.Facebook`
  - 設定 Facebook OAuth2（同時處理 Instagram 帳號，UX 顯示「以 Instagram 繼續」）
  - 說明：Instagram 消費者帳號登入底層走 Meta Facebook Login，使用同一 app_id
  - BDD Feature：`facebook登入.feature`、`instagram登入.feature`

- [x] **Step 3.5 - Threads Login（自訂 Handler）**
  - Threads API OAuth2：
    - Authorization URL：`https://threads.net/oauth/authorize`
    - Token URL：`https://graph.threads.net/oauth/access_token`
    - Scope：`threads_basic`
  - 繼承 `OAuthHandler<ThreadsOptions>` 實作自訂 Handler
  - 實作 `ThreadsOptions`、`ThreadsExtensions`
  - BDD Feature：`threads登入.feature`

---

### Phase 4：API 端點

- [x] **Step 4.1 - UserInfo Endpoint**
  - `GET /connect/userinfo`（OpenIddict 標準端點）
  - 回傳：sub、name、email、picture（來自 social profile 或本地資料）
  - BDD Feature：`userinfo.feature`

- [x] **Step 4.2 - Account 管理 API**
  - `GET /api/v1/account/me`：取得目前登入使用者資料
  - `GET /api/v1/account/external-logins`：取得已綁定的 Social Provider 清單
  - `DELETE /api/v1/account/external-logins/{provider}`：解除綁定
  - `PUT /api/v1/account/password`：修改密碼
  - BDD Feature：`帳號管理.feature`

---

### Phase 5：Client 示範站台（ASP.NET Core 10）

- [x] **Step 5.1 - ASP.NET Core 10 MVC Client（Cookie SSO）**
  - 建立 `OAuth.Client.Mvc` 專案（ASP.NET Core 10 MVC）
  - 設定 OpenIdConnect + Cookie Authentication：
    - Cookie Authentication（存放登入 Session）
    - OpenIdConnect（指向本地 AS，Authorization Code + PKCE）
    - 自動 Refresh Token（Token 過期前靜默刷新）
  - 示範頁面：
    - `/`：首頁（顯示登入狀態）
    - `/account/login`：觸發 OIDC Challenge
    - `/account/logout`：登出（清 Cookie + 通知 AS 撤銷 Token）
    - `/profile`：需登入（顯示 sub、name、email、picture）
  - 啟動埠：`https://localhost:5101`

- [x] **Step 5.2 - ASP.NET Core 10 Web API Client（Bearer Token）**
  - 建立 `OAuth.Client.WebAPI` 專案（ASP.NET Core 10 Web API）
  - 設定 JWT Bearer Authentication（驗證 AS 發放的 Access Token）
  - 示範端點：
    - `GET /api/v1/me`：需 Bearer Token，回傳登入使用者資料（從 JWT Claims 取得）
    - `GET /api/v1/protected`：需 Bearer Token + 特定 Scope（`api:read`）
  - 啟動埠：`https://localhost:5102`

- [ ] **Step 5.3 - Playwright E2E 測試（SSO 互動驗證）**
  - 建立 `OAuth.E2E.PlaywrightTest` 專案
  - 安裝 `Microsoft.Playwright.NUnit` 或 `Microsoft.Playwright.MSTest`
  - 測試情境（均透過真實瀏覽器互動）：

    **情境 A：本地帳密 SSO 完整流程**
    ```
    Given 使用者未登入，瀏覽 MVC Client 受保護頁面
    When  被重導至 AS 登入頁，輸入帳密登入
    Then  重導回 MVC Client，顯示使用者資料（Cookie 已建立）
    When  直接瀏覽第二個受保護頁面
    Then  無需再次登入（SSO Cookie 有效）
    ```

    **情境 B：Social Login（Google）SSO 流程**
    ```
    Given 使用者未登入，點擊「以 Google 繼續」
    When  重導至 Google 登入（使用 Playwright 填入 Google 測試帳號）
    Then  重導回 AS 完成綁定，再重導回 MVC Client
    Then  顯示 Google 帳號資訊（name、email、picture）
    ```

    **情境 C：跨站 SSO（MVC + WebAPI）**
    ```
    Given 使用者已在 MVC Client 登入
    When  MVC Client 呼叫 WebAPI（帶 Access Token）
    Then  WebAPI 回傳 200 與使用者資料
    When  Access Token 過期
    Then  MVC Client 自動用 Refresh Token 換新 Token，WebAPI 仍回傳 200
    ```

    **情境 D：登出（Single Logout）**
    ```
    Given 使用者已登入 MVC Client
    When  使用者點擊登出
    Then  Cookie 清除，AS 撤銷 Refresh Token
    When  嘗試再次瀏覽受保護頁面
    Then  被重導至登入頁（Session 已失效）
    ```

    **情境 E：Refresh Token Rotation**
    ```
    Given 使用者已登入，持有 Refresh Token
    When  使用舊 Refresh Token 換新 Access Token
    Then  得到新 Access Token + 新 Refresh Token
    When  再次使用舊 Refresh Token（已 Rotation）
    Then  AS 回傳 400 invalid_grant（Token 已失效）
    ```

---

### Phase 6：整合測試基礎建設

- [x] **Step 6.1 - 建立 IntegrationTest 專案骨架**
  - 參考 `JobBank1111.Job.IntegrationTest` 結構
  - 安裝套件：
    - `Reqnroll.xUnit`
    - `Testcontainers.PostgreSql`（PostgreSQL 容器）
    - `Microsoft.AspNetCore.Mvc.Testing`（WebApplicationFactory）
    - `FluentAssertions`
    - `xunit`
  - 建立 `BaseStep.cs`（BeforeTestRun 建立 PostgreSQL 容器、啟動 TestServer）
  - 建立 `TestServer.cs`（繼承 WebApplicationFactory，注入測試環境變數）
  - 建立 `TestAssistant.cs`（環境變數設定工具）

- [x] **Step 6.2 - 建立共用 BDD Step（中文）**
  - `Given 初始化測試伺服器`
  - `Given 調用端已準備 Header 參數`
  - `When 調用端發送 "{method}" 請求至 "{url}"`
  - `Then 預期得到 HttpStatusCode 為 "{code}"`
  - `Then 預期回傳內容為`
  - `Then 預期回傳內容中路徑 "{path}" 的"{type}" "{value}"`

---

### Phase 7：驗證與收尾

- [x] **Step 7.1 - 端對端流程驗證**
  - SPA PKCE 完整流程：取得 Code → 換 Token → Refresh Token Rotation
  - MVC Client 完整流程：登入 → Cookie Session → 跨頁 SSO → 登出
  - WebAPI Client：Bearer Token 驗證 → Access Token 過期 → Refresh
  - Playwright E2E 全部情境（A～E）綠燈

- [x] **Step 7.2 - 安全性檢核**
  - Token 不得明文落地（無 localStorage、無 URL fragment 含 token）
  - Refresh Token Rotation 正確執行（舊 token 失效）
  - PKCE code_verifier 正確驗證
  - Social Provider ClientSecret 不可寫入程式碼，僅讀環境變數

- [x] **Step 7.3 - 文件與 tree.md 更新**
  - 補齊 `doc/openapi.yml`
  - 更新 `tree.md` 確保結構正確
  - 移動計畫書至 `.archive/`

---

## 注意事項

1. **Instagram 登入**：底層走 Meta Facebook Login（同一 app_id），UX 顯示「以 Instagram 繼續」
2. **Threads Token 有效期**：Threads short-lived token 有效期 1 小時，須換 long-lived token（60 天）
3. **Social Provider 憑證**：所有 ClientId / ClientSecret 從環境變數讀取，絕不硬寫進程式碼
4. **禁止 Mock DB**：整合測試一律用 Testcontainers PostgreSQL 容器
5. **Playwright E2E 測試**：Social Login（Google、Line 等）需要真實帳號，建議使用測試專用帳號，憑證存放於 `~/.claude/creds/.creds`
6. **跨站 SSO**：MVC Client 與 WebAPI Client 各自啟動，Playwright 測試同時驅動兩個站台
