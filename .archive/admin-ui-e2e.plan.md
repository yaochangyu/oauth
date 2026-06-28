# Admin UI E2E 測試計畫

## 目標
用 Playwright 驗證 Admin UI 所有頁面功能正常，使用管理員帳號 admin/Admin@123456 登入。

## 前置條件
- AuthServer（https://localhost:7001）必須運行
- Admin UI（https://localhost:7002）必須運行
- DB 有 admin 帳號（AdminUserSeeder 會在 AuthServer 啟動時自動建立）

## 測試涵蓋頁面
- `/` Home
- `/users` 使用者列表
- `/users/edit/{id}` 使用者編輯
- `/roles` 角色列表
- `/applications` 應用程式列表
- `/applications/new` 新增應用程式
- `/scopes` Scope 列表
- `/scopes/new` 新增 Scope

## 實作步驟

- [x] **步驟 1：加入 Playwright 套件到 E2E 測試專案**
  - 安裝 `Microsoft.Playwright` NuGet 套件
  - 加入 `Microsoft.Playwright.MSTest` 或搭配 xunit 的 Playwright 套件
  - 執行 `playwright install` 安裝瀏覽器
  - 理由：目前 E2E 專案只有空的 placeholder，沒有 Playwright 相依性

- [x] **步驟 2：建立登入 Helper（OIDC 流程）**
  - 建立 `AdminUIFixture` 處理 OIDC 登入流程
  - 在 AuthServer 登入頁輸入 admin/Admin@123456
  - 儲存登入後的 browser context（避免每個測試都重新登入）
  - 理由：所有頁面測試都需要先完成 OIDC 登入，抽取為共用 fixture

- [x] **步驟 3：啟動服務（如果尚未運行）**
  - 確認 AuthServer 和 Admin UI 正在運行
  - 理由：E2E 測試針對真實服務進行

- [x] **步驟 4：實作 Home 頁面測試**
  - 驗證頁面載入正常，不顯示錯誤

- [x] **步驟 5：實作 Users 頁面測試**
  - 列表頁顯示使用者（至少有 admin）
  - 搜尋功能可用
  - 點擊編輯按鈕跳轉到編輯頁

- [x] **步驟 6：實作 Roles 頁面測試**
  - 列表顯示角色（至少有 admin role）

- [x] **步驟 7：實作 Applications 頁面測試**
  - 列表顯示應用程式（seeder 建立的 spa-client 等）
  - 點擊新增進入表單頁

- [x] **步驟 8：實作 Scopes 頁面測試**
  - 列表顯示 Scope（api）
  - 點擊新增進入表單頁

- [x] **步驟 9：Build 並執行測試**
  - `dotnet build`
  - `dotnet test`

## 完成條件
所有測試通過，沒有頁面渲染錯誤。

## 結果
✅ 10/10 測試全部通過（2026-06-28）

### 主要問題與修復
1. **OIDC 無限迴圈**：Admin UI 同時使用 `AddIdentity` 和 `AddAuthentication`，`AddIdentity` 設定了 `DefaultAuthenticateScheme = Identity.Application`，OIDC callback 用 `Cookies` scheme 簽入，兩者不一致造成每次 `/` 都發新的 OIDC challenge。改用 `AddIdentityCore`（只提供 UserManager/RoleManager，不設定 auth scheme）解決。
2. **Strict Mode**：`GetByText("admin")` 匹配到多個元素；改用 `GetByRole(AriaRole.Cell, ...)` 精確選擇。
3. **隱藏 Input**：Roles 頁面的 `input, input` 選擇器選到 antiforgery hidden input；改用 `input.mud-input-slot[type='text']`。
