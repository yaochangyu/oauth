# 第三方應用審核流開發者與維運指南

本功能提供系統管理員對開發者提交之第三方應用程式進行狀態機治理，包含審核核准（Approve）、駁回（Reject）、違規停用（Suspend）以及解除停用（Unsuspend），並支援應用程式 CRUD 管理。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **取得待審核應用清單**
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/app-review/pending`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/app-review/pending \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```

2. **核准應用程式 (InReview -> Approved)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/app-review/{id}/approve`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/app-review/app_5e97bdf0/approve \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `200 OK`，App 狀態轉為 `Approved`。

3. **駁回應用程式 (InReview -> Rejected)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/app-review/{id}/reject`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/app-review/app_5e97bdf0/reject \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{ "reason": "Redirect URI 格式不符合安全規範" }'
   ```

4. **強制停用違規應用並吊銷流通 Token (Approved -> Suspended)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/app-review/{id}/suspend`
   - 伺服器將 App 狀態轉為 `Suspended`，並級聯吊銷該應用名下所有有效之 Access/Refresh Tokens。

5. **恢復已停用應用 (Suspended -> Approved)**
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/app-review/{id}/unsuspend`

6. **管理員建立、編輯與刪除應用**
   支援 `POST /api/v1/admin/app-review/applications` 建立、`PUT` 編輯與 `DELETE` 刪除應用。

## 常見錯誤與例外狀況

- **對非 InReview 狀態執行核准或駁回** → 回傳 `400 Bad Request` (`{"message": "只有處於 InReview 狀態的應用程式才可執行審核"}`) → 違反應有狀態機轉換規則 → 僅針對待審核狀態之應用程式進行審查。
- **對非 Approved 狀態執行停用** → 回傳 `400 Bad Request` → 僅允許停用已上線之應用。
- **對非 Suspended 狀態執行恢復** → 回傳 `400 Bad Request` → 僅允許恢復已停用之應用。

## 你現在可以做的下一步

- 進行使用者違規帳號管理：參閱 [使用者狀態管理指南](../user-status-management/user-status-management-for-developers.md)
- 執行第三方應用審核流測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~第三方應用審核流"`
