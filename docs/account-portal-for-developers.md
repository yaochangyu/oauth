# Account Portal 個人中心開發者操作指南

本文件彙整使用者個人帳號中心（Account Portal）之後端 API 與安全防護機制，包含個人基本資料維護、密碼修改與 2FA 兩步驟驗證、授權管理與級聯撤銷（Cascade Revocation），以及 Token 驗證與安全性防護。

---

## 個人資料維護開發者操作指南

本功能提供使用者在帳號管理模組（Account Module）查詢自身個人資料並更新暱稱/顯示名稱（DisplayName）與個人頭像網址（AvatarUrl）。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./profile-maintenance/diagram.html)

### 主要操作流程

1. **查詢個人資料**
   呼叫 `GET /api/v1/account/profile` 取得當前使用者身分資料。
   - **HTTP Method**: `GET`
   - **URL**: `https://account.example.com/api/v1/account/profile`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://account.example.com/api/v1/account/profile \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應範例：
   ```json
   {
     "userId": "user_acc_01",
     "email": "user_acc_01@example.com",
     "displayName": "測試小明",
     "avatarUrl": "https://example.com/avatar.png",
     "emailConfirmed": true,
     "isTwoFactorEnabled": false
   }
   ```

2. **更新個人資料**
   發送 PUT 請求更新個人顯示名稱與頭像網址。
   - **HTTP Method**: `PUT`
   - **URL**: `https://account.example.com/api/v1/account/profile`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X PUT https://account.example.com/api/v1/account/profile \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
     -d '{
       "displayName": "小明改名卡",
       "avatarUrl": "https://example.com/avatar_new.png"
     }'
   ```
   回應狀態碼為 `200 OK` 並回傳更新後的個人資料物件。

### 常見錯誤與例外狀況

- **未帶 Token 存取個人資料** → 回傳 `401 Unauthorized` → 端點缺少有效的使用者驗證 Token → 在請求中加入 `Authorization: Bearer <token>`。
- **使用者不存在** → 回傳 `404 Not Found` (`{"message": "找不到使用者"}`) → Token 解析出之 sub 或 NameIdentifier 找不到對應的資料庫使用者帳號。
- **更新失敗** → 回傳 `400 Bad Request` (`{"message": "更新個人資料失敗", "errors": [...]}`) → 違反資料庫或模型驗證條件。

### 你現在可以做的下一步

- 執行個人資料維護測試：
  ```bash
  dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~個人資料維護"
  ```
- 進行帳號安全防護設定：參閱 [帳號安全與 2FA 操作指南](#帳號安全與-2fa-開發者操作指南)
- 前往會員中心畫面測試更新：參閱 [個人基本資料維護操作指南 (使用者版)](./account-portal-for-users.md#個人基本資料維護操作指南)

---

## 帳號安全與 2FA 開發者操作指南

本功能提供使用者修改登入密碼、查詢雙步驟驗證（2FA TOTP）啟用狀態、產生金鑰 URI 以及驗證並啟用/停用 2FA 安全防護。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./account-security-2fa/diagram.html)

### 主要操作流程

1. **修改密碼**
   呼叫 `POST /api/v1/account/security/change-password` 傳入舊密碼、新密碼與確認密碼。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/account/security/change-password`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://account.example.com/api/v1/account/security/change-password \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>" \
     -d '{
       "currentPassword": "OldPassword123",
       "newPassword": "NewPassword888",
       "confirmPassword": "NewPassword888"
     }'
   ```
   成功回傳 `200 OK` (`{"message": "密碼修改成功"}`)。

2. **查詢 2FA 狀態**
   呼叫 `GET /api/v1/account/security/2fa-status` 取得是否已啟用及是否有金鑰。
   - **HTTP Method**: `GET`
   - **URL**: `https://account.example.com/api/v1/account/security/2fa-status`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://account.example.com/api/v1/account/security/2fa-status \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   {
     "isTwoFactorEnabled": false,
     "hasAuthenticator": false
   }
   ```

3. **產生 2FA TOTP 金鑰與 URI**
   呼叫 `POST /api/v1/account/security/2fa/generate-key` 產生金鑰與 Authenticator URI。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/account/security/2fa/generate-key`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://account.example.com/api/v1/account/security/2fa/generate-key \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   {
     "sharedKey": "JBSWY3DPEHPK3PXP",
     "authenticatorUri": "otpauth://totp/OAuthPortal:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=OAuthPortal&digits=6"
   }
   ```

4. **校驗並啟用 2FA**
   使用者以 Authenticator 驗證器產生 6 位數 Code 後送出校驗啟用。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/account/security/2fa/verify-and-enable`
   - **Content-Type**: `application/json`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://account.example.com/api/v1/account/security/2fa/verify-and-enable \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>" \
     -d '{ "code": "123456" }'
   ```
   回應 `200 OK` (`{"message": "雙層驗證 (2FA) 已成功啟用"}`)。

5. **停用 2FA**
   呼叫 `POST /api/v1/account/security/2fa/disable` 解除雙步驟驗證並重設密鑰。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/account/security/2fa/disable`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://account.example.com/api/v1/account/security/2fa/disable \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>"
   ```
   回應 `200 OK` (`{"message": "雙層驗證 (2FA) 已成功停用"}`)。

### 常見錯誤與例外狀況

- **修改密碼時新密碼與確認密碼不符** → 回傳 `400 Bad Request` (`{"message": "新密碼與確認密碼不相符"}`)。
- **修改密碼時舊密碼錯誤或密碼原則不符** → 回傳 `400 Bad Request` (`{"message": "密碼修改失敗", "errors": [...]}`)。
- **2FA 驗證碼 (TOTP Code) 錯誤或過期** → 回傳 `400 Bad Request` (`{"message": "驗證碼無效或已過期"}`) → 提示使用者輸入驗證器最新產生的 6 碼動態碼。
- **未帶 Token 存取安全設定端點** → 回傳 `401 Unauthorized`。

### 你現在可以做的下一步

- 執行帳號安全與 2FA 測試：
  ```bash
  dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~帳號安全與2FA"
  ```
- 檢視授權管理與 Token 安全：參閱 [授權管理與級聯撤銷指南](#授權管理與級聯撤銷開發者操作指南)
- 前往會員安全中心畫面體驗：參閱 [密碼修改與兩步驟安全驗證操作指南 (使用者版)](./account-portal-for-users.md#密碼修改與兩步驟安全驗證操作指南)

---

## 授權管理與級聯撤銷開發者操作指南

本功能提供使用者查詢已授權的第三方應用程式清單，並在撤銷授權時執行級聯清理（Cascade Revocation），一併作廢與該授權關聯的所有 Refresh Token 與流通 Access Token。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./consent-management-cascade-revocation/diagram.html)

### 主要操作流程

1. **查詢已授權應用程式清單**
   呼叫 `GET /api/v1/consents` 取得使用者當前已授權之 Client 清單。
   - **HTTP Method**: `GET`
   - **URL**: `https://account.example.com/api/v1/consents`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://account.example.com/api/v1/consents \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應範例：
   ```json
   [
     {
       "clientId": "mvc-client",
       "clientName": "MVC Client Application",
       "scopes": ["openid", "profile", "api"],
       "createdAt": "2026-09-01T08:00:00Z"
     }
   ]
   ```

