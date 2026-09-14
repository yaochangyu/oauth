# Developer Portal 開發者中心操作指南

本文件彙整開發者入口平台（Developer Portal）之功能與技術規範，包含 OAuth 客戶端應用程式生命週期管理、PKCE 設定、雙金鑰零停機輪替機制、沙盒測試工具與開發者模組安全性權限控制。

---

## 應用程式生命週期與 PKCE 開發者操作指南

本功能提供已啟用開發者身分之工程師建立機密型 Web、公開型 SPA 或 Mobile 應用程式，自動強制配置 PKCE 防護，並支援查詢詳細資訊與提交上線審核。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./application-lifecycle-pkce/diagram.html)

### 主要操作流程

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

### 常見錯誤與例外狀況

- **未啟用開發者身分嘗試建立 App** → 回傳 `403 Forbidden` (`{"message": "尚未啟用開發者身分"}`) → 該帳號尚未開通開發者權限 → 先前往開發者帳號端點進行開通。
- **公開型客戶端 (SPA / Mobile) 未強制啟用 PKCE** → 伺服器拒絕建立或自動強制設定 `requirePkce=true` → 公開客戶端無安全密鑰儲存機制，依安全規範強制必須啟用 PKCE。
- **對已在審核中 (InReview) 或已核准 (Approved) 的 App 重複送審** → 回傳 `400 Bad Request` → 僅允許 `Draft` 或 `Rejected` 狀態送審 → 請等待當前審核結果或於退回後修改再送審。

### 你現在可以做的下一步

