# 授權同意 API 開發者操作指南

本功能提供前端認證介面（Vue 3 / Headless WebUI）在使用者登入後，向後端查詢請求授權的應用程式資訊，並送出使用者的同意（Accept）或拒絕（Deny）決策。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **查詢授權要求資訊**
   前端介面依據授權跳轉的 returnUrl 查詢目標 Client 名稱與要求請求的權限範圍（Scopes）。
   - **HTTP Method**: `GET`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client...`

   ```bash
   curl -X GET "https://auth.example.com/api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client%26response_type%3Dcode%26scope%3Dopenid%20profile%20api%26redirect_uri%3Dhttps%253A%252F%252Flocalhost%253A5101%252Fsignin-oidc" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE"
   ```
   回應結果範例：
   ```json
   {
     "clientName": "MVC Client Application",
     "scopes": ["openid", "profile", "api"]
   }
   ```

2. **使用者同意授權 (Accept)**
   使用者在畫面上點擊同意後，送出 Accept 請求並取得帶有授權碼（Authorization Code）的重導向網址。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-accept`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://auth.example.com/api/v1/connect/consent-accept \
     -H "Content-Type: application/json" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE" \
     -d '{
       "returnUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile%20api&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc"
     }'
   ```
   回應範例：
   ```json
   {
     "redirectUrl": "https://localhost:5101/signin-oidc?code=AUTH_CODE_VALUE&state=STATE_VALUE"
   }
   ```

3. **使用者拒絕授權 (Deny)**
   使用者點擊拒絕時，送出 Deny 請求，系統將產生帶有 `error=access_denied` 的跳轉網址。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-deny`

   ```bash
   curl -X POST https://auth.example.com/api/v1/connect/consent-deny \
     -H "Content-Type: application/json" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE" \
     -d '{
       "returnUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile%20api&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc"
     }'
   ```
   回應範例：
   ```json
   {
     "redirectUrl": "https://localhost:5101/signin-oidc?error=access_denied&error_description=The+user+denied+the+authorization+request."
   }
   ```

## 常見錯誤與例外狀況

- **未登入狀態下呼叫 Consent API** → 回傳 `401 Unauthorized` → 缺少有效的 Identity Session Cookie → 引導使用者先前往 `/login` 進行帳號身分驗證。
- **傳入無效或毀損的 returnUrl** → 回傳 `400 Bad Request`，Body 為 `{"message": "無效的 returnUrl"}` → returnUrl 無法解析為合法的 OpenIddict 授權請求 → 確保前端完整轉發從 `/connect/authorize` 帶來的 query 參數。
- **Client 設定為強制跳過同意（Implicit Consent / SkipConsent）** → `/connect/authorize` 直接回傳重導網址，無需呼叫 Consent API → 系統內建信任客戶端（如內部系統）無須使用者手動確認。

## 你現在可以做的下一步

- 整合同意頁面至 Vue 3 前端認證站：參閱 [同意頁面操作手冊](../consent-page-ui/consent-page-ui-for-users.md)
- 執行授權同意 API 整合測試：`dotnet test --filter "FullyQualifiedName~授權同意API"`
