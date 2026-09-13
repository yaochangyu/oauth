# 應用程式生命週期與 PKCE 開發者操作指南

本功能提供已啟用開發者身分之工程師建立機密型 Web、公開型 SPA 或 Mobile 應用程式，自動強制配置 PKCE 防護，並支援查詢詳細資訊與提交上線審核。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **建立應用程式 (機密型 Web / 公開型 SPA / Mobile)**
   呼叫 `POST /api/v1/developer/apps` 建立新應用程式。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
     -d '{
       "displayName": "企業電商 Web 應用",
       "clientType": "confidential",
       "redirectUris": ["https://shop.example.com/signin-oidc"],
       "requirePkce": true,
       "allowedScopes": ["openid", "profile", "api"]
     }'
   ```
   回應範例（包含自動產生的 `clientId` 與 `clientSecret`）：
   ```json
   {
     "id": "app_5e97bdf0-47b2-4d2b-bb4e-6e2cbda83401",
     "clientId": "shop-web-client",
     "displayName": "企業電商 Web 應用",
     "clientType": "confidential",
     "status": "Draft",
     "requirePkce": true,
     "primarySecret": "sec_w98127391823..."
   }
   ```

2. **查詢開發者名下之應用程式清單**
   - **HTTP Method**: `GET`
   - **URL**: `https://developer.example.com/api/v1/developer/apps`
   ```bash
   curl -X GET https://developer.example.com/api/v1/developer/apps \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```

3. **送出上線審核申請 (Submit Review)**
   將應用程式狀態由 `Draft` 轉移至 `InReview`，等待平台管理員審查。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps/app_5e97bdf0-47b2-4d2b-bb4e-6e2cbda83401/submit-review`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps/app_5e97bdf0-47b2-4d2b-bb4e-6e2cbda83401/submit-review \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>" \
     -d '{ "notes": "已完成沙盒聯調，申請正式上線。" }'
   ```
   回應狀態為 `200 OK`，App 狀態變更為 `InReview`。

## 常見錯誤與例外狀況

- **未啟用開發者身分嘗試建立 App** → 回傳 `403 Forbidden` (`{"message": "尚未啟用開發者身分"}`) → 該帳號尚未開通開發者權限 → 先前往開發者帳號端點進行開通。
- **公開型客戶端 (SPA / Mobile) 未強制啟用 PKCE** → 伺服器拒絕建立或自動強制設定 `requirePkce=true` → 公開客戶端無安全密鑰儲存機制，依安全規範強制必須啟用 PKCE。
- **對已在審核中 (InReview) 或已核准 (Approved) 的 App 重複送審** → 回傳 `400 Bad Request` → 僅允許 `Draft` 或 `Rejected` 狀態送審 → 請等待當前審核結果或於退回後修改再送審。

## 你現在可以做的下一步

- 進行金鑰輪替維護：參閱 [雙金鑰輪替操作指南](../secret-rotation/secret-rotation-for-developers.md)
- 執行應用程式生命週期測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~應用程式生命週期"`
