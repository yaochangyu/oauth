# 開發者帳號與沙盒工具操作指南

本功能提供一般會員自助開通為開發者身分，並使用內建沙盒工具自動計算 PKCE 參數與生成授權測試網址。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **開通啟用開發者身分**
   呼叫 `POST /api/v1/developer/account/enable` 提交組織或團隊名稱完成身分開通。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/account/enable`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/account/enable \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
     -d '{
       "organizationName": "Acme Software Ltd.",
       "contactEmail": "dev@acme.example.com"
     }'
   ```
   回應 `200 OK`，身分狀態持久化為 `Active`。

2. **查詢開發者身分狀態**
   呼叫 `GET /api/v1/developer/account/status` 確認開通結果。
   ```bash
   curl -X GET https://developer.example.com/api/v1/developer/account/status \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```

3. **使用沙盒工具自動產生 PKCE 授權網址**
   發送請求至沙盒端點，系統將自動隨機產生 `code_verifier`，計算 SHA256 雜湊與 Base64Url `code_challenge` 並拼裝為完整 URL。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/sandbox/generate-authorize-url`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/sandbox/generate-authorize-url \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>" \
     -d '{
       "clientId": "test-spa-client",
       "redirectUri": "https://localhost:5001/callback",
       "scopes": ["openid", "profile", "api"]
     }'
   ```
   回應範例：
   ```json
   {
     "authorizeUrl": "https://auth.example.com/connect/authorize?client_id=test-spa-client&response_type=code&scope=openid%20profile%20api&redirect_uri=https%3A%2F%2Flocalhost%3A5001%2Fcallback&code_challenge=abc12345...&code_challenge_method=S256",
     "codeVerifier": "random_generated_verifier_string_value...",
     "codeChallenge": "abc12345..."
   }
   ```

## 常見錯誤與例外狀況

- **重複呼叫啟用開發者身分** → 回傳 `200 OK` 並保持當前狀態 → 系統支援等冪性開通處理。
- **沙盒生成網址時傳入未註冊的 redirectUri** → 回傳 `400 Bad Request` → 傳入之跳轉網址未在該 Client 允許名單中 → 請傳入已登記之有效 Redirect URI。
- **未登入直接存取沙盒工具** → 回傳 `401 Unauthorized` → 需先完成使用者身分驗證。

## 你現在可以做的下一步

- 參閱開發者權限與安全防護機制：[安全性與權限控制指南](../developer-security-access-control/developer-security-access-control-for-developers.md)
- 執行開發者帳號與沙盒測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~開發者帳號與沙盒"`
