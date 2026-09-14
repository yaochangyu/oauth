# AuthServer 核心模組開發者操作指南

本文件彙整 AuthServer 認證授權伺服器之核心功能，包含使用者註冊、Token 交換、安全性防禦、授權同意 API、登入登出、Bearer 驗證、PKCE 流程、Me 端點與受保護資源存取控制。

---

## 帳號註冊開發者操作指南

本功能提供外部系統或前端應用程式透過 RESTful API 註冊全新的使用者帳號，建立身分認證基礎資料。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./account-registration/diagram.html)

### 主要操作流程

1. **發送註冊請求**
   呼叫認證伺服器的註冊端點 `POST /api/v1/account/register`，並攜帶使用者註冊資訊。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/account/register`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://auth.example.com/api/v1/account/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test1234",
       "displayName": "測試使用者"
     }'
   ```

2. **接收成功回應並解析用戶識別碼**
   伺服器驗證通過後，將建立使用者帳號並回傳 `201 Created` 狀態碼與使用者資料。
   ```json
   {
     "userId": "usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
     "email": "test@example.com",
     "displayName": "測試使用者"
   }
   ```

### 常見錯誤與例外狀況

- **重複 Email 註冊** → 回傳 `409 Conflict`，回應 Body 為 `{"code": "email_already_exists", "message": "Email already exists"}` → 系統中已存在相同電子信箱的使用者 → 提示使用者改用該信箱直接登入，或更換其他電子信箱註冊。
- **密碼複雜度不足或長度小於 8 碼** → 回傳 `400 Bad Request`，回應 Body 包含驗證錯誤清單 → 密碼未符合安全原則（需至少 8 個字元、包含至少 1 個小寫英文字母與 1 個數字） → 前端在提交前進行表單檢核，要求使用者輸入符合規定的密碼。
- **缺少必填欄位 (Email 或 Password 為空)** → 回傳 `400 Bad Request` → 請求內容未通過 FluentValidation 驗證器檢驗 → 確保送出的 JSON 結構包含有效格式之 `email` 與 `password`。

### 你現在可以做的下一步

- 執行整合測試確認行為：`dotnet test --filter "FullyQualifiedName~帳號註冊"`
- 前往帳號登入端點驗證剛註冊成功的帳號：參閱 [帳號登入登出操作指南](#帳號登入登出開發者操作指南)

---

## Token 交換端點開發者操作指南

本功能提供客戶端應用程式依循 OAuth 2.0 / OpenID Connect 標準通訊協定，透過授權碼、Client 憑證或 Refresh Token 向認證伺服器換取 Access Token。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./token-exchange/diagram.html)

### 主要操作流程

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

### 常見錯誤與例外狀況

- **使用無效或不存在的 client_id** → 回傳 `401 Unauthorized`，Body 為 `{"error": "invalid_client"}` → 傳入的客戶端帳號不存在或已被停用 → 檢查傳入之 `client_id` 與 `client_secret` 是否與 Developer Portal 註冊資訊一致。
- **使用無效或已過期的 Refresh Token** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_grant"}` → Refresh Token 已過期、已被撤銷或已經被使用換發過一次（Token 輪替保護） → 引導使用者重新執行授權碼流程（Authorization Code Flow）重新登入授權。
- **授權碼 (Code) 換 Token 時缺少或帶錯 code_verifier** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_grant"}` → PKCE 驗證失敗，SHA256 計算值與授權發起時傳入的 `code_challenge` 不一致 → 檢查客戶端 PKCE 生成演算法與 code_verifier 儲存狀態。

### 你現在可以做的下一步

- 攜帶換得的 Bearer Token 呼叫受保護資源：參閱 [Bearer Token 驗證指南](#bearer-token-驗證開發者操作指南)
- 執行 Token 整合測試：`dotnet test --filter "FullyQualifiedName~Token交換"`

---

## 安全性驗證防禦開發者操作指南

本功能定義認證伺服器在面對開放重導向（Open Redirect）、Token 偽造、CSRF 跨站請求與金鑰外洩等攻擊情境下的防護機制與檢驗規則。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./security-validation/diagram.html)

### 主要操作流程

1. **白名單 Redirect URI 檢核驗證**
   發起 OAuth 授權請求時，傳入的 `redirect_uri` 必須與 Developer Portal 註冊之有效網址完全比對一致。
   - **HTTP Method**: `GET`
   - **URL**: `https://auth.example.com/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc`

2. **驗證 Access Token 簽章與過期時間**
   資源伺服器必須使用認證站的公開 RSA 金鑰檢核 JWT 簽名有效性，禁止接受偽造金鑰或對稱簽名 Token。

