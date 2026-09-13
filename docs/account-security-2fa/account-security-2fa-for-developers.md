# 帳號安全與 2FA 開發者操作指南

本功能提供使用者修改登入密碼、查詢雙步驟驗證（2FA TOTP）啟用狀態、產生金鑰 URI 以及驗證並啟用/停用 2FA 安全防護。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **修改密碼時新密碼與確認密碼不符** → 回傳 `400 Bad Request` (`{"message": "新密碼與確認密碼不相符"}`)。
- **修改密碼時舊密碼錯誤或密碼原則不符** → 回傳 `400 Bad Request` (`{"message": "密碼修改失敗", "errors": [...]}`)。
- **2FA 驗證碼 (TOTP Code) 錯誤或過期** → 回傳 `400 Bad Request` (`{"message": "驗證碼無效或已過期"}`) → 提示使用者輸入驗證器最新產生的 6 碼動態碼。
- **未帶 Token 存取安全設定端點** → 回傳 `401 Unauthorized`。

## 你現在可以做的下一步

- 執行帳號安全與 2FA 測試：
  ```bash
  dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~帳號安全與2FA"
  ```
- 檢視授權管理與 Token 安全：參閱 [授權管理與級聯撤銷指南](../consent-management-cascade-revocation/consent-management-cascade-revocation-for-developers.md)
- 前往會員安全中心畫面體驗：參閱 [密碼修改與兩步驟安全驗證操作指南 (使用者版)](./account-security-2fa-for-users.md)
