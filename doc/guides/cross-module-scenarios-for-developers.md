# 跨模組真實場景整合開發者指南

本文件彙整跨多模組（AuthServer、Account Portal、Developer Portal、Admin Portal）之端到端真實情境整合與 WebUI Playwright E2E 自動化測試流程。

---

## 跨模組真實場景整合開發者指南

本功能涵蓋 OAuth 2.0 / OIDC 完整生命週期的端到端跨模組整合流程：包含「帳號註冊 → 授權發起 → 登入與同意 → Code換Token → API存取 → Token換發 → 授權撤銷與級聯作廢」。

### 流程架構圖

- [檢視跨模組端到端線性流程圖 (HTML)](./cross-module-real-world-scenarios/diagram.html)
- [檢視跨模組端到端循序圖 (HTML)](./cross-module-real-world-scenarios/diagram-2.html)

### 主要操作流程

1. **新使用者註冊新帳號**
   呼叫註冊 API 建立使用者身分資料庫記錄。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/account/register`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://auth.example.com/api/v1/account/register \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "scenario_user_01@example.com",
       "email": "scenario_user_01@example.com",
       "password": "Password123!",
       "displayName": "真實場景用戶一",
       "phoneNumber": "0912345678"
     }'
   ```
   回應 `201 Created`。

2. **第三方 App 發起 Authorization Code + PKCE 授權流程**
   客戶端產生 `code_verifier` 與 SHA256 雜湊之 `code_challenge`，並引導使用者前往授權端點。
   - **URL**: `https://auth.example.com/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile%20email&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256`

3. **使用者登入並完成授權同意 (Consent)**
   使用者以 `POST /api/v1/account/login` 完成登入後，呼叫 `/api/v1/connect/consent-accept` 提交同意授權，跟隨跳轉取得包含 `code` 的重導向 URL。

4. **第三方 App 拿授權碼交換 Access Token 與 Refresh Token**
   後端發送 POST 請求至 `/connect/token` 換發 Token。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/connect/token`
   - **Content-Type**: `application/x-www-form-urlencoded`

   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=authorization_code&client_id=mvc-client&client_secret=mvc-client-secret&code=AUTH_CODE&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk"
   ```
   回應 `200 OK`，取得 JWT 格式之 `access_token` 與 `refresh_token`。

5. **攜帶 Bearer Token 呼叫 UserInfo 或資源 API**
   第三方應用程式使用 Access Token 存取資源伺服器之 `/connect/userinfo` 或 `/api/v1/me`。
   ```bash
   curl -X GET https://auth.example.com/connect/userinfo \
     -H "Authorization: Bearer ACCESS_TOKEN"
   ```
   回應包含同意 Scope 對應之 Claims（`email`, `name` 等）。

6. **使用 Refresh Token 續期換發 Token**
   當 Access Token 到期時，使用 Refresh Token 取得全新 Access Token。
   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=refresh_token&client_id=mvc-client&client_secret=mvc-client-secret&refresh_token=REFRESH_TOKEN"
   ```

7. **使用者在個人中心撤銷授權 (Cascade Revocation)**
   使用者主動解除該應用程式的授權，伺服器將級聯作廢該使用者發給該 Client 的所有流通 Token。
   ```bash
   curl -X DELETE https://account.example.com/api/v1/account/consents/{authorizationId} \
     -H "Authorization: Bearer USER_ACCESS_TOKEN"
   ```
   回應 `204 No Content`。

8. **驗證已撤銷的 Refresh Token 無法再次換發**
   第三方 App 嘗試再次以舊 Refresh Token 換發時，將回傳 `400 Bad Request` (`{"error": "invalid_grant"}`)。

### 常見錯誤與例外狀況

- **使用已被撤銷的 Refresh Token 換 Token** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 該授權已在帳號中心被使用者撤銷。
- **使用者於 Consent 頁面拒絕授權 (Deny)** → 導向帶有 `error=access_denied` 的跳轉網址，不核發 Code。
- **請求超出核准範圍之 Scope** → 回傳 `400 Bad Request` (`{"error": "invalid_scope"}`)。
- **授權碼 (Code) 重複使用 (Replay Attack)** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`)。