3. **檢查 Cookie 安全屬性**
   確認驗證站回應的認證 Cookie 皆具備 `HttpOnly`、`Secure` 與 `SameSite=Lax` 或 `SameSite=Strict` 標籤，防止 XSS 盜取與 CSRF 攻擊。

### 常見錯誤與例外狀況

- **傳入未註冊的 redirect_uri (Open Redirect 攻擊)** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_request", "error_description": "The specified 'redirect_uri' is not valid for this client application."}` → 請求中的跳轉網址不在該 Client 的白名單內 → 在開發者管理平台為該 Client 新增合法的 Redirect URI。
- **偽造或竄改 JWT 簽章 (Signature Invalid)** → 資源伺服器驗證中介軟體攔截並回傳 `401 Unauthorized` → Token 的數位簽章與伺服器 JWKS 公鑰不匹配 → 確保 Token 係由官方 AuthServer 簽發，且未遭傳輸竄改。
- **客戶端金鑰密碼錯誤 (Invalid Client Secret)** → `/connect/token` 回傳 `401 Unauthorized` (`{"error": "invalid_client"}`) → 傳入的 client_secret 與資料庫雜湊值不符 → 登入開發者平台確認目前有效的 Client Secret 或執行金鑰輪替。

### 你現在可以做的下一步

- 參閱應用程式金鑰輪替機制：[雙金鑰輪替操作指南](./developer-portal-for-developers.md#雙金鑰輪替與零停機過渡開發者操作指南)
- 執行安全性自動化驗證測試：`dotnet test --filter "FullyQualifiedName~安全性驗證"`

---

## 授權同意 API 開發者操作指南

本功能提供前端認證介面（Vue 3 / Headless WebUI）在使用者登入後，向後端查詢請求授權的應用程式資訊，並送出使用者的同意（Accept）或拒絕（Deny）決策。系統透過 60 秒短期快取憑證機制將授權決策傳遞至授權處理管線。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./consent-api/diagram.html)

### 主要操作流程

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

### 常見錯誤與例外狀況

- **未登入狀態下呼叫 Consent API** → 回傳 `401 Unauthorized` → 缺少身分驗證 Session → 引導使用者先前往登入頁進行帳號身分驗證。
- **查詢時傳入無效或非法的 returnUrl** → `consent-info` 回傳 `400 Bad Request`（`{"message": "無效或非法的 returnUrl"}`）→ returnUrl 未通過開放重導向（Open Redirect）防護校驗。
- **查詢時 Client 不存在** → `consent-info` 回傳 `400 Bad Request`（`{"message": "找不到對應的第三方應用程式"}`）。
- **提交同意或拒絕時 returnUrl 非法** → `consent-accept` / `consent-deny` 回傳 `400 Bad Request`（`{"message": "非法跳轉目標"}`）。

### 你現在可以做的下一步

- 執行同意頁面與授權決策 E2E 測試：
  ```bash
  dotnet test test/OAuth.AuthServer.WebUI.E2E --filter "FullyQualifiedName~同意頁面"
  ```
