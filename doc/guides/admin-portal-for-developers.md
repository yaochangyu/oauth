# Admin Portal 後台管理維運與開發者操作指南

本文件彙整系統管理員後台（Admin Portal）之管理與維運功能，包含管理員權限保護、第三方應用程式審核流程、使用者狀態與帳號鎖定管理、Scope 權限範圍與審計日誌管理、系統角色維護，以及 AdminUI 管理後台操作。

---

## 管理員權限保護操作指南

本功能定義 Admin WebAPI 核心防護機制，確保僅有具備 `Administrator` 角色之管理員 Token 方可存取系統管理端點，並拒絕未登入與一般使用者之存取。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./admin-authorization-protection/diagram.html)

### 主要操作流程

1. **以管理員身分取得 Access Token**
   管理員登入並換發含有 `role: Administrator` Claim 之 JWT Access Token。

2. **呼叫管理端點執行操作**
   在請求 Header 附帶管理員 Token。
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/scopes`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/scopes \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   驗證通過回傳 `200 OK` 與資源資料。

### 常見錯誤與例外狀況

- **未登入使用者存取管理端點 (未帶 Token)** → 回傳 `401 Unauthorized` → 缺少授權憑證 → 先行登入以取得 Token。
- **一般登入使用者存取管理端點 (無 Administrator 角色)** → 回傳 `403 Forbidden` → Token 雖有效但其 Claims 中缺少 `role: Administrator` → 拒絕非管理員之越權存取。
- **嘗試以未授權身分呼叫修改/刪除端點 (POST/PUT/DELETE)** → 回傳 `401 Unauthorized`（未登入）或 `403 Forbidden`（非管理員） → 修改端點受同等嚴格角色保護。

### 你現在可以做的下一步

- 進行第三方應用審核：參閱 [第三方應用審核流指南](#第三方應用審核流開發者與維運指南)
- 執行管理員權限保護測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~管理員權限保護"`

---

## 第三方應用審核流開發者與維運指南

本功能提供系統管理員對開發者提交之第三方應用程式進行狀態機治理，包含審核核准（Approve）、駁回（Reject）、違規停用（Suspend）以及恢復上線（Restore），並支援管理員端應用程式 CRUD 管理。

### 流程架構圖

- [檢視線性操作流程圖 (HTML)](./app-review-workflow/diagram.html)
- [檢視狀態機流轉圖 (HTML)](./app-review-workflow/diagram-2.html)

### 主要操作流程

1. **取得待審核應用清單**
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/pending`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/apps/pending \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應包含所有狀態為 `InReview` 的第三方應用清單。

2. **核准應用程式 (InReview -> Approved)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}/approve`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/apps/app-to-approve/approve \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，App 狀態轉為 `Approved`，並寫入審計日誌（AppApproved）。

3. **駁回應用程式 (InReview -> Rejected)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}/reject`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/apps/app-to-reject/reject \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{ "reason": "Redirect URI 不符合 HTTPS 規範，請修正後重新送審" }'
   ```
   回應 `200 OK`，App 狀態轉為 `Rejected`，記錄駁回原因並寫入審計日誌（AppRejected）。

4. **強制停用違規應用並吊銷流通 Token (Approved -> Suspended)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}/suspend`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/apps/violating-app/suspend \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   伺服器將 App 狀態轉為 `Suspended`，並級聯吊銷該應用名下所有流通的 Access/Refresh Tokens 與授權紀錄，寫入審計日誌（AppSuspended）。

5. **恢復已停用應用 (Suspended -> Approved)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}/restore`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/apps/suspended-app/restore \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，App 狀態由 `Suspended` 恢復為 `Approved`，寫入審計日誌（AppRestored）。

6. **管理員建立第三方應用 (Create App)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/apps`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/apps \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{
       "clientId": "created-app-01",
       "displayName": "Created App 01",
       "clientType": "confidential",
       "clientSecret": "Secret123!",
       "consentType": "explicit",
       "developer": "dev@test.com",
       "redirectUris": ["https://created.example.com/callback"],
       "postLogoutRedirectUris": ["https://created.example.com/logout"],
       "permissions": ["ept:authorization", "ept:token", "gt:authorization_code", "gt:refresh_token", "scp:api"]
     }'
   ```
   回應 `201 Created`，建立應用並寫入審計日誌（AppCreated）。

