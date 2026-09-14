# OAuth2 + OIDC 授權流程、系統交互與狀態機架構設計文件

本文件記錄了本專案中核心 OAuth2 / OIDC 授權流程、帳號安全、授權同意、開發者平台、後台管理之循序圖（Sequence Diagrams），以及 Token、應用程式審核、金鑰輪替與憑證之狀態機（State Diagrams）。

---

## 目錄 (Table of Contents)

- [1. 核心 OAuth2 / OIDC 授權流程循序圖](#1-核心-oauth2--oidc-授權流程循序圖)
  - [1.1 Authorization Code Flow + PKCE (本地登入與 Headless 同意畫面)](#11-authorization-code-flow--pkce-本地登入與-headless-同意畫面)
  - [1.2 Social Login (Google 等) 聯邦綁定與登入流程](#12-social-login-google-等-聯邦綁定與登入流程)
  - [1.3 Client Credentials Flow (服務對服務)](#13-client-credentials-flow-服務對服務)
  - [1.4 Permanent Authorization (永久授權略過同意畫面)](#14-permanent-authorization-永久授權略過同意畫面)
- [2. 帳號安全與授權管理循序圖](#2-帳號安全與授權管理循序圖)
  - [2.1 會員註冊流程](#21-會員註冊流程)
  - [2.2 帳號安全與 2FA (TOTP) 管理](#22-帳號安全與-2fa-totp-管理)
  - [2.3 授權管理與級聯撤銷 (Cascading Revocation)](#23-授權管理與級聯撤銷-cascading-revocation)
- [3. 開發者平台與金鑰管理循序圖](#3-開發者平台與金鑰管理循序圖)
  - [3.1 第三方應用程式註冊與審核送審](#31-第三方應用程式註冊與審核送審)
  - [3.2 雙金鑰輪替零停機過渡機制 (Dual-Secret Rotation)](#32-雙金鑰輪替零停機過渡機制-dual-secret-rotation)
- [4. 管理員治理與後台管理循序圖](#4-管理員治理與後台管理循序圖)
  - [4.1 第三方應用審核與強制停用級聯吊銷](#41-第三方應用審核與強制停用級聯吊銷)
  - [4.2 使用者狀態管理 (凍結、解凍與強制登出)](#42-使用者狀態管理-凍結解凍與強制登出)
  - [4.3 角色管理與 OIDC Roles Claim 發放](#43-角色管理與-oidc-roles-claim-發放)
- [5. 核心狀態機 (State Diagrams)](#5-核心狀態機-state-diagrams)
  - [5.1 OIDC Token 狀態機 (Auth Code, Access Token, Refresh Token Rotation)](#51-oidc-token-狀態機-auth-code-access-token-refresh-token-rotation)
  - [5.2 第三方應用程式審核生命週期狀態機](#52-第三方應用程式審核生命週期狀態機)
  - [5.3 開發者雙金鑰生命週期狀態機](#53-開發者雙金鑰生命週期狀態機)
  - [5.4 開發與測試環境憑證安全狀態機](#54-開發與測試環境憑證安全狀態機)

---

## 1. 核心 OAuth2 / OIDC 授權流程循序圖

### 1.1 Authorization Code Flow + PKCE (本地登入與 Headless 同意畫面)

> **原始碼依據**：
> - 授權端點：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/AuthorizationController.cs:24-145`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/AuthorizationController.cs#L24-L145)
> - 登入端點：[`src/AuthServer/OAuth.AuthServer.WebAPI/Account/AccountApiController.cs:44-76`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Account/AccountApiController.cs#L44-L76)
> - 同意端點：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/ConsentApiController.cs:26-75`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/ConsentApiController.cs#L26-L75)
> - Token 端點：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs:19-77`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs#L19-L77)
> - 測試案例：[`test/OAuth.AuthServer.IntegrationTest/_04_Consent/授權同意API.feature`](../../test/OAuth.AuthServer.IntegrationTest/_04_Consent/%E6%8E%88%E6%AC%8A%E5%90%8C%E6%84%8FAPI.feature)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Client as 客戶端 (SPA/MVC)
    participant Browser as 瀏覽器 / Vue 3 前端
    participant AS as 授權伺服器 (AuthServer)
    participant Cache as 分散式快取 (IDistributedCache)
    participant DB as 資料庫 (Identity + OpenIddict)

    User->>Client: 點擊登入
    Note over Client: 產生 code_verifier<br/>與 code_challenge (S256)
    Client->>Browser: 導向 /connect/authorize?response_type=code&client_id=...&scope=...&redirect_uri=...&code_challenge=...&code_challenge_method=S256
    Browser->>AS: GET /connect/authorize
    Note over AS: 檢查 HttpContext.AuthenticateAsync(Identity.Application)<br/>未登入 (Authentication Result != Succeeded)
    AS-->>Browser: 302 重導向 /login?returnUrl=/connect/authorize?...
    Browser->>User: 顯示登入頁面 (/login)
    User->>Browser: 輸入帳號密碼並送出
    Browser->>AS: POST /api/v1/account/login (含 RateLimit 防暴力破解)
    AS->>DB: 驗證帳密 (SignInManager.PasswordSignInAsync)
    DB-->>AS: 驗證成功
    AS-->>Browser: 回傳 200 OK (寫入 HttpOnly Identity Cookie) & returnUrl
    Browser->>AS: 導回 returnUrl (GET /connect/authorize，帶 Cookie)
    Note over AS: 驗證已登入。查詢 Client ConsentType (Explicit)<br/>且無現有 Permanent Authorization
    AS-->>Browser: 302 重導向 /consent?returnUrl=/connect/authorize?...
    Browser->>AS: GET /api/v1/connect/consent-info?returnUrl=...
    Note over AS: ReturnUrlValidator 驗證防止 Open Redirect
    AS->>DB: 查詢 Client 顯示名稱與 Scope 定義
    DB-->>AS: 回傳 Client 資訊
    AS-->>Browser: 回傳 200 OK (clientId, clientDisplayName, scopes)
    Browser->>User: 顯示同意畫面 (列出請求的權限範圍)
    
    alt 使用者點擊「同意」
        User->>Browser: 點擊同意授權
        Browser->>AS: POST /api/v1/connect/consent-accept (returnUrl, clientId)
        AS->>Cache: 寫入暫存 consent:{__ct} = "granted:{clientId}" (TTL 60s)
        AS-->>Browser: 回傳 200 OK (redirectUrl: /connect/authorize?...&__ct=...)
        Browser->>AS: GET /connect/authorize?...&__ct=...
        AS->>Cache: 讀取並清除 consent:{__ct}
        AS->>DB: 建立 Permanent Authorization (OpenIddict)
        AS-->>Browser: 302 重導向 Client redirect_uri?code=AUTH_CODE
        Browser->>Client: 攜帶 Authorization Code 回調
        Client->>AS: POST /connect/token (grant_type=authorization_code, code, code_verifier, client_id, client_secret)
        AS->>AS: 驗證 PKCE code_verifier 與 code_challenge
        AS->>DB: 驗證 Authorization 狀態有效性 (Statuses.Valid) 並發行 Token
        DB-->>AS: 寫入成功
        AS-->>Client: 200 OK (access_token, id_token, refresh_token)
    else 使用者點擊「拒絕」
        User->>Browser: 點擊拒絕授權
        Browser->>AS: POST /api/v1/connect/consent-deny (returnUrl, clientId)
        AS->>Cache: 寫入暫存 consent:{__ct} = "denied:{clientId}" (TTL 60s)
        AS-->>Browser: 回傳 200 OK (redirectUrl: /connect/authorize?...&__ct=...)
        Browser->>AS: GET /connect/authorize?...&__ct=...
        AS->>Cache: 讀取並清除 consent:{__ct}
        AS-->>Browser: 302 重導向 Client redirect_uri?error=access_denied
        Browser->>Client: 回傳授權被拒絕錯誤
    end
```

---

### 1.2 Social Login (Google 等) 聯邦綁定與登入流程

> **原始碼依據**：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/ExternalLoginController.cs:18-85`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/ExternalLoginController.cs#L18-L85)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Client as 客戶端 (SPA/MVC)
    participant Browser as 瀏覽器
    participant AS as 授權伺服器 (AuthServer)
    participant Provider as 外部 Identity Provider (Google等)
    participant DB as 資料庫 (Identity)

    User->>Client: 點擊使用外部帳號登入
    Client->>Browser: 導向 /connect/authorize
    Browser->>AS: GET /connect/authorize
    AS-->>Browser: 302 重導向 /login
    User->>Browser: 點擊「以 Google 帳號登入」
    Browser->>AS: GET /connect/authorize/external?provider=Google&returnUrl=...
    AS-->>Browser: 302 Challenge 重導向至 Provider 授權頁面
    Browser->>Provider: 使用者輸入 Google 帳密並同意授權
    Provider-->>Browser: 302 重導向回 /connect/authorize/external/callback?code=EXT_CODE
    Browser->>AS: GET /connect/authorize/external/callback
    AS->>Provider: 交換外部 Token 並獲取 User Profile (email, name, picture)
    Provider-->>AS: 回傳 Profile 資料
    AS->>DB: 查詢 AspNetUserLogins 是否已綁定 (FindByLoginAsync)
    alt 已存在綁定
        DB-->>AS: 回傳對應 ApplicationUser
    else 未綁定但 Email 已存在本地帳號
        AS->>DB: 查詢 FindByEmailAsync 並呼叫 AddLoginAsync 綁定
        DB-->>AS: 綁定成功
    else 全新使用者
        AS->>DB: 建立新 ApplicationUser (UserManager.CreateAsync) + AddLoginAsync
        DB-->>AS: 建立與綁定成功
    end
    AS->>AS: 建立本地登入工作階段 (SignInManager.SignInAsync)
    AS-->>Browser: 302 重導回原始 returnUrl (/connect/authorize)
    Note over Browser,AS: 進入標準 OIDC 同意與授權碼發放流程 (詳見 1.1 節)
```

---

### 1.3 Client Credentials Flow (服務對服務)

> **原始碼依據**：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs:79-85`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs#L79-L85)

```mermaid
sequenceDiagram
    autonumber
    participant Client as 後端 API 客戶端 (Machine-to-Machine)
    participant AS as 授權伺服器 (AuthServer)
    participant DB as 資料庫 (OpenIddict)

    Client->>AS: POST /connect/token<br/>(grant_type=client_credentials, client_id, client_secret, scope)
    AS->>DB: 驗證 Client ID、Client Secret 與請求的 Scope 是否在核准清單內
    alt 驗證通過且 Scope 合法
        DB-->>AS: 驗證成功
        AS->>DB: 記錄並簽發 Access Token
        DB-->>AS: 寫入成功
        AS-->>Client: 200 OK (access_token, token_type=Bearer, expires_in=900)
    else Scope 超出核准範圍 (Unauthorized Scope)
        AS-->>Client: 400 Bad Request (error: invalid_scope)
    else 金鑰錯誤 (Invalid Secret)
        AS-->>Client: 400 Bad Request (error: invalid_client)
    end
```

---

### 1.4 Permanent Authorization (永久授權略過同意畫面)

> **原始碼依據**：
> - [`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/AuthorizationController.cs:44-56, 96-101`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/AuthorizationController.cs#L44-L56)
> - 測試案例：[`test/OAuth.AuthServer.IntegrationTest/_06_RealWorldScenarios/跨模組真實場景.feature:267-314`](../../test/OAuth.AuthServer.IntegrationTest/_06_RealWorldScenarios/%E8%B7%A8%E6%A8%A1%E7%B5%84%E7%9C%9F%E5%AF%A6%E5%A0%B4%E6%99%AF.feature#L267-L314)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者 (已登入)
    participant Client as 客戶端
    participant Browser as 瀏覽器
    participant AS as 授權伺服器 (AuthServer)
    participant DB as 資料庫 (OpenIddict)

    User->>Client: 發起授權請求 (相同 ClientId 與 Scopes)
    Client->>Browser: 導向 /connect/authorize
    Browser->>AS: GET /connect/authorize (帶有 Cookie)
    AS->>DB: 查詢 authorizationManager.FindAsync(subject, client, status=Valid, type=Permanent, scopes)
    DB-->>AS: 找到有效永久授權紀錄 (Permanent Authorization)
    Note over AS: 判定已獲得授權，略過 /consent 同意頁面重導
    AS->>DB: 發放 Authorization Code
    DB-->>AS: 儲存成功
    AS-->>Browser: 302 重導向 Client redirect_uri?code=AUTH_CODE
    Browser->>Client: 攜帶 Code 完成後續 Token 交換
```

---

## 2. 帳號安全與授權管理循序圖

### 2.1 會員註冊流程

> **原始碼依據**：
> - 控制器：[`src/AuthServer/OAuth.AuthServer.WebAPI/Account/AccountApiController.cs:19-38`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Account/AccountApiController.cs#L19-L38)
> - 測試案例：[`test/OAuth.AuthServer.IntegrationTest/_01_Account/帳號註冊.feature`](../../test/OAuth.AuthServer.IntegrationTest/_01_Account/%E5%B8%B3%E8%99%9F%E8%A8%BB%E5%86%8A.feature)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Browser as 註冊前端頁面
    participant API as Account API (AccountApiController)
    participant DB as 資料庫 (Identity)

    User->>Browser: 輸入 Email、密碼、DisplayName 等資訊
    Browser->>API: POST /api/v1/account/register
    API->>API: FluentValidation 驗證 (欄位必填、Email 格式、密碼長度與複雜度)
    alt 欄位驗證不符規則
        API-->>Browser: 400 Bad Request (Validation Errors)
    else 格式合法
        API->>DB: 檢查 Email 是否已存在 (FindByEmailAsync)
        alt Email 重複
            DB-->>API: 已存在帳號
            API-->>Browser: 409 Conflict (code: email_already_exists)
        else Email 可用
            API->>DB: 建立新使用者 ApplicationUser (UserManager.CreateAsync)
            DB-->>API: 寫入成功
            API-->>Browser: 201 Created ({ userId, email, displayName })
            Browser->>User: 顯示註冊成功並引導登入
        end
    end
```

---

### 2.2 帳號安全與 2FA (TOTP) 管理

> **原始碼依據**：
> - 控制器：[`src/Account/OAuth.Account.WebAPI/Controllers/SecurityController.cs`](../../src/Account/OAuth.Account.WebAPI/Controllers/SecurityController.cs)
> - 測試案例：[`src/Account/OAuth.Account.Tests/_02_Security/帳號安全與2FA.feature`](../../src/Account/OAuth.Account.Tests/_02_Security/%E5%B8%B3%E8%99%9F%E5%AE%89%E5%85%A8%E8%88%872FA.feature)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant App as Authenticator App (Google/MS)
    participant UI as Account Portal UI
    participant API as Security API (SecurityController)
    participant DB as 資料庫 (Identity)

    Note over User,DB: 【情境 A：密碼修改】
    User->>UI: 輸入舊密碼與新密碼
    UI->>API: POST /api/v1/account/security/change-password (Bearer Token)
    API->>DB: UserManager.ChangePasswordAsync(user, currentPwd, newPwd)
    alt 舊密碼正確且新密碼符合政策
        DB-->>API: 修改成功
        API-->>UI: 200 OK (message: "密碼修改成功")
    else 舊密碼錯誤或密碼不符政策
        DB-->>API: 失敗
        API-->>UI: 400 Bad Request
    end

    Note over User,DB: 【情境 B：啟用 2FA (TOTP)】
    User->>UI: 點擊啟用雙層驗證
    UI->>API: POST /api/v1/account/security/2fa/generate-key
    API->>DB: 讀取或重設 AuthenticatorKey
    API-->>UI: 200 OK (sharedKey, authenticatorUri: "otpauth://totp/...")
    UI->>User: 顯示 QR Code 與金鑰
    User->>App: 掃描 QR Code 綁定帳號
    App-->>User: 顯示 6 位數動態驗證碼 (TOTP)
    User->>UI: 輸入 6 位數驗證碼
    UI->>API: POST /api/v1/account/security/2fa/verify-and-enable ({ code })
    API->>API: VerifyTwoFactorTokenAsync 驗證動態碼
    alt 驗證碼正確
        API->>DB: SetTwoFactorEnabledAsync(user, true)
        DB-->>API: 更新成功
        API-->>UI: 200 OK ("雙層驗證已成功啟用")
    else 驗證碼錯誤
        API-->>UI: 400 Bad Request ("驗證碼無效或已過期")
    end

    Note over User,DB: 【情境 C：停用 2FA】
    User->>UI: 點擊停用 2FA
    UI->>API: POST /api/v1/account/security/2fa/disable
    API->>DB: SetTwoFactorEnabledAsync(false) & ResetAuthenticatorKeyAsync
    DB-->>API: 更新成功
    API-->>UI: 200 OK ("雙層驗證已成功停用")
```

---

### 2.3 授權管理與級聯撤銷 (Cascading Revocation)

> **原始碼依據**：
> - 控制器：[`src/Account/OAuth.Account.WebAPI/Controllers/ConsentsController.cs:18-87`](../../src/Account/OAuth.Account.WebAPI/Controllers/ConsentsController.cs#L18-L87)
> - 測試案例：[`src/Account/OAuth.Account.Tests/_03_Consents/授權管理與級聯撤銷.feature`](../../src/Account/OAuth.Account.Tests/_03_Consents/%E6%8E%88%E6%AC%8A%E7%AE%A1%E7%90%86%E8%88%87%E7%B4%9A%E8%81%AF%E6%92%A4%E9%8A%B7.feature)

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Portal as Account Portal (前端)
    participant API as Consents API (ConsentsController)
    participant AS as 授權伺服器 (TokenController)
    participant DB as 資料庫 (OpenIddict)
    participant ClientA as 第三方 App A (已撤銷)
    participant ClientB as 第三方 App B (仍有效)

    User->>Portal: 進入「已授權應用管理」
    Portal->>API: GET /api/v1/account/consents (Bearer Token)
    API->>DB: 查詢 authorizationManager.FindAsync(subject=userId, status=Valid)
    DB-->>API: 回傳授權清單 (App A & App B)
    API-->>Portal: 200 OK ([{ authorizationId: "Auth_A", clientId: "app-a" }, { authorizationId: "Auth_B", clientId: "app-b" }])

    User->>Portal: 點擊撤銷 App A 授權
    Portal->>API: DELETE /api/v1/account/consents/Auth_A
    API->>DB: 驗證授權所有人 (auth.Subject == userId，防 IDOR)
    alt 非本人授權 (IDOR 攻擊)
        API-->>Portal: 403 Forbidden
    else 驗證成功
        API->>DB: 1. authorizationManager.TryRevokeAsync(Auth_A)
        API->>DB: 2. 級聯作廢：tokenManager.FindByAuthorizationIdAsync(Auth_A)<br/>作廢所有關聯之 Access Tokens 與 Refresh Tokens
        DB-->>API: 級聯撤銷完成
        API-->>Portal: 204 No Content
    end

    Note over ClientA,AS: 【後續影響驗證】
    ClientA->>AS: POST /connect/token (grant_type=refresh_token, refresh_token=RT_A)
    AS->>DB: 查詢 Auth_A 狀態 (HasStatusAsync(Statuses.Valid))
    DB-->>AS: 狀態已為 Revoked / 無效
    AS-->>ClientA: 400 Bad Request (error: invalid_grant)

    ClientB->>AS: POST /connect/token (grant_type=refresh_token, refresh_token=RT_B)
    AS->>DB: 查詢 Auth_B 狀態
    DB-->>AS: 狀態仍為 Valid
    AS-->>ClientB: 200 OK (回傳新 Access Token，證明撤銷互不干擾)
```

---

## 3. 開發者平台與金鑰管理循序圖

### 3.1 第三方應用程式註冊與審核送審

> **原始碼依據**：
> - 應用註冊：[`src/Developer/OAuth.Developer.WebAPI/Controllers/ApplicationsController.cs`](../../src/Developer/OAuth.Developer.WebAPI/Controllers/ApplicationsController.cs)
> - 審核送審：[`src/Developer/OAuth.Developer.WebAPI/Controllers/ReviewSubmissionController.cs`](../../src/Developer/OAuth.Developer.WebAPI/Controllers/ReviewSubmissionController.cs)
> - 測試案例：[`test/OAuth.AuthServer.IntegrationTest/_06_RealWorldScenarios/跨模組真實場景.feature:38-68`](../../test/OAuth.AuthServer.IntegrationTest/_06_RealWorldScenarios/%E8%B7%A8%E6%A8%A1%E7%B5%84%E7%9C%9F%E5%AF%A6%E5%A0%B4%E6%99%AF.feature#L38-L68)

```mermaid
sequenceDiagram
    autonumber
    actor Dev as 開發者
    participant Portal as Developer Portal
    participant AppAPI as Applications API
    participant ReviewAPI as Review Submission API
    participant DB as 資料庫 (OpenIddict + Custom Properties)

    Dev->>Portal: 填寫應用程式資訊 (名稱、Redirect URIs、Requested Scopes: openid, profile, email)
    Portal->>AppAPI: POST /api/v1/developer/apps
    AppAPI->>DB: 建立 OpenIddict 應用 (設定 ClientType: confidential, 狀態設為 Sandbox)
    DB-->>AppAPI: 寫入成功
    AppAPI-->>Portal: 201 Created ({ id, clientId, clientSecret, status: "Sandbox" })
    
    Dev->>Portal: 在沙盒測試完成後，點擊「提交審核」
    Portal->>ReviewAPI: POST /api/v1/developer/apps/{id}/submit-review ({ notes })
    ReviewAPI->>DB: 驗證開發者擁有權並將 status 更新為 "InReview" (記錄 submittedAt)
    DB-->>ReviewAPI: 更新成功
    ReviewAPI-->>Portal: 200 OK ({ status: "InReview" })
    
    Dev->>Portal: 查詢審核狀態
    Portal->>ReviewAPI: GET /api/v1/developer/apps/{id}/review-status
    ReviewAPI->>DB: 讀取應用狀態
    DB-->>ReviewAPI: 回傳目前狀態
    ReviewAPI-->>Portal: 200 OK ({ status: "InReview", submittedAt: "..." })
```

---

### 3.2 雙金鑰輪替零停機過渡機制 (Dual-Secret Rotation)

> **原始碼依據**：
> - 控制器：[`src/Developer/OAuth.Developer.WebAPI/Controllers/CredentialsController.cs:34-80`](../../src/Developer/OAuth.Developer.WebAPI/Controllers/CredentialsController.cs#L34-L80)
> - 服務層：[`src/Developer/OAuth.Developer.WebAPI/Services/SecretRotationManager.cs`](../../src/Developer/OAuth.Developer.WebAPI/Services/SecretRotationManager.cs)
> - 測試案例：[`src/Developer/OAuth.Developer.Tests/_02_SecretRotation/雙金鑰輪替.feature`](../../src/Developer/OAuth.Developer.Tests/_02_SecretRotation/%E9%9B%99%E9%87%91%E9%91%B0%E8%BC%AA%E6%9B%BF.feature)

```mermaid
sequenceDiagram
    autonumber
    actor Dev as 開發者 / 維運人員
    participant Portal as Developer Portal
    participant API as Credentials API (CredentialsController)
    participant MGR as SecretRotationManager
    participant Sandbox as Sandbox 驗證 / AuthServer
    participant DB as 資料庫 (OpenIddict Properties)

    Note over Dev,DB: 【階段 1：觸發金鑰輪替】
    Dev->>Portal: 點擊「輪替金鑰 (Rotate Secret)」
    Portal->>API: POST /api/v1/developer/apps/{id}/rotate-secret
    API->>MGR: RotateSecretAsync(app)
    Note over MGR: 1. 現有 Active Secret 轉為 Retiring Secret<br/>2. 設定 retiringSecretExpiresAt = Now + 7 天<br/>3. 產生全新 Active Secret
    MGR->>DB: 儲存雙金鑰設定 (ClientSecret = Hash(NewSecret))
    DB-->>MGR: 更新成功
    API-->>Portal: 200 OK (newSecret, retiringSecretExpiresAt: 7天後)

    Note over Dev,DB: 【階段 2：雙金鑰並存期 (7 天零停機過渡)】
    Dev->>Sandbox: 使用新金鑰驗證 (POST /sandbox/validate-credentials)
    Sandbox->>DB: 比對 Active Secret
    Sandbox-->>Dev: 200 OK (isValid: true)
    
    Dev->>Sandbox: 尚未改版之舊服務使用舊金鑰驗證
    Sandbox->>DB: 比對 Retiring Secret 且未逾期
    Sandbox-->>Dev: 200 OK (isValid: true，零停機保證)

    Note over Dev,DB: 【階段 3：線上全面部署完成，手動作廢舊金鑰】
    Dev->>Portal: 點擊「立即作廢舊金鑰」
    Portal->>API: POST /api/v1/developer/apps/{id}/revoke-retiring-secret
    API->>MGR: RevokeRetiringSecretAsync(app)
    MGR->>DB: 清除 RetiringSecret 與 Expiration
    DB-->>MGR: 清除成功
    API-->>Portal: 200 OK ("舊金鑰已成功作廢")

    Dev->>Sandbox: 再次使用舊金鑰驗證
    Sandbox-->>Dev: 400 Bad Request (isValid: false, error: invalid_client)
    Dev->>Sandbox: 使用新金鑰驗證
    Sandbox-->>Dev: 200 OK (isValid: true)
```

---

## 4. 管理員治理與後台管理循序圖

### 4.1 第三方應用審核與強制停用級聯吊銷

> **原始碼依據**：
> - 控制器：[`src/Admin/OAuth.Admin.WebAPI/Controllers/AppReviewController.cs:21-214`](../../src/Admin/OAuth.Admin.WebAPI/Controllers/AppReviewController.cs#L21-L214)
> - 測試案例：[`src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_01_AppReview/第三方應用審核流.feature`](../../src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_01_AppReview/%E7%AC%AC%E4%B8%89%E6%96%B9%E6%87%89%E7%94%A8%E5%AF%A9%E6%A0%B8%E6%B5%81.feature)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 管理員
    participant Portal as Admin Portal (WebAPI)
    participant API as AppReviewController
    participant Audit as AuditLogService
    participant DB as 資料庫 (OpenIddict)

    Admin->>Portal: 查看待審核清單
    Portal->>API: GET /api/v1/admin/apps/pending
    API->>DB: 篩選 status == "InReview" 的應用
    DB-->>API: 回傳待審核清單
    API-->>Portal: 200 OK

    alt 審核通過
        Admin->>Portal: 點擊核准
        Portal->>API: POST /api/v1/admin/apps/{id}/approve
        API->>DB: 驗證目前狀態為 InReview -> 更新 status = "Approved"
        API->>Audit: 記錄審計日誌 (EventType: "AppApproved")
        API-->>Portal: 200 OK ("Application approved successfully")
    else 審核駁回
        Admin->>Portal: 輸入駁回原因並提交
        Portal->>API: POST /api/v1/admin/apps/{id}/reject ({ reason })
        API->>DB: 驗證目前狀態為 InReview -> 更新 status = "Rejected", rejectReason = reason
        API->>Audit: 記錄審計日誌 (EventType: "AppRejected")
        API-->>Portal: 200 OK ("Application rejected successfully")
    else 緊急強制停用違規應用 (Suspension & Cascading Revoke)
        Admin->>Portal: 發現違規行為，點擊停用
        Portal->>API: POST /api/v1/admin/apps/{id}/suspend
        API->>DB: 1. 更新 status = "Suspended"<br/>2. 級聯吊銷：tokenManager.RevokeAsync(client=appId)<br/>3. 級聯吊銷：authManager.RevokeAsync(client=appId)
        DB-->>API: 狀態已停用且所有流通 Token 已被註銷
        API->>Audit: 記錄審計日誌 (EventType: "AppSuspended")
        API-->>Portal: 200 OK ("Application suspended and all active tokens revoked")
    else 恢復已停用應用
        Admin->>Portal: 調查完畢，點擊恢復
        Portal->>API: POST /api/v1/admin/apps/{id}/restore
        API->>DB: 驗證目前狀態為 Suspended -> 更新 status = "Approved"
        API->>Audit: 記錄審計日誌 (EventType: "AppRestored")
        API-->>Portal: 200 OK ("Application restored successfully")
    end
```

---

### 4.2 使用者狀態管理 (凍結、解凍與強制登出)

> **原始碼依據**：
> - 控制器：[`src/Admin/OAuth.Admin.WebAPI/Controllers/UserManageController.cs:74-134`](../../src/Admin/OAuth.Admin.WebAPI/Controllers/UserManageController.cs#L74-L134)
> - 測試案例：[`src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_02_UserManage/使用者狀態管理.feature`](../../src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_02_UserManage/%E4%BD%BF%E7%94%A8%E8%80%85%E7%8B%80%E6%85%8B%E7%AE%A1%E7%90%86.feature)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 管理員
    participant Portal as Admin Portal
    participant API as UserManageController
    participant Audit as AuditLogService
    participant DB as 資料庫 (Identity)

    Note over Admin,DB: 【情境 1：凍結使用者帳號】
    Admin->>Portal: 選擇使用者點擊「凍結帳號」
    Portal->>API: PUT /api/v1/admin/users/{id}/lockout
    API->>DB: SetLockoutEnabledAsync(true) & SetLockoutEndDateAsync(+100年)
    DB-->>API: 更新成功
    API->>Audit: 記錄審計日誌 ("UserLockedOut")
    API-->>Portal: 200 OK (isLocked: true)

    Note over Admin,DB: 【情境 2：解除使用者凍結】
    Admin->>Portal: 點擊「解除凍結」
    Portal->>API: PUT /api/v1/admin/users/{id}/unlock
    API->>DB: SetLockoutEndDateAsync(null)
    DB-->>API: 更新成功
    API->>Audit: 記錄審計日誌 ("UserUnlocked")
    API-->>Portal: 200 OK (isLocked: false)

    Note over Admin,DB: 【情境 3：強制登出所有工作階段】
    Admin->>Portal: 點擊「強制撤銷所有工作階段」
    Portal->>API: POST /api/v1/admin/users/{id}/revoke-sessions
    API->>DB: UpdateSecurityStampAsync(user) 更新安全性戳記
    DB-->>API: 安全戳記已更新 (既有 Cookie 與 Session 立即失效)
    API->>Audit: 記錄審計日誌 ("UserSessionsRevoked")
    API-->>Portal: 200 OK
```

---

### 4.3 角色管理與 OIDC Roles Claim 發放

> **原始碼依據**：
> - 角色管理：[`src/Admin/OAuth.Admin.WebAPI/Controllers/RoleManageController.cs:16-104`](../../src/Admin/OAuth.Admin.WebAPI/Controllers/RoleManageController.cs#L16-L104)
> - 用戶角色：[`src/Admin/OAuth.Admin.WebAPI/Controllers/UserManageController.cs:136-165`](../../src/Admin/OAuth.Admin.WebAPI/Controllers/UserManageController.cs#L136-L165)
> - Token 發放：[`src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs:67-73`](../../src/AuthServer/OAuth.AuthServer.WebAPI/Connect/TokenController.cs#L67-L73)
> - 測試案例：[`src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_04_RoleManage/角色管理.feature`](../../src/Admin/OAuth.Admin.WebAPI.IntegrationTest/_04_RoleManage/%E8%A7%92%E8%89%B2%E7%AE%A1%E7%90%86.feature)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as 管理員
    participant Portal as Admin Portal
    participant RoleAPI as RoleManageController
    participant UserAPI as UserManageController
    participant TokenAPI as TokenController (AuthServer)
    participant DB as 資料庫 (Identity Roles)

    Admin->>Portal: 新增角色 "AuditManager"
    Portal->>RoleAPI: POST /api/v1/admin/roles ({ name: "AuditManager" })
    RoleAPI->>DB: 檢查已存在角色 (RoleExistsAsync)
    alt 角色已存在
        RoleAPI-->>Portal: 400 Bad Request ("Role already exists")
    else 角色名稱唯一
        RoleAPI->>DB: RoleManager.CreateAsync(new IdentityRole("AuditManager"))
        DB-->>RoleAPI: 建立成功
        RoleAPI-->>Portal: 200 OK
    end

    Admin->>Portal: 指派角色給使用者 user01
    Portal->>UserAPI: POST /api/v1/admin/users/{id}/roles ({ roleName: "AuditManager" })
    UserAPI->>DB: UserManager.AddToRoleAsync(user, "AuditManager")
    DB-->>UserAPI: 加入成功
    UserAPI-->>Portal: 200 OK

    Note over TokenAPI,DB: 【OIDC Token 交換時 Roles Claim 動態注入】
    Note over TokenAPI: Client 發起 Token 交換 (Scope 包含 roles)
    TokenAPI->>DB: UserManager.GetRolesAsync(user) 重新載入最新角色清單
    DB-->>TokenAPI: 回傳 ["AuditManager"]
    TokenAPI->>TokenAPI: 注入 Claim(Claims.Role, "AuditManager") 至 AccessToken 與 IdentityToken
    TokenAPI-->>TokenAPI: 完成簽章並核發 Token
```

---

## 5. 核心狀態機 (State Diagrams)

### 5.1 OIDC Token 狀態機 (Auth Code, Access Token, Refresh Token Rotation)

> 本專案基於 OpenIddict 實作，其 Token 生產、使用與輪替之狀態轉換如下：

```mermaid
stateDiagram-v2
    [*] --> AuthCode_Created : 使用者登入並完成授權 (Authorize Endpoint)
    state "Authorization Code (授權碼)" as AuthCode {
        AuthCode_Created --> AuthCode_Exchanged : POST /connect/token 成功交換
        AuthCode_Created --> AuthCode_Expired : 超過有效時效 (未於期限內交換)
        AuthCode_Exchanged --> [*] : 標記已使用並作廢
        AuthCode_Expired --> [*] : 清理過期資料
    }

    [*] --> AccessToken_Active : 授權碼交換或 Client Credentials 流程成功
    state "Access Token (存取憑證)" as AccessToken {
        AccessToken_Active --> AccessToken_Expired : 達到有效期限 (15 分鐘)
        AccessToken_Active --> AccessToken_Revoked : 使用者/管理員撤銷授權或停用 App
        AccessToken_Expired --> [*] : 拒絕存取
        AccessToken_Revoked --> [*] : 拒絕存取
    }

    [*] --> RefreshToken_Active : 隨 Auth Code 交換發放 (具有 offline_access scope)
    state "Refresh Token (含 Rotation 輪替)" as RefreshToken {
        RefreshToken_Active --> RefreshToken_Rotated : POST /connect/token 成功換發新 Token
        RefreshToken_Active --> RefreshToken_Revoked : 使用者撤銷授權 / 登出 (/connect/endsession) / 管理員停用 App
        RefreshToken_Active --> RefreshToken_Expired : 超過滑動過期限制 (30 天)
        
        RefreshToken_Rotated --> [*] : 立即失效，不可再次使用
        RefreshToken_Revoked --> [*] : 標記為 Revoked，不可再次使用
        RefreshToken_Expired --> [*] : 標記為 Expired，不可再次使用
    }
    
    note right of RefreshToken_Rotated : 授權伺服器若偵測到重送已 Rotated 的舊 Token，將回傳 invalid_grant 拒絕請求。
```

---

### 5.2 第三方應用程式審核生命週期狀態機

> **原始碼依據**：[`src/Admin/OAuth.Admin.WebAPI/Controllers/AppReviewController.cs`](../../src/Admin/OAuth.Admin.WebAPI/Controllers/AppReviewController.cs) 與 [`src/Developer/OAuth.Developer.WebAPI/Controllers/ReviewSubmissionController.cs`](../../src/Developer/OAuth.Developer.WebAPI/Controllers/ReviewSubmissionController.cs)

```mermaid
stateDiagram-v2
    [*] --> Sandbox : 開發者建立應用程式 (POST /api/v1/developer/apps)
    
    state Sandbox {
        [*] --> InDevelopment
        InDevelopment : 開發者沙盒測試與除錯
    }

    Sandbox --> InReview : 開發者提交審核 (POST /submit-review)
    
    state InReview {
        [*] --> PendingAudit
        PendingAudit : 等待平台管理員審核 Redirect URI 與 Scopes
    }

    InReview --> Approved : 管理員審核通過 (POST /admin/apps/{id}/approve)
    InReview --> Rejected : 管理員審核駁回並附理由 (POST /admin/apps/{id}/reject)
    
    state Rejected {
        [*] --> NeedsFix
        NeedsFix : 開發者依駁回理由修正設定
    }
    Rejected --> InReview : 開發者修正後重新送審 (POST /submit-review)

    state Approved {
        [*] --> ActiveProduction
        ActiveProduction : 正式環境流通，允許使用者授權並發行 Token
    }

    Approved --> Suspended : 管理員強制停用違規 App (POST /admin/apps/{id}/suspend)\n【級聯作廢所有流通 Token 與授權】
    
    state Suspended {
        [*] --> Frozen
        Frozen : 禁止所有授權與 Token 換發
    }

    Suspended --> Approved : 管理員解除停用並恢復上線 (POST /admin/apps/{id}/restore)
```

---

### 5.3 開發者雙金鑰生命週期狀態機

> **原始碼依據**：[`src/Developer/OAuth.Developer.WebAPI/Services/SecretRotationManager.cs`](../../src/Developer/OAuth.Developer.WebAPI/Services/SecretRotationManager.cs)

```mermaid
stateDiagram-v2
    [*] --> Active_Initial : 建立機密型 App (核發初始 Client Secret)

    state "Active Secret (目前金鑰)" as ActiveState {
        Active_Initial --> Active_InUse : 用於客戶端認證
        Active_InUse --> Retiring : 觸發金鑰輪替 (POST /rotate-secret)
    }

    state "Retiring Secret (過渡期舊金鑰)" as RetiringState {
        Retiring : 降級為過渡金鑰\n設定 7 天相容期
        Retiring --> Revoked_Manual : 管理員/開發者手動作廢 (POST /revoke-retiring-secret)
        Retiring --> Expired_Auto : 超過 7 天緩衝期自動失效
    }

    state "Invalid / Revoked" as DeadState {
        Revoked_Manual --> [*] : 驗證失敗 (invalid_client)
        Expired_Auto --> [*] : 驗證失敗 (invalid_client)
    }

    state "New Active Secret" as NewActiveState {
        [*] --> New_Active : 輪替時同步產生全新金鑰
        New_Active --> [*] : 立即具備完整驗證效力
    }
```

---

### 5.4 開發與測試環境憑證安全狀態機

> 為確保憑證安全性，避免 API 金鑰與個人 Token 落地外洩，本專案依循 DevOps 憑證安全規範管理如下：

```mermaid
stateDiagram-v2
    state "個人測試憑證 (creds)" as LocalCreds {
        [*] --> Creds_Created : 於本機手動建立
        Creds_Created --> Creds_Stored : 集中存放於 ~/.claude/creds/.creds (不進 git)
        Creds_Stored --> Creds_InUse : 驅動 Playwright E2E 測試與自動化驗證
        Creds_Stored --> Creds_Rotated : 變更測試帳密時更新
        Creds_Stored --> Creds_Deleted : 測試帳號廢棄清理
    }

    state "Git HTTPS 憑證" as GitCreds {
        [*] --> Git_Token_Issued : GitLab/GitHub 核發 Personal Access Token
        Git_Token_Issued --> Git_Helper_Configured : 設定 git credential helper (不寫入 Remote URL)
        Git_Helper_Configured --> Git_Clean_URL : remote 保持標準 https://host/repo.git
        Git_Clean_URL --> Git_Helper_Authed : 執行 git 指令時由 helper 自動注入憑證
        Git_Helper_Authed --> Git_Token_Expired : Token 逾期或定期輪替
        Git_Helper_Authed --> Git_Token_Revoked : 主動註銷 Token
    }

    state "Social Login Client Secret" as SocialSecrets {
        [*] --> Secret_Generated : 於 Google/Line 等開發者後台產生
        Secret_Generated --> Secret_Local_Configured : 寫入 appsettings.Development.json (不進 git)
        Secret_Local_Configured --> Secret_In_Memory : 服務啟動時載入記憶體
        Secret_In_Memory --> Secret_OIDC_Handshake : 與 OAuth Provider 進行後台 Token 交換驗證
        Secret_In_Memory --> Secret_Rotated : 金鑰外洩或定期維護時更新
    }
```
