# Token 交換端點開發者操作指南

本功能提供客戶端應用程式依循 OAuth 2.0 / OpenID Connect 標準通訊協定，透過授權碼、Client 憑證或 Refresh Token 向認證伺服器換取 Access Token。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **探索 OIDC Discovery 文件**
   發送 GET 請求取得認證伺服器的端點設定清單。
   - **HTTP Method**: `GET`
   - **URL**: `https://auth.example.com/.well-known/openid-configuration`

   ```bash
   curl -X GET https://auth.example.com/.well-known/openid-configuration
   ```
   回應將包含 `token_endpoint` (`https://auth.example.com/connect/token`)、`authorization_endpoint` 與 `userinfo_endpoint`。

2. **使用 Client Credentials 授權模式取得 Access Token**
   後端服務對服務（M2M）通訊時，透過 Client 識別碼與金鑰換取應用程式權限 Token。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/connect/token`
   - **Content-Type**: `application/x-www-form-urlencoded`

   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=client_credentials&client_id=webapi-client&client_secret=webapi-client-secret&scope=api"
   ```
   回應結果範例：
   ```json
   {
     "token_type": "Bearer",
     "access_token": "eyJhbGciOiJSUzI1NiIs...",
     "expires_in": 3600
   }
   ```

3. **使用 Refresh Token 換發新的 Access Token**
   在 Access Token 即將過期時，使用持有的 Refresh Token 延續存取權限。
   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=refresh_token&refresh_token=YOUR_REFRESH_TOKEN&client_id=mvc-client&client_secret=mvc-client-secret"
   ```

## 常見錯誤與例外狀況

- **使用無效或不存在的 client_id** → 回傳 `401 Unauthorized`，Body 為 `{"error": "invalid_client"}` → 傳入的客戶端帳號不存在或已被停用 → 檢查傳入之 `client_id` 與 `client_secret` 是否與 Developer Portal 註冊資訊一致。
- **使用無效或已過期的 Refresh Token** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_grant"}` → Refresh Token 已過期、已被撤銷或已經被使用換發過一次（Token 輪替保護） → 引導使用者重新執行授權碼流程（Authorization Code Flow）重新登入授權。
- **授權碼 (Code) 換 Token 時缺少或帶錯 code_verifier** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_grant"}` → PKCE 驗證失敗，SHA256 計算值與授權發起時傳入的 `code_challenge` 不一致 → 檢查客戶端 PKCE 生成演算法與 code_verifier 儲存狀態。

## 你現在可以做的下一步

- 攜帶換得的 Bearer Token 呼叫受保護資源：參閱 [Bearer Token 驗證指南](../bearer-authentication/bearer-authentication-for-developers.md)
- 執行 Token 整合測試：`dotnet test --filter "FullyQualifiedName~Token交換"`
