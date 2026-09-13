# 開發者帳號與沙盒工具操作指南

本功能提供一般會員自助開通為開發者身分（Developer Profile），並使用內建沙盒工具組裝與生成 OAuth / OIDC 授權測試網址，以及驗證 Client 憑證。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **開通啟用開發者身分**
   呼叫 `POST /api/v1/developer/account/enable` 提交組織或團隊名稱與聯絡信箱完成開通。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/account/enable`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/account/enable \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
     -d '{
       "organizationName": "Acme Software Inc",
       "contactEmail": "developer@acme.com",
       "acceptAgreement": true
     }'
   ```
   回應範例：
   ```json
   {
     "isDeveloperEnabled": true,
     "organizationName": "Acme Software Inc",
     "contactEmail": "developer@acme.com",
     "registeredAt": "2026-09-13T04:00:00+00:00"
   }
   ```

2. **查詢開發者身分狀態**
   呼叫 `GET /api/v1/developer/account/status` 確認當前開發者身分資訊。
   - **HTTP Method**: `GET`
   - **URL**: `https://developer.example.com/api/v1/developer/account/status`
   - **Header**: `Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://developer.example.com/api/v1/developer/account/status \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```

3. **使用沙盒工具組裝授權網址**
   發送請求至沙盒端點，傳入 `clientId`、`redirectUri`、`scope` 以及呼叫端計算之 `codeChallenge` 等參數，系統將自動拼接並格式化為合法的 `/connect/authorize` 請求 URL。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/sandbox/generate-authorize-url`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/sandbox/generate-authorize-url \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>" \
     -d '{
       "clientId": "test_client_id",
       "redirectUri": "https://myapp.com/callback",
       "scope": "openid profile email",
       "codeChallenge": "E9Melhoa2OwvFrGMTJguCH5rtx64LxU93US3rKR3b7U",
       "codeChallengeMethod": "S256",
       "state": "xyz123"
     }'
   ```
   回應範例：
   ```json
   {
     "authorizeUrl": "https://auth.example.com/connect/authorize?response_type=code&client_id=test_client_id&redirect_uri=https%3A%2F%2Fmyapp.com%2Fcallback&scope=openid+profile+email&code_challenge=E9Melhoa2OwvFrGMTJguCH5rtx64LxU93US3rKR3b7U&code_challenge_method=S256&state=xyz123"
   }
   ```

4. **使用沙盒驗證 Client 憑證**
   呼叫 `POST /api/v1/developer/sandbox/validate-credentials` 測試 ClientId 與 ClientSecret 是否相符（包含雙金鑰輪替過渡期檢驗）。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/sandbox/validate-credentials`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/sandbox/validate-credentials \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>" \
     -d '{
       "clientId": "test_client_id",
       "clientSecret": "Secret123!"
     }'
   ```
   回應 `200 OK` (`{"isValid": true}`)。

## 常見錯誤與例外狀況

- **生成授權網址時缺少 ClientId 或 RedirectUri** → 回傳 `400 Bad Request` (`{"error": "ClientId 與 RedirectUri 為必填欄位"}`)。
- **驗證憑證時 ClientId 不存在或金鑰錯誤** → 回傳 `400 Bad Request` (`{"isValid": false, "error": "invalid_client", "errorDescription": "..."}`)。
- **未登入直接存取沙盒工具** → 回傳 `401 Unauthorized`。

## 你現在可以做的下一步

- 執行開發者帳號與沙盒整合測試：
  ```bash
  dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~開發者帳號與沙盒"
  ```
- 參閱雙金鑰輪替機制：[雙金鑰輪替與零停機過渡指南](../secret-rotation/secret-rotation-for-developers.md)
- 參閱開發者權限與安全防護機制：[安全性與權限控制指南](../developer-security-access-control/developer-security-access-control-for-developers.md)
