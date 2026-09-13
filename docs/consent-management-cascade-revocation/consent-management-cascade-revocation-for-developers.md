# 授權管理與級聯撤銷開發者操作指南

本功能提供使用者查詢已授權的第三方應用程式清單，並在撤銷授權時執行級聯清理（Cascade Revocation），一併作廢與該授權關聯的所有 Refresh Token 與流通 Access Token。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **查詢已授權應用程式清單**
   呼叫 `GET /api/v1/consents` 取得使用者當前已授權之 Client 清單。
   - **HTTP Method**: `GET`
   - **URL**: `https://account.example.com/api/v1/consents`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://account.example.com/api/v1/consents \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應範例：
   ```json
   [
     {
       "clientId": "mvc-client",
       "clientName": "MVC Client Application",
       "scopes": ["openid", "profile", "api"],
       "createdAt": "2026-09-01T08:00:00Z"
     }
   ]
   ```

2. **撤銷指定應用程式授權 (級聯作廢)**
   發送 DELETE 請求移除對指定 `clientId` 的授權記錄。
   - **HTTP Method**: `DELETE`
   - **URL**: `https://account.example.com/api/v1/consents/mvc-client`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X DELETE https://account.example.com/api/v1/consents/mvc-client \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應狀態碼為 `204 No Content`。伺服器底層將同步作廢 OpenIddict 中的 Authorization 實體及其關聯的所有 Token。

## 常見錯誤與例外狀況

- **嘗試撤銷其他使用者的授權 (IDOR 攻擊)** → 回傳 `403 Forbidden` 或 `404 Not Found` → 系統實施使用者範圍防護，禁止跨帳號撤銷 → 確保僅能操作當前 Token 識別身分名下之授權。
- **撤銷後第三方 App 仍使用舊 Refresh Token 換發** → `/connect/token` 端點回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 級聯機制已成功清除伺服器端 Token 記錄 → 第三方 App 必須引導使用者重新登入。
- **未帶 Token 呼叫授權管理 API** → 回傳 `401 Unauthorized` → 缺少有效身分憑證 → 需先完成使用者登入。

## 你現在可以做的下一步

- 檢視後端 Token 驗證與安全性機制：參閱 [Token 驗證與安全性指南](../token-validation-security/token-validation-security-for-developers.md)
- 執行級聯撤銷整合測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~授權管理與級聯撤銷"`