7. **管理員編輯第三方應用設定 (Update App)**
   - **HTTP Method**: `PUT`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}`
   - **Content-Type**: `application/json`

   ```bash
   curl -X PUT https://admin.example.com/api/v1/admin/apps/app-to-edit \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{
       "displayName": "Updated Display Name",
       "clientType": "public",
       "consentType": "implicit",
       "developer": "dev-updated@example.com",
       "redirectUris": ["https://updated.example.com/callback"],
       "postLogoutRedirectUris": ["https://updated.example.com/logout"],
       "permissions": ["ept:authorization", "gt:authorization_code", "rst:code"]
     }'
   ```
   回應 `200 OK`，更新應用設定並寫入審計日誌（AppUpdated）。

8. **管理員刪除第三方應用 (Delete App)**
   - **HTTP Method**: `DELETE`
   - **URL**: `https://admin.example.com/api/v1/admin/apps/{id}`

   ```bash
   curl -X DELETE https://admin.example.com/api/v1/admin/apps/app-to-delete \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，刪除該應用程式並寫入審計日誌（AppDeleted）。

### 常見錯誤與例外狀況

涵蓋整合測試（`第三方應用審核流.feature`）共 12 項情境與常見邊界驗證：

1. **對非 InReview 狀態執行核准（例如 Sandbox、Approved）** → 回傳 `400 Bad Request`（`{"message": "Cannot approve application with status '...'. Application must be in 'InReview' status."}`）→ 僅允許審核處於 `InReview` 狀態的應用程式。
2. **對非 InReview 狀態執行駁回（例如 Approved、Sandbox）** → 回傳 `400 Bad Request`（`{"message": "Cannot reject application with status '...'. Application must be in 'InReview' status."}`）→ 僅允許駁回處於 `InReview` 狀態的應用程式。
3. **駁回時缺少原因** → 回傳 `400 Bad Request`（`{"message": "Rejection reason is required"}`）→ 呼叫駁回 API 時必須提供明確的 `reason` 欄位。
4. **對非 Approved 狀態執行停用（例如 Sandbox、InReview）** → 回傳 `400 Bad Request`（`{"message": "Cannot suspend application with status '...'. Application must be in 'Approved' status."}`）→ 僅允許停用處於 `Approved` 狀態的已上線應用。
5. **對非 Suspended 狀態執行恢復（例如 InReview、Approved）** → 回傳 `400 Bad Request`（`{"message": "Cannot restore application with status '...'. Application must be in 'Suspended' status."}`）→ 僅允許恢復處於 `Suspended` 狀態的停用應用。
6. **查詢或操作不存在的應用 ID** → 回傳 `404 Not Found`（`{"message": "Application not found"}`）→ 找不到指定之 ClientId 或應用主鍵。
7. **建立應用時缺少 ClientId** → 回傳 `400 Bad Request`（`{"message": "ClientId is required"}`）→ 必填參數不可為空。
8. **建立應用時 ClientId 重複** → 回傳 `400 Bad Request`（`{"message": "Application with this ClientId already exists"}`）→ 系統不允許重複的 ClientId。
9. **刪除應用後再次查詢該應用** → 首次刪除回傳 `200 OK`，再次 `GET /api/v1/admin/apps/{id}` 回傳 `404 Not Found`（`{"message": "Application not found"}`）。
10. **編輯不存在之應用 ID** → 回傳 `404 Not Found`（`{"message": "Application not found"}`）。
11. **刪除不存在之應用 ID** → 回傳 `404 Not Found`（`{"message": "Application not found"}`）。
12. **未授權或非管理員帳號存取** → 回傳 `401 Unauthorized` 或 `403 Forbidden` → 端點受到 `[Authorize(Roles = "Administrator,admin")]` 保護。

### 你現在可以做的下一步

- 執行第三方應用審核流整合測試驗證：
  ```bash
  dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~第三方應用審核流"
  ```
- 前往 Admin UI 檢視應用程式列表與審核介面：參閱 [AdminUI 管理後台操作指南](#adminui-管理介面維運操作指南)
- 檢視違規帳號管理操作：參閱 [使用者狀態管理指南](#使用者狀態管理維運操作指南)

---

## 使用者狀態管理維運操作指南

本功能提供系統管理員針對違規用戶進行帳號凍結（Lockout）、解除凍結（Unlock）以及強制終止現行工作階段（Revoke Sessions）。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./user-status-management/diagram.html)

### 主要操作流程

1. **凍結違規用戶帳號 (Lockout)**
   呼叫 `PUT /api/v1/admin/users/{userId}/lockout` 鎖定帳號。
   - **HTTP Method**: `PUT`
   - **URL**: `https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/lockout`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X PUT https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/lockout \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，該使用者將無法再登入系統，並寫入審計日誌（UserLockedOut）。

2. **解除用戶帳號凍結 (Unlock)**
   呼叫 `PUT /api/v1/admin/users/{userId}/unlock`。
   - **HTTP Method**: `PUT`
   - **URL**: `https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/unlock`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X PUT https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/unlock \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，恢復正常登入權限，並寫入審計日誌（UserUnlocked）。

3. **強制登出使用者工作階段 (Revoke Sessions)**
   呼叫 `POST /api/v1/admin/users/{userId}/revoke-sessions`，立即更新該使用者的 SecurityStamp 並作廢所有發放中的 Token 與 Cookie。
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/revoke-sessions`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/users/usr_9b1deb4d/revoke-sessions \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應狀態為 `200 OK`，並寫入審計日誌（UserSessionsRevoked）。

### 常見錯誤與例外狀況

- **操作不存在的使用者 ID 或 Username** → 回傳 `404 Not Found` (`{"message": "User not found"}`) → 傳入的 userId 無匹配資料。
- **非 Administrator 或 admin 角色呼叫** → 回傳 `401 Unauthorized` 或 `403 Forbidden` → 缺少管理員角色權限。