2. **撤銷指定應用程式授權 (級聯作廢)**
   發送 DELETE 請求移除對指定 `clientId` 的授權記錄。
   - **HTTP Method**: `DELETE`
   - **URL**: `https://account.example.com/api/v1/consents/mvc-client`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X DELETE https://account.example.com/api/v1/consents/mvc-client \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應狀態碼為 `204 No Content`。伺服器底層將同步作廢 OpenIddict 中的 Authorization 實體及其關聯的所有 Token。

### 常見錯誤與例外狀況

- **嘗試撤銷其他使用者的授權 (IDOR 攻擊)** → 回傳 `403 Forbidden` 或 `404 Not Found` → 系統實施使用者範圍防護，禁止跨帳號撤銷 → 確保僅能操作當前 Token 識別身分名下之授權。
- **撤銷後第三方 App 仍使用舊 Refresh Token 換發** → `/connect/token` 端點回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 級聯機制已成功清除伺服器端 Token 記錄 → 第三方 App 必須引導使用者重新登入。
- **未帶 Token 呼叫授權管理 API** → 回傳 `401 Unauthorized` → 缺少有效身分憑證 → 需先完成使用者登入。

### 你現在可以做的下一步

- 檢視後端 Token 驗證與安全性機制：參閱 [Token 驗證與安全性指南](#token-驗證與安全性開發者操作指南)
- 執行級聯撤銷整合測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~授權管理與級聯撤銷"`

---

## Token 驗證與安全性開發者操作指南

本功能定義系統在驗證傳入之 JWT Token 時所執行的嚴格加密校驗規則，包含對稱金鑰偽造防禦、非信任 RSA 金鑰防禦以及 Issuer、Audience 聲明檢驗。

### 流程架構圖

[檢視架構與流程圖 (HTML)](./token-validation-security/diagram.html)

### 主要操作流程

1. **發送帶有 JWT 之 API 請求**
   客戶端呼叫受保護 API 時夾帶 Token。
   - **Header**: `Authorization: Bearer <TOKEN>`

2. **中介軟體執行四大安全性校驗**
   - **簽章金鑰校驗**：比對 JWT Header 之 `kid` 與 AuthServer 公鑰集合，拒絕自製對稱金鑰（HMAC-SHA256）與未知 RSA 私鑰簽發之 Token。
   - **Issuer (簽發者) 檢驗**：宣告之 `iss` 必須完全等於合法認證站網址（如 `https://auth.example.com/`）。
   - **Audience (受眾) 檢驗**：宣告之 `aud` 必須包含目標資源伺服器之 Client ID 或 Resource 識別碼。
   - **效期檢驗**：檢查 `nbf`（生效時間）與 `exp`（過期時間）。

### 常見錯誤與例外狀況

- **使用自製對稱金鑰 (HS256) 偽造 Token 存取** → 回傳 `401 Unauthorized` → 系統嚴格要求使用 RSA256 非對稱簽章，拒絕 HS256 演算法 → 僅接受官方 AuthServer RSA 私鑰簽發之 Token。
- **使用未知第三方 RSA 私鑰簽發之 Token** → 回傳 `401 Unauthorized` → 金鑰指紋 (kid) 未在認證伺服器 JWKS 清單中 → 嚴格使用授權伺服器頒發之 Token。
- **偽造 Issuer (iss Claim 不匹配)** → 回傳 `401 Unauthorized` → Token 中的簽發者宣告與系統設定之權威認證站不符 → 確保連線至正確之環境認證伺服器。
- **偽造 Audience (aud Claim 不匹配)** → 回傳 `401 Unauthorized` → Token 未授權存取當前資源伺服器 → 申請授權時需加入目標資源之 Scope。

### 你現在可以做的下一步

- 了解應用程式生命週期管理：參閱 [應用程式生命週期與 PKCE 指南](./developer-portal-for-developers.md#應用程式生命週期與-pkce-開發者操作指南)
- 執行 Token 安全性檢驗測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~Token驗證與安全性"`
