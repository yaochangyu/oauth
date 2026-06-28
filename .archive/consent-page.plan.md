# Consent Page 實作計畫

## 目標

當 OAuth client 的 `ConsentType` 設為 `explicit` 時，授權流程需顯示同意頁面讓用戶確認；`implicit` 則跳過同意直接授權。

## 流程

TDD：先寫測試（紅燈）→ 實作 → 確認綠燈

測試格式：BDD（Reqnroll + Playwright）

---

## 步驟

- [x] **步驟 1：E2E 專案加入 Reqnroll 相依**
  - 在 `test/OAuth.E2E.WebwrightTest/OAuth.E2E.WebwrightTest.csproj` 加入 `Reqnroll.xUnit` 套件
  - 確認 build 成功

- [x] **步驟 2：先寫 BDD E2E 測試（紅燈）**
  - 新增 `test/OAuth.E2E.WebwrightTest/_04_Consent/同意頁面.feature`：
    ```gherkin
    Feature: 同意頁面

    Background:
        Given 開啟全新的瀏覽器視窗

    Scenario: Explicit Client 觸發同意頁面
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面
        And 同意頁面應列出請求的 scopes

    Scenario: Implicit Client 不顯示同意頁面
        Given 使用者尚未登入
        When 使用者透過 "mvc-implicit" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 不應顯示同意頁面
        And 應直接完成授權跳轉
    ```
  - 新增 `_04_Consent/同意頁面Step.cs`：實作 Step（Playwright 操作）
  - 新增 `ConsentFixture.cs`：每個 Scenario 提供乾淨的未登入 Browser Context
  - 執行 `dotnet test --filter "Category=Consent"`，確認**紅燈（FAIL）**

- [x] **步驟 3：在 OpenIddictDataSeeder 新增 `mvc-implicit` client**
  - `ClientId = "mvc-implicit"`，`ConsentType = "implicit"`
  - Redirect URI 沿用 `https://localhost:5101/signin-oidc`（與 mvc-client 相同）
  - 僅 `create if not exists`，不覆蓋已存在資料

- [x] **步驟 4：實作同意頁面 Razor Page**
  - 新增 `src/AuthServer/OAuth.AuthServer.WebAPI/Pages/Connect/Consent.cshtml`
    - 顯示：client 的 DisplayName、請求的 scopes 清單
    - 按鈕：Accept、Deny
  - 新增 `Consent.cshtml.cs`：
    - GET：讀取 OpenIddict 授權請求資訊
    - POST Accept：在 session 記錄用戶已同意，redirect 回 `/connect/authorize`
    - POST Deny：回傳 OpenIddict 的 `access_denied` 錯誤
  - 執行 build，確認無編譯錯誤

- [x] **步驟 5：修改 AuthorizationController，加入 consent 判斷**
  - 注入 `IOpenIddictApplicationManager` 取得 client 的 `ConsentType`
  - 若 `ConsentType == "explicit"` 且 session 無同意記錄 → redirect 到 `/Connect/Consent`
  - 若 `ConsentType == "implicit"` → 維持現有行為，直接 SignIn
  - 執行 build，確認無編譯錯誤

- [x] **步驟 6：執行 E2E 測試，確認綠燈**
  - 4 個 Scenario 均 PASS（Explicit 顯示同意、用戶同意跳轉、用戶拒絕 access_denied、Implicit 跳過同意）

- [x] **步驟 7：確認現有測試無退步**
  - Integration test：11/11 PASS（修正 migration race condition + HTTPS scheme + invalid_client 401）
  - E2E consent：4/4 PASS

---

## 完成後移至 `.archive/`
