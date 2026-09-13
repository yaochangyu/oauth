# 第三方應用審核流開發者與維運指南

本功能提供系統管理員對開發者提交之第三方應用程式進行狀態機治理，包含審核核准（Approve）、駁回（Reject）、違規停用（Suspend）以及恢復上線（Restore），並支援管理員端應用程式 CRUD 管理。

## 流程架構圖

- [檢視線性操作流程圖 (HTML)](./diagram.html)
- [檢視狀態機流轉圖 (HTML)](./diagram-2.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

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

## 你現在可以做的下一步

- 執行第三方應用審核流整合測試驗證：
  ```bash
  dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~第三方應用審核流"
  ```
- 前往 Admin UI 檢視應用程式列表與審核介面：參閱 [AdminUI 管理後台操作指南](../admin-ui/admin-ui-for-developers.md)
- 檢視違規帳號管理操作：參閱 [使用者狀態管理指南](../user-status-management/user-status-management-for-developers.md)
