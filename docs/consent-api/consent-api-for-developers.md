# 授權同意 API 開發者操作指南

本功能提供前端認證介面（Vue 3 / Headless WebUI）在使用者登入後，向後端查詢請求授權的應用程式資訊，並送出使用者的同意（Accept）或拒絕（Deny）決策。系統透過 60 秒短期快取憑證機制將授權決策傳遞至授權處理管線。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **查詢授權要求資訊**
   前端介面依據授權跳轉的 returnUrl 查詢目標 Client 名稱與要求請求的權限範圍（Scopes）。
   - **HTTP Method**: `GET`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client...`
   - **Header**: `Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE`

   ```bash
   curl -X GET "https://auth.example.com/api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client%26response_type%3Dcode%26scope%3Dopenid%20profile%20api%26redirect_uri%3Dhttps%253A%252F%252Flocalhost%253A5101%252Fsignin-oidc" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE"
   ```
   回應結果範例：
   ```json
   {
     "clientId": "mvc-client",
     "clientDisplayName": "MVC Client Application",
     "scopes": ["openid", "profile", "api"],
     "returnUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid profile api&redirect_uri=https://localhost:5101/signin-oidc"
   }
   ```

2. **使用者同意授權 (Accept)**
   使用者在畫面上點擊同意後，送出 Accept 請求。後端將決策寫入 60 秒分散式快取（`consent:{token}` -> `granted:{clientId}`）並回傳帶有 `__ct` 參數的跳轉網址。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-accept`
   - **Content-Type**: `application/json`
   - **Header**: `Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE`

   ```bash
   curl -X POST https://auth.example.com/api/v1/connect/consent-accept \
     -H "Content-Type: application/json" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE" \
     -d '{
       "clientId": "mvc-client",
       "returnUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid profile api&redirect_uri=https://localhost:5101/signin-oidc"
     }'
   ```
   回應範例：
   ```json
   {
     "redirectUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid profile api&redirect_uri=https://localhost:5101/signin-oidc&__ct=4a2bc91e70d4408197e42d7653fa3812"
   }
   ```

3. **使用者拒絕授權 (Deny)**
   使用者點擊拒絕時，送出 Deny 請求。後端將決策寫入 60 秒分散式快取（`consent:{token}` -> `denied:{clientId}`）並回傳跳轉網址。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/connect/consent-deny`
   - **Content-Type**: `application/json`
   - **Header**: `Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE`

   ```bash
   curl -X POST https://auth.example.com/api/v1/connect/consent-deny \
     -H "Content-Type: application/json" \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE" \
     -d '{
       "clientId": "mvc-client",
       "returnUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid profile api&redirect_uri=https://localhost:5101/signin-oidc"
     }'
   ```
   回應範例：
   ```json
   {
     "redirectUrl": "/connect/authorize?client_id=mvc-client&response_type=code&scope=openid profile api&redirect_uri=https://localhost:5101/signin-oidc&__ct=9f8e7d6c5b4a302110abcdef12345678"
   }
   ```

## 常見錯誤與例外狀況

- **未登入狀態下呼叫 Consent API** → 回傳 `401 Unauthorized` → 缺少身分驗證 Session → 引導使用者先前往登入頁進行帳號身分驗證。
- **查詢時傳入無效或非法的 returnUrl** → `consent-info` 回傳 `400 Bad Request`（`{"message": "無效或非法的 returnUrl"}`）→ returnUrl 未通過開放重導向（Open Redirect）防護校驗。
- **查詢時 Client 不存在** → `consent-info` 回傳 `400 Bad Request`（`{"message": "找不到對應的第三方應用程式"}`）。
- **提交同意或拒絕時 returnUrl 非法** → `consent-accept` / `consent-deny` 回傳 `400 Bad Request`（`{"message": "非法跳轉目標"}`）。

## 你現在可以做的下一步

- 執行同意頁面與授權決策 E2E 測試：
  ```bash
  dotnet test test/OAuth.AuthServer.WebUI.E2E --filter "FullyQualifiedName~同意頁面"
  ```
- 參閱同意頁面前端實作與手冊：[同意頁面操作手冊](../consent-page-ui/consent-page-ui-for-users.md)
- 參閱已授權應用管理與撤銷：[授權管理與級聯撤銷指南](../consent-management-cascade-revocation/consent-management-cascade-revocation-for-developers.md)
