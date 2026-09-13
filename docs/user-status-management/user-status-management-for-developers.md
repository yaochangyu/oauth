# 使用者狀態管理維運操作指南

本功能提供系統管理員針對違規用戶進行帳號凍結（Lockout）、解除凍結（Unlock）以及強制終止現行工作階段（Revoke Sessions）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **操作不存在的使用者 ID 或 Username** → 回傳 `404 Not Found` (`{"message": "User not found"}`) → 傳入的 userId 無匹配資料。
- **非 Administrator 或 admin 角色呼叫** → 回傳 `401 Unauthorized` 或 `403 Forbidden` → 缺少管理員角色權限。

## 你現在可以做的下一步

- 執行使用者狀態管理測試：
  ```bash
  dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~使用者狀態管理"
  ```
- 前往 Admin UI 進行使用者管理介面維運：參閱 [AdminUI 管理介面操作指南](../admin-ui/admin-ui-for-developers.md)
- 檢視系統權限與審計日誌：參閱 [Scope 與審計日誌管理指南](../scope-and-audit-logs/scope-and-audit-logs-for-developers.md)