### 你現在可以做的下一步

- 執行跨模組真實場景整合測試：
  ```bash
  dotnet test test/OAuth.AuthServer.IntegrationTest --filter "FullyQualifiedName~跨模組真實場景"
  ```
- 前往授權管理中心確認授權狀態：參閱 [授權管理與級聯撤銷指南](./account-portal-for-developers.md#授權管理與級聯撤銷開發者操作指南)
- 參閱真實情境使用者操作指南：[跨模組真實情境操作手冊 (使用者版)](./cross-module-scenarios-for-users.md#跨模組真實情境操作手冊)

---

## WebUI 跨模組真實場景 E2E 開發者指南

本功能定義透過 Playwright E2E 驅動之全鏈路整合場景，涵蓋註冊新會員並登入第三方 App、授權碼流程同意跳轉、拒絕跳轉、兩階段登入（PermanentAuthorization 略過同意頁）以及 SPA 透過 PKCE 登入之完整自動化流程。

### 流程架構圖

[檢視全鏈路 E2E 架構圖 (HTML)](./webui-cross-module-scenarios/diagram.html)

### 主要操作流程

1. **場景一：真實使用者註冊新會員並透過第三方 App 成功登入**
   - 瀏覽器開啟第三方 App 觸發 OIDC 導向至認證站 `/login`。
   - 點擊「註冊」填寫真實信箱密碼送出。
   - 註冊成功後返回登入頁輸入新帳密登入，自動跳轉同意頁並授權成功返回第三方 App。

2. **場景三：第三方 App 發起授權碼流程，用戶同意後跳轉**
   - 發起 `/connect/authorize` 請求帶 `response_type=code`。
   - 進入 `/consent` 頁面顯示請求之 Scopes。
   - 點擊「同意」，認證站簽發 code 並 302 重導向至 `redirect_uri`，第三方 App 換取 Access Token 完成登入。

3. **場景四：使用者拒絕同意導回第三方並顯示授權失敗**
   - 於 Consent 頁面點擊「拒絕 (Deny)」。
   - 認證站重導向回第三方並附加 `error=access_denied`。
   - 第三方 App 正確捕捉錯誤並於畫面顯示「使用者已拒絕授權」。

4. **場景七：兩階段登入 (PermanentAuthorization) 略過同意頁**
   - 針對已建立 Permanent Authorization 的用戶與 Client，登入後系統自動跳過同意頁，直接發放授權碼。

5. **場景外加：SPA 應用程式透過 PKCE 登入授權**
   - SPA 於前端產製 `code_challenge` 並跳轉認證站。
   - 登入並完成同意後，SPA 於回調頁（Callback）使用 `code_verifier` 換取 JWT Token 並儲存於前端狀態。

### 常見錯誤與例外狀況

- **E2E 測試找不到畫面元素 (Selector Timeout)** → 測試腳本失敗 → 前端 DOM 結構變更或非同步載入延遲 → 使用 `data-testid` 穩定定位屬性，並搭配 `await page.WaitForSelectorAsync()`。
- **跳轉時發生 Cookie 未傳遞 (SameSite=Strict 跨域限制)** → 認證站將使用者判定為未登入並再次要求登入 → 檢查認證 Cookie 之 `SameSite` 設定應為 `Lax` 以支援 OIDC 頂層重導向。
- **SPA PKCE 換 Token 缺少 code_verifier** → Token 端點回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → SPA 回調處理器未自 SessionStorage 取回暫存之 verifier → 確保在發起跳轉前正確寫入本地暫存。

### 你現在可以做的下一步

- 執行完整的 WebUI E2E 測試套件：`dotnet test test/OAuth.AuthServer.WebUI.E2E`
- 檢閱後端 Consent 端點實作：參閱 [授權同意 API 指南](./authserver-core-for-developers.md#授權同意-api-開發者操作指南)