### 你現在可以做的下一步

- 執行使用者狀態管理測試：
  ```bash
  dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~使用者狀態管理"
  ```
- 前往 Admin UI 進行使用者管理介面維運：參閱 [AdminUI 管理介面操作指南](#adminui-管理介面維運操作指南)
- 檢視系統權限與審計日誌：參閱 [Scope 與審計日誌管理指南](#scope-與審計日誌管理維運指南)

---

## Scope 與審計日誌管理維運指南

本功能提供管理員建立與維護全域 Scope 權限定義（可標記敏感權限 `isSensitive`），並查詢用戶 Consent 授權與撤銷之審計日誌（Audit Logs）。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./scope-and-audit-logs/diagram.html)

### 主要操作流程

1. **建立全域 Scope 並標記敏感權限**
   呼叫 `POST /api/v1/admin/scopes`。
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/scopes`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/scopes \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{
       "name": "payment.execute",
       "displayName": "執行線上支付",
       "description": "允許代表使用者扣款與執行付款交易",
       "isSensitive": true
     }'
   ```
   回應 `201 Created`。

2. **查詢全域 Scope 清單**
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/scopes`
   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/scopes \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```

3. **查詢授權審計日誌**
   呼叫 `GET /api/v1/admin/audit-logs/consents` 查詢歷史記錄。
   ```bash
   curl -X GET "https://admin.example.com/api/v1/admin/audit-logs/consents?userId=usr_9b1deb4d&page=1&pageSize=20" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   [
     {
       "id": "log_8a7d1123",
       "userId": "usr_9b1deb4d",
       "clientId": "mvc-client",
       "action": "Granted",
       "scopes": ["openid", "profile"],
       "ipAddress": "192.168.1.100",
       "timestamp": "2026-09-13T10:00:00Z"
     }
   ]
   ```

### 常見錯誤與例外狀況

- **重複建立相同名稱之 Scope** → 回傳 `400 Bad Request` (`{"message": "Scope 名稱已存在"}`) → Scope 名稱具備唯一性約束 → 使用不同名稱或更新既有 Scope。
- **查詢審計日誌時分頁參數無效 (page < 1)** → 回傳 `400 Bad Request` → 參數校驗失敗 → 傳入有效之 page 與 pageSize 參數。

### 你現在可以做的下一步

- 進行角色與權限管理：參閱 [角色管理指南](#系統角色管理維運操作指南)
- 執行 Scope 與審計日誌測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~Scope與審計日誌"`

---

## 系統角色管理維運操作指南

本功能提供管理員查詢系統角色清單、新增角色以及刪除指定角色，支援基於角色之存取控制（RBAC）體系。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./role-management/diagram.html)

### 主要操作流程

1. **查詢角色清單**
   呼叫 `GET /api/v1/admin/roles` 取得所有角色。
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/roles`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/roles \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   [
     { "id": "rol_01", "name": "Administrator" },
     { "id": "rol_02", "name": "Auditor" }
   ]
   ```

2. **新增自訂角色**
   呼叫 `POST /api/v1/admin/roles` 建立新角色。
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/roles`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/roles \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{ "roleName": "SupportEngineer" }'
   ```
   回應 `201 Created`。

3. **刪除角色**
   發送 DELETE 請求移除指定角色。
   - **HTTP Method**: `DELETE`
   - **URL**: `https://admin.example.com/api/v1/admin/roles/SupportEngineer`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X DELETE https://admin.example.com/api/v1/admin/roles/SupportEngineer \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `204 No Content`。

### 常見錯誤與例外狀況

- **重複新增已存在之角色** → 回傳 `400 Bad Request` (`{"message": "角色名稱已存在"}`) → 角色名稱具備唯一性約束 → 檢查是否已存在同名角色。
- **刪除不存在之角色** → 回傳 `404 Not Found` (`{"message": "找不到指定之角色"}`) → 目標角色不存在 → 確認角色名稱無誤。
- **未授權身分操作角色管理** → 回傳 `401 Unauthorized` 或 `403 Forbidden` → 僅限 Administrator 角色執行。

### 你現在可以做的下一步

- 驗證管理員權限保護機制：參閱 [管理員權限保護指南](#管理員權限保護操作指南)
- 執行角色管理測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~角色管理"`

---

## AdminUI 管理介面維運操作指南

本功能提供系統管理員透過 Web 管理介面（Blazor Server / MudBlazor）進行後台儀表板檢視、使用者帳號與角色配置、OAuth 應用程式管理以及 Scopes 授權範圍維護。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./admin-ui/diagram.html)

### 主要操作流程

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

### 常見錯誤與例外狀況

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

### 你現在可以做的下一步

- 執行 AdminUI Playwright E2E 自動化測試：
  ```bash
  dotnet test test/OAuth.AuthServer.WebUI.E2E --filter "FullyQualifiedName~AdminUI"
  ```
- 參閱後端應用審核 API：[第三方應用審核流指南](#第三方應用審核流開發者與維運指南)
- 參閱使用者狀態管理 API：[使用者狀態管理指南](#使用者狀態管理維運操作指南)
