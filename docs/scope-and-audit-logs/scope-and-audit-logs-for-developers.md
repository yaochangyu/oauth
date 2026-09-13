# Scope 與審計日誌管理維運指南

本功能提供管理員建立與維護全域 Scope 權限定義（可標記敏感權限 `isSensitive`），並查詢用戶 Consent 授權與撤銷之審計日誌（Audit Logs）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **重複建立相同名稱之 Scope** → 回傳 `400 Bad Request` (`{"message": "Scope 名稱已存在"}`) → Scope 名稱具備唯一性約束 → 使用不同名稱或更新既有 Scope。
- **查詢審計日誌時分頁參數無效 (page < 1)** → 回傳 `400 Bad Request` → 參數校驗失敗 → 傳入有效之 page 與 pageSize 參數。

## 你現在可以做的下一步

- 進行角色與權限管理：參閱 [角色管理指南](../role-management/role-management-for-developers.md)
- 執行 Scope 與審計日誌測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~Scope與審計日誌"`
