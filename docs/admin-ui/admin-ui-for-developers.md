# AdminUI 管理介面維運操作指南

本功能提供系統管理員透過 Web 管理介面（Blazor Server / MudBlazor）進行後台儀表板檢視、使用者帳號與角色配置、OAuth 應用程式管理以及 Scopes 授權範圍維護。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

以下為 AdminUI 管理介面的標準操作步驟與對應之畫面元素（基於 Playwright E2E 測試定義）：

1. **管理員登入與首頁儀表板 (Dashboard)**
   - **導向路徑**: `/`
   - **操作元素**:
     - 帳號輸入框：`input[name='userName']`
     - 密碼輸入框：`input[type='password']`
     - 送出按鈕：`button[type='submit']`
   - **驗證畫面**: 登入成功後首頁顯示 `h5:has-text('Dashboard')` 標題，並提供 `Users`、`Roles`、`Scopes`、`Applications` 導覽連結。

2. **使用者帳號管理與編輯 (Users Management)**
   - **導向路徑**: `/users`
   - **操作元素**:
     - 搜尋輸入框：`input[placeholder*='Search']`（支援 Enter 觸發搜尋）
     - 編輯按鈕：表格每筆資料之操作按鈕 `table .mud-icon-button`
   - **使用者編輯頁**:
     - 導向 `/users/edit/{id}`
     - 使用者名稱欄位：`input[aria-label='Username']`
     - 角色核取方塊：`.mud-checkbox`（例如勾選 `admin` 角色）

3. **角色權限管理 (Roles Management & CRUD)**
   - **導向路徑**: `/roles`
   - **新增角色**:
     - 新角色名稱輸入框：`input[aria-label='New Role Name']`
     - 新增按鈕：`button:has-text('Add Role')`
     - 成功後角色即時出現在列表表格中。
   - **刪除角色**:
     - 點擊目標角色列之刪除按鈕：`tr:has(td:has-text('{roleName}')) .mud-icon-button`
     - 確認對話框：`.mud-message-box button:has-text('Delete')`
     - 刪除後該角色自列表移除。

4. **OAuth 應用程式管理 (Applications Management)**
   - **導向路徑**: `/applications`
   - **操作元素**:
     - 頁面標題：`h5:has-text('OAuth Applications')`
     - 搜尋輸入框：`input[placeholder*='Search']`
     - 編輯按鈕：`tr:has(td:has-text('{clientId}')) .mud-icon-button`
   - **應用程式編輯頁**:
     - 導向 `/applications/edit/{id}`
     - Client Id 欄位：`input[aria-label='Client Id']`（顯示如 `mvc-client`）

5. **授權範圍管理 (Scopes Management)**
   - **導向路徑**: `/scopes`
   - **操作元素**:
     - Scope 列表表格：顯示系統內建與自訂 Scopes（如 `api`）
     - 編輯按鈕：`tr:has(td:has-text('{scopeName}')) .mud-icon-button`
   - **Scope 編輯頁**:
     - 導向 `/scopes/edit/{id}`
     - Scope Name 欄位：`input[aria-label='Name']`（顯示如 `api`）

## 常見錯誤與例外狀況

本介面完整涵蓋 E2E 測試（`AdminUI管理介面.feature`）之 17 項場景與邊界驗證：

1. **Dashboard 導覽完整性驗證** → 首頁正確渲染 Dashboard 標題與 Users、Roles、Scopes、Applications 導覽列。
2. **Users 列表種子資料確認** → 開啟 `/users` 頁面，列表正確載入且包含預設 `admin` 帳號。
3. **Users 搜尋功能** → 於搜尋框輸入 `admin`，表格精確過濾顯示相符帳號。
4. **Users 搜尋不存在之帳號** → 搜尋 `nonexistent-user-xyz` 時，表格顯示為空列表（`tbody tr` 為 0）。
5. **Users 編輯頁跳轉** → 點擊使用者列之編輯按鈕，SignalR 路由平滑跳轉至 `/users/edit/{id}`。
6. **Users 編輯頁資料繫結** → 編輯頁正確顯示 Username（如 `admin`）。
7. **Users 角色核取狀態** → 編輯頁中該用戶所屬之角色方塊（如 `admin`）呈現已勾選狀態。
8. **Roles 列表種子資料確認** → 開啟 `/roles` 頁面，列表顯示預設 `admin` 角色。
9. **Roles 新增角色** → 輸入新角色名稱（如 `test-role`）點擊新增，列表立即呈現新增之角色項目。
10. **Roles 新增重複角色防呆** → 新增已存在之角色名稱（如 `admin`）時，系統彈出錯誤提示通知條（`.mud-snackbar.mud-alert-filled-error`）。
11. **Roles 刪除角色** → 點擊刪除並於二次確認對話框（`.mud-message-box`）確認後，角色從列表即時移除。
12. **Applications 列表檢視** → 開啟 `/applications` 顯示 `OAuth Applications` 標題與現有 Clients（如 `mvc-client`）。
13. **Applications 搜尋不存在之 Client** → 搜尋 `nonexistent-client-xyz` 時，表格過濾顯示空列表。
14. **Applications 編輯頁跳轉與欄位顯示** → 點擊 Client 編輯按鈕跳轉至 `/applications/edit/{id}`，Client Id 欄位完整顯示。
15. **Scopes 列表檢視** → 開啟 `/scopes` 正確列出包含 `api` 等授權範圍。
16. **Scopes 編輯頁跳轉與欄位顯示** → 點擊 Scope 編輯按鈕跳轉至 `/scopes/edit/{id}`，Scope Name 欄位正確顯示。
17. **未授權存取保護** → 非管理員身分無法進入 `/users`、`/roles`、`/applications`、`/scopes` 等管理路由。

## 你現在可以做的下一步

- 執行 AdminUI Playwright E2E 自動化測試：
  ```bash
  dotnet test test/OAuth.AuthServer.WebUI.E2E --filter "FullyQualifiedName~AdminUI"
  ```
- 參閱後端應用審核 API：[第三方應用審核流指南](../app-review-workflow/app-review-workflow-for-developers.md)
- 參閱使用者狀態管理 API：[使用者狀態管理指南](../user-status-management/user-status-management-for-developers.md)