- 參閱同意頁面前端實作與手冊：[同意頁面操作手冊](./authserver-core-for-users.md#同意授權頁面操作手冊)
- 參閱已授權應用管理與撤銷：[授權管理與級聯撤銷指南](./account-portal-for-developers.md#授權管理與級聯撤銷開發者操作指南)

---

## 帳號登入登出開發者操作指南

本功能提供 Headless 前端認證介面進行使用者帳號密碼驗證、簽發身分 Session Cookie 以及安全清除登入狀態（登出）。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./account-login-logout/diagram.html)

### 主要操作流程

1. **發送登入請求**
   呼叫認證伺服器之登入端點 `POST /api/v1/account/login`，支援以使用者帳號（UserName）或電子信箱（Email）登入。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/account/login`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://auth.example.com/api/v1/account/login \
     -H "Content-Type: application/json" \
     -d '{
       "userName": "test@example.com",
       "password": "TestPassword123",
       "returnUrl": "/connect/authorize?client_id=mvc-client..."
     }'
   ```

2. **接收成功回應與 Session Cookie**
   驗證成功後，伺服器於回應 Header 中透過 `Set-Cookie` 寫入 `.AspNetCore.Identity.Application`（HttpOnly, Secure），並回傳 `200 OK`。
   ```json
   {
     "success": true,
     "returnUrl": "/connect/authorize?client_id=mvc-client..."
   }
   ```

3. **發送登出請求**
   使用者結束操作時，發送登出請求以作廢伺服器端 Session 並清除 Cookie。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/api/v1/account/logout`

   ```bash
   curl -X POST https://auth.example.com/api/v1/account/logout \
     -H "Cookie: .AspNetCore.Identity.Application=YOUR_SESSION_COOKIE"
   ```
   回應狀態碼為 `204 No Content`。

### 常見錯誤與例外狀況

- **帳號不存在或密碼錯誤** → 回傳 `401 Unauthorized`，Body 為 `{"message": "帳號或密碼錯誤"}` → 登入憑證不符 → 提示使用者檢查帳號密碼重新輸入。
- **短時間內連續登入失敗超過 5 次** → 回傳 `429 Too Many Requests` → 觸發登入端點 Rate Limiter 防暴力破解保護（每 IP 每分鐘上限 5 次） → 等待 1 分鐘後再重新嘗試。
- **傳入非法或跨域 returnUrl (Open Redirect 攻擊)** → 回傳 `400 Bad Request`，Body 為 `{"message": "無效或非法的 returnUrl"}` → returnUrl 未通過本機相對路徑或合法授權端點檢查 → 確保 returnUrl 為站內相對路徑。

### 你現在可以做的下一步

- 結合授權碼跳轉流程：參閱 [授權同意 API 開發者指南](#授權同意-api-開發者操作指南)
- 執行登入登出整合測試：`dotnet test --filter "FullyQualifiedName~帳號登入登出"`

---

## Bearer Token 驗證開發者操作指南

本功能定義客戶端呼叫受保護資源 WebAPI 時，在 HTTP Header 攜帶 Bearer Token 進行身分驗證與權限檢核的標準流程。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./bearer-authentication/diagram.html)

### 主要操作流程

1. **取得有效之 Access Token**
   透過 OAuth 2.0 Token 交換端點取得由 AuthServer 簽發的 JWT Bearer Access Token。

2. **在 HTTP 請求標頭附帶 Token**
   呼叫受保護 API 端點時，在 `Authorization` Header 加入 `Bearer <ACCESS_TOKEN>`。
   - **HTTP Method**: `GET`
   - **URL**: `https://api.example.com/api/v1/protected`
   - **Header**: `Authorization: Bearer eyJhbGciOiJSUzI1NiIs...`

   ```bash
   curl -X GET https://api.example.com/api/v1/protected \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```

3. **伺服器驗證並回傳資料**
   中介軟體透過公鑰驗證簽名與效期，成功後回傳 `200 OK` 與受保護業務資料。

### 常見錯誤與例外狀況

- **未帶 Authorization Header** → 回傳 `401 Unauthorized` → 請求中缺少身分識別憑證 → 在 HTTP 標頭中加入 `Authorization: Bearer <token>`。
- **Token 格式錯誤或簽章無效 (Invalid Signature)** → 回傳 `401 Unauthorized` → Token 遭竄改或非本系統認證站簽發 → 重新自授權伺服器取得合法 Token。
- **Token 已過期 (Expired Token)** → 回傳 `401 Unauthorized` → JWT 中的 `exp` 時間已過期 → 使用 Refresh Token 換發新 Access Token 後再重試。

### 你現在可以做的下一步

- 呼叫使用者資訊端點：參閱 [Me 端點操作指南](#me-端點開發者操作指南)
- 執行 Bearer 驗證整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~Bearer"`

---

## PKCE 授權流程開發者操作指南

本功能提供公共客戶端（SPA、Mobile App）或機密客戶端依據 RFC 7636 規範實作 Proof Key for Code Exchange (PKCE)，防止授權碼攔截攻擊。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./pkce-flow/diagram.html)

### 主要操作流程

1. **生成 Code Verifier 與 Code Challenge**
   客戶端本地隨機產生一組高強度字串 `code_verifier`（長度 43-128 字元），並計算其 SHA256 雜湊與 Base64Url 編碼得到 `code_challenge`。

2. **發起 PKCE 授權請求**
   引導瀏覽器前往 `/connect/authorize` 端點，帶入 `code_challenge` 與 `code_challenge_method=S256`。
   - **URL**: `https://auth.example.com/connect/authorize?client_id=spa-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Fspa.example.com%2Fcallback&code_challenge=BASE64URL_SHA256_VERIFIER&code_challenge_method=S256`

3. **使用授權碼與 Code Verifier 換取 Token**
   在跳轉回調頁面取得 `code` 後，發送 POST 請求至 `/connect/token`，並附帶原始的 `code_verifier`。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/connect/token`
   - **Content-Type**: `application/x-www-form-urlencoded`

   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=authorization_code&client_id=spa-client&code=AUTHORIZATION_CODE&redirect_uri=https%3A%2F%2Fspa.example.com%2Fcallback&code_verifier=ORIGINAL_CODE_VERIFIER"
   ```
   回應包含 `access_token` 與 `id_token`。

### 常見錯誤與例外狀況

- **換 Token 時缺少 code_verifier** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 該 Client 啟用了 PKCE 強制要求，換 Token 必須提供 verifier → 在換 Token 請求中補上 `code_verifier`。
- **code_verifier 雜湊值與 code_challenge 不符** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 送出的 verifier 與發起授權時的 challenge 不匹配 → 檢查前端是否有多分頁覆蓋或編碼錯誤（必須使用 Base64Url 無 Padding 編碼）。
- **授權碼過期或重複使用** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 授權碼僅有效 5 分鐘且限單次使用 → 引導使用者重新發起登入。

### 你現在可以做的下一步

- 參閱開發者沙盒自動計算工具：[開發者帳號與沙盒指南](./developer-portal-for-developers.md#開發者帳號與沙盒工具操作指南)
- 執行 PKCE 整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~PKCE"`

---

## Me 端點開發者操作指南

本功能提供已取得授權之客戶端應用程式查詢當前登入使用者的個人身分基本資訊（如 sub、name、email）。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./me-endpoint/diagram.html)

### 主要操作流程

1. **發送 GET 請求至 /api/v1/me**
   在 HTTP Header 中夾帶有效的 Access Token。
   - **HTTP Method**: `GET`
   - **URL**: `https://api.example.com/api/v1/me`
   - **Header**: `Authorization: Bearer <ACCESS_TOKEN>`

   ```bash
   curl -X GET https://api.example.com/api/v1/me \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```

2. **解析使用者資訊回應**
   伺服器校驗 Token 宣告之 Claim 並回傳使用者身分物件。
   ```json
   {
     "sub": "usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
     "name": "測試使用者",
     "email": "test@example.com"
   }
   ```

### 常見錯誤與例外狀況

本端點受標準 `[Authorize]` 屬性保護，常見回應與驗證情境如下：

- **正常取得使用者資訊** → 回傳 `200 OK` → 攜帶由認證伺服器簽發之有效 Bearer Token，回應包含 `sub`、`name` 與 `email` 欄位。
- **未帶 Token 存取 /api/v1/me** → 回傳 `401 Unauthorized` → 缺少身分驗證標頭 → 在請求中附帶 `Authorization: Bearer <token>`。
- **使用無效或偽造 Token 存取** → 回傳 `401 Unauthorized` → Token 簽名校驗失敗、過期或格式錯誤。

### 你現在可以做的下一步

- 執行 Me 端點整合測試：
  ```bash
  dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~MeEndpoint"
  ```
- 存取一般受保護業務端點：參閱 [受保護資源操作指南](#受保護資源-api-開發者操作指南)
- 參閱 Bearer Token 驗證機制：[Bearer 認證指南](#bearer-token-驗證開發者操作指南)

---

## 受保護資源 API 開發者操作指南

本功能定義資源伺服器（Resource Server）對各類業務 API 端點的存取控制規範與權限校驗。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./protected-resources/diagram.html)

### 主要操作流程

1. **發送受保護 API 請求**
   - **HTTP Method**: `GET`
   - **URL**: `https://api.example.com/api/v1/protected`
   - **Header**: `Authorization: Bearer <ACCESS_TOKEN>`

   ```bash
   curl -X GET https://api.example.com/api/v1/protected \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```

2. **驗證回應狀態**
   驗證通過後回傳 `200 OK`：
   ```json
   {
     "message": "Hello from protected API resource",
     "timestamp": "2026-09-13T12:00:00Z"
   }
   ```

### 常見錯誤與例外狀況

- **無 Token 存取受保護端點** → 回傳 `401 Unauthorized` → 端點受到 `[Authorize]` 屬性保護 → 在 Header 中附帶有效的 Bearer Token。
- **偽造 Token 存取** → 回傳 `401 Unauthorized` → 簽名不合法 → 禁止使用未經認證站簽發之自製 Token。
- **權限不足 (缺少特定 Scope)** → 回傳 `403 Forbidden` → Token 未包含存取此資源所需之 Scope（如 `scope=api`） → 客戶端需向使用者申請對應權限授權。

### 你現在可以做的下一步

- 了解 Scope 全域管理配置：參閱 [Scope 與審計日誌管理指南](./admin-portal-for-developers.md#scope-與審計日誌管理維運指南)
- 執行受保護資源整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~ProtectedResource"`
