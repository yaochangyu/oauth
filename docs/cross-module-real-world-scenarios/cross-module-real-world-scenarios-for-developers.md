# 跨模組真實場景整合開發者指南

本功能涵蓋 OAuth 2.0 / OIDC 完整生命週期的端到端跨模組整合流程：包含「帳號註冊 → 授權發起 → 登入與同意 → Code換Token → API存取 → Token換發 → 授權撤銷與級聯作廢」。

## 流程架構圖

[檢視跨模組端到端架構流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **新使用者註冊新帳號**
   呼叫註冊 API 建立使用者身分資料庫記錄。
   ```bash
   curl -X POST https://auth.example.com/api/v1/account/register \
     -H "Content-Type: application/json" \
     -d '{"email": "realuser@example.com", "password": "Password123!", "displayName": "真實測試員"}'
   ```

2. **第三方 App 發起 Authorization Code + PKCE 授權流程**
   客戶端產生 `code_verifier` 與 SHA256 雜湊之 `code_challenge`，並引導使用者前往授權端點。
   - **URL**: `https://auth.example.com/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile%20api%20offline_access&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&code_challenge=E9Melhoa2OwvFrGMTJguCH5rtx64LxPU6VSwnVZn_o8&code_challenge_method=S256`

3. **使用者登入並完成授權同意 (Consent)**
   使用者輸入密碼登入後，呼叫 `/api/v1/connect/consent-accept` 提交同意授權，取得包含 `code` 的跳轉網址。

4. **第三方 App 拿授權碼交換 Access Token 與 Refresh Token**
   後端發送 POST 請求至 `/connect/token` 換發 Token。
   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=authorization_code&code=AUTH_CODE&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&client_id=mvc-client&client_secret=mvc-client-secret&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
   ```

5. **攜帶 Bearer Token 呼叫受保護資源 API**
   第三方應用程式使用 Access Token 存取資源伺服器之 `/api/v1/me`。
   ```bash
   curl -X GET https://api.example.com/api/v1/me \
     -H "Authorization: Bearer ACCESS_TOKEN"
   ```

6. **使用 Refresh Token 續期換發 Token**
   當 Access Token 到期時，使用 Refresh Token 取得全新 Access Token。

7. **使用者在個人中心撤銷授權 (Cascade Revocation)**
   使用者主動解除該應用程式的授權，伺服器將級聯作廢該使用者發給該 Client 的所有流通 Token。
   ```bash
   curl -X DELETE https://account.example.com/api/v1/consents/mvc-client \
     -H "Authorization: Bearer USER_ACCESS_TOKEN"
   ```

8. **驗證已撤銷的 Refresh Token 無法再次換發**
   第三方 App 嘗試再次以舊 Refresh Token 換發時，將被伺服器嚴格拒絕。

## 常見錯誤與例外狀況

- **使用已被撤銷的 Refresh Token 換 Token** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 該授權已在帳號中心被使用者撤銷 → 清除本地快取之 Token，引導使用者重新進行授權。
- **授權碼 (Code) 重複使用 (Replay Attack)** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 授權碼僅限使用一次，重複使用將被系統視為重放攻擊並作廢相關 Token → 重新發起授權流程。
- **未具備必要 Scope 存取特定 API** → 資源伺服器回傳 `403 Forbidden` → Token 內宣告之 `scope` 不包含端點要求的權限名稱 → 在發起授權時加入正確的 scope 參數。

## 你現在可以做的下一步

- 執行端到端真實跨模組整合測試：`dotnet test --filter "FullyQualifiedName~跨模組真實場景"`
- 前往授權管理中心確認授權狀態：參閱 [授權管理與級聯撤銷指南](../consent-management-cascade-revocation/consent-management-cascade-revocation-for-developers.md)