- 進行金鑰輪替維護：參閱 [雙金鑰輪替操作指南](#雙金鑰輪替與零停機過渡開發者操作指南)
- 執行應用程式生命週期測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~應用程式生命週期"`

---

## 雙金鑰輪替與零停機過渡開發者操作指南

本功能提供機密型應用程式在金鑰即將到期或需定期輪替時，透過雙金鑰（Primary 與 Retiring Secret）並存機制實現零停機（Zero-downtime）無縫平滑切換。

### 流程架構圖

- [檢視線性操作流程圖 (HTML)](./secret-rotation/diagram.html)
- [檢視雙金鑰輪替時序互動圖 (HTML)](./secret-rotation/diagram-2.html)

### 主要操作流程

1. **觸發金鑰輪替 (產生新金鑰並保留舊金鑰過渡)**
   呼叫 `POST /api/v1/developer/apps/{id}/rotate-secret`。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/rotate-secret`
   - **Header**: `Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/rotate-secret \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   {
     "primarySecret": "sec_NEW_KEY_77a98b2c...",
     "retiringSecret": "sec_OLD_KEY_11e23f4d...",
     "retiringExpiresAt": "2026-09-20T12:00:00Z"
   }
   ```
   *此時處於雙金鑰並存狀態：客戶端無論使用新金鑰或舊金鑰請求 `/connect/token` 均能正常通過驗證。*

2. **客戶端線上服務部署更新為新金鑰**
   逐步更新所有生產環境微服務節點的 `client_secret` 為 `primarySecret`。

3. **正式廢止舊金鑰 (Revoke Retiring Secret)**
   確認所有服務節點皆已完成更新後，呼叫廢止端點。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/revoke-retiring-secret`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/revoke-retiring-secret \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```
   回應狀態為 `200 OK`，此後舊金鑰立即失效，僅新金鑰有效。

### 常見錯誤與例外狀況

- **使用已廢止的舊金鑰換 Token** → 回傳 `400 Bad Request` (`{"error": "invalid_client"}`) → 舊金鑰已從資料庫完全刪除作廢 → 檢查微服務是否仍有殘留節點使用舊金鑰配置。
- **非 App 擁有者嘗試觸發金鑰輪替** → 回傳 `403 Forbidden` → 越權存取他人 App 金鑰 → 確保操作者為 App 登記之合法開發者。
- **在尚未處於輪替過渡狀態時呼叫廢止舊金鑰** → 回傳 `400 Bad Request` (`{"message": "當前無處於退役中之金鑰"}`) → 目前僅有單一 Primary 金鑰 → 無須執行廢止動作。

### 你現在可以做的下一步

- 執行雙金鑰輪替整合測試：
  ```bash
  dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~雙金鑰輪替"
  ```
- 於沙盒工具驗證新金鑰：參閱 [開發者帳號與沙盒指南](#開發者帳號與沙盒工具操作指南)
- 參閱開發者安全性與授權控制：[安全性與權限控制指南](#開發者模組安全性與權限控制操作指南)

---

## 開發者帳號與沙盒工具操作指南

本功能提供一般會員自助開通為開發者身分（Developer Profile），並使用內建沙盒工具組裝與生成 OAuth / OIDC 授權測試網址，以及驗證 Client 憑證。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./developer-account-sandbox/diagram.html)

### 主要操作流程

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

### 常見錯誤與例外狀況

- **生成授權網址時缺少 ClientId 或 RedirectUri** → 回傳 `400 Bad Request` (`{"error": "ClientId 與 RedirectUri 為必填欄位"}`)。
- **驗證憑證時 ClientId 不存在或金鑰錯誤** → 回傳 `400 Bad Request` (`{"isValid": false, "error": "invalid_client", "errorDescription": "..."}`)。
- **未登入直接存取沙盒工具** → 回傳 `401 Unauthorized`。

### 你現在可以做的下一步

- 執行開發者帳號與沙盒整合測試：
  ```bash
  dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~開發者帳號與沙盒"
  ```
- 參閱雙金鑰輪替機制：[雙金鑰輪替與零停機過渡指南](#雙金鑰輪替與零停機過渡開發者操作指南)
- 參閱開發者權限與安全防護機制：[安全性與權限控制指南](#開發者模組安全性與權限控制操作指南)

---

## 開發者模組安全性與權限控制操作指南

本功能定義開發者中心在多租戶環境下的嚴格安全防禦邊界，包含防範偽造 HTTP Header 竄改身分、跨租戶越權存取（IDOR）攔截以及 Token 偽造防護。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./developer-security-access-control/diagram.html)

### 主要操作流程

1. **強制從 JWT Token 解析擁有者身分**
   開發者模組端點全面忽略客戶端傳入之自訂 Header（如 `X-Developer-UserId`），唯一信任 JWT Payload 中經過數位簽章的 `sub` Claim 作為資料歸屬擁有者。

2. **跨租戶資源隔離檢查**
   當開發者 A 嘗試查詢、修改、刪除屬於開發者 B 的應用程式或金鑰時，伺服器執行擁有權比對並回傳 `403 Forbidden`。

3. **金鑰與審核越權防禦**
   跨帳號請求金鑰輪替、廢止或送審均被嚴格攔截。

### 常見錯誤與例外狀況

- **未帶 Token 存取開發者 API** → 回傳 `401 Unauthorized` → 缺少身分驗證憑證 → 需先登入並在 Header 附帶 `Authorization: Bearer <token>`。
- **跨使用者存取他人 App (IDOR 越權存取)** → 回傳 `403 Forbidden`，Body 為 `{"message": "您無權存取或修改此應用程式"}` → 該 App 擁有者不是當前登入者 → 僅能操作自己帳號名下建立之應用程式。
- **偽造 X-Developer-UserId Header 嘗試冒用他人身分** → 系統忽略該 Header 並僅以 Token sub claim 建立資料 → 攻擊無效，資料正確歸屬於 Token 簽發者。
- **使用偽造 Issuer / Audience 之 Token 存取** → 回傳 `401 Unauthorized` → 簽章與屬性校驗失敗 → 確保使用官方 AuthServer 簽發之合法 Token。

### 你現在可以做的下一步

- 了解管理員後台權限控制：參閱 [管理員權限保護指南](./admin-portal-for-developers.md#管理員權限保護操作指南)
- 執行開發者安全性與權限測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~安全性與權限控制"`
