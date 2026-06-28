# OAuth.Clients.PlaywrightTest 建立計畫

## 目標

新增 `OAuth.Clients.PlaywrightTest` 測試專案，結構參考 `OAuth.AuthServer.IntegrationTest`，
將所有 Client 端 E2E 測試改寫為 BDD（Reqnroll + Playwright）：
- 同意頁面（從 `OAuth.E2E.WebwrightTest` 搬移）
- Admin UI 管理介面（從 `AdminUITests.cs` 改寫）
- MVC Client 驗證流程（從 `MvcClientTests.cs` 改寫）
- SPA Host 驗證流程（從 `SpaHostTests.cs` 改寫）

同時復原 `OAuth.E2E.WebwrightTest` 這次新增的異動。

---

## 被測目標物（SUT）與服務清單

| 服務 | Port | 用途 |
|---|---|---|
| PostgreSQL | Testcontainer | 所有服務的資料庫 |
| AuthServer | 7001 | OIDC 授權核心，所有測試都需要 |
| Admin UI | 7002 | Admin UI 管理介面測試 |
| MVC Client | 5101 | Consent + MVC 驗證流程測試 |
| WebAPI Client | 5102 | 未來 API 流程測試用 |
| SPA Host | 5200 | SPA 驗證流程測試 |

所有服務於 `[BeforeTestRun]` 一次啟動，`[AfterTestRun]` 統一清理。

---

## 新專案結構

```
test/OAuth.Clients.PlaywrightTest/
├── OAuth.Clients.PlaywrightTest.csproj
├── TestSettings.cs              # URL / 帳密設定（環境變數優先）
├── PlaywrightBaseStep.cs        # [BeforeTestRun]/[AfterTestRun]（服務生命週期）
│                                # [BeforeScenario]/[AfterScenario]（瀏覽器建立/釋放）
│                                # 共用 Step（開啟瀏覽器、輸入帳密登入）
├── _01_AdminUI/
│   ├── AdminUI管理介面.feature
│   └── AdminUI管理介面Step.cs
├── _02_MvcClient/
│   ├── MvcClient驗證流程.feature
│   └── MvcClient驗證流程Step.cs
├── _03_SpaHost/
│   ├── SpaHost驗證流程.feature
│   └── SpaHost驗證流程Step.cs
└── _04_Consent/
    ├── 同意頁面.feature           # 搬移自 OAuth.E2E.WebwrightTest
    └── 同意頁面Step.cs            # 搬移並調整（移除自身 [BeforeScenario]，改用共用）
```

---

## 步驟

- [ ] **步驟 1：建立 `.csproj`**
  - 路徑：`test/OAuth.Clients.PlaywrightTest/`
  - 套件：`Reqnroll.xUnit`、`Microsoft.Playwright`、`Testcontainers.PostgreSql`、
    `xunit`、`xunit.runner.visualstudio`、`Microsoft.NET.Test.Sdk`、`coverlet.collector`
  - 不需要 project reference（透過瀏覽器測試）
  - 加入 `OAuth.slnx` 的 `/test/` folder

- [ ] **步驟 2：新增 `TestSettings.cs`**
  - 複製 `OAuth.E2E.WebwrightTest/TestSettings.cs`，改 namespace

- [ ] **步驟 3：新增 `PlaywrightBaseStep.cs`**
  - `[assembly: CollectionBehavior(DisableTestParallelization = true)]`
  - `[Binding]` 類別包含：
    - `[BeforeTestRun]`：啟動 PostgreSQL TestContainer → migration → 啟動 6 個服務 → 健康檢查
    - `[AfterTestRun]`：停止所有服務 + container
    - `[BeforeScenario]`：建立 Playwright + Chromium browser + context（IgnoreHTTPSErrors）+ page，存入 `ScenarioContext`
    - `[AfterScenario]`：釋放 browser + playwright
    - `[Given("開啟全新的瀏覽器視窗")]`：no-op（BeforeScenario 已處理）
    - `[When("使用者輸入帳號 {string} 密碼 {string} 登入")]`：填表單、送出、等待 DOMContentLoaded（共用 login step）

- [ ] **步驟 4：新增 Admin UI 測試（BDD 改寫）**
  - `_01_AdminUI/AdminUI管理介面.feature`：
    - Background: Given 已登入 Admin UI
    - Scenario: 首頁顯示 Dashboard
    - Scenario: Users 列表顯示 admin 帳號
    - Scenario: Users 搜尋 admin 可找到結果
    - Scenario: Users 點擊編輯跳轉編輯頁
    - Scenario: Roles 列表顯示 admin 角色
    - Scenario: Applications 列表顯示 seeded 應用程式
    - Scenario: Scopes 列表顯示 api scope
  - `_01_AdminUI/AdminUI管理介面Step.cs`：實作上述 Steps（從 ScenarioContext 取 IPage）

- [ ] **步驟 5：新增 MVC Client 測試（BDD 改寫）**
  - `_02_MvcClient/MvcClient驗證流程.feature`：
    - Background: Given 開啟全新的瀏覽器視窗
    - Scenario: 登入成功後停在 MVC Client 不發生 OIDC 迴圈
    - Scenario: 個人資料頁顯示歡迎文字
    - Scenario: 個人資料包含 admin role claim
    - Scenario: Access Token 有值
  - `_02_MvcClient/MvcClient驗證流程Step.cs`

- [ ] **步驟 6：新增 SPA Host 測試（BDD 改寫）**
  - `_03_SpaHost/SpaHost驗證流程.feature`：
    - Background: Given 開啟全新的瀏覽器視窗
    - Scenario: 登入成功後停在 SPA Host 不發生 OIDC 迴圈
    - Scenario: 首頁顯示已登入狀態
    - Scenario: 個人資料顯示 Email
    - Scenario: Access Token 有效標記顯示
    - Scenario: Claims 表格包含 role 和 admin
  - `_03_SpaHost/SpaHost驗證流程Step.cs`

- [ ] **步驟 7：搬移 Consent 測試**
  - 複製 `_04_Consent/同意頁面.feature` → 新專案 `_04_Consent/`（改 namespace）
  - 複製 `_04_Consent/同意頁面Step.cs` → 新專案，移除其中的 `[BeforeScenario]`/`[AfterScenario]`（改由 `PlaywrightBaseStep.cs` 統一處理），調整 namespace

- [ ] **步驟 8：復原 `OAuth.E2E.WebwrightTest` 這次異動**
  - 刪除 `E2ETestRun.cs`
  - 移除 csproj 的 `Testcontainers.PostgreSql` 套件
  - 刪除 `_04_Consent/` 目錄（已搬移至新專案）
  - 確認 `OAuth.E2E.WebwrightTest` 仍可 build

- [ ] **步驟 9：build 並安裝 Playwright browsers**
  - `dotnet build test/OAuth.Clients.PlaywrightTest/`
  - `pwsh playwright.ps1 install chromium`（或 dotnet tool run playwright install）

- [ ] **步驟 10：執行新專案測試（紅燈確認）**
  - 因服務未啟動，預期測試連線失敗（正常的紅燈）
  - 確認 xUnit 有抓到所有 Scenario

- [ ] **步驟 11：更新 `tree.md`**

---

## 注意事項

- 每個 Scenario 使用獨立的 Playwright browser context（無狀態共享），各自執行登入流程
- Admin UI 的 Background 每個 Scenario 都需要執行登入（無法共用 session），這是正確的 BDD 行為
- `PlaywrightBaseStep.cs` 的 `[BeforeTestRun]` 啟動服務為一次性，各 Scenario 只建立 browser

---

## 完成後移至 `.archive/`
