# 帳號安全與 2FA 開發者操作指南

本功能提供使用者修改登入密碼、查詢雙步驟驗證（2FA TOTP）啟用狀態、產生金鑰 URI 以及驗證並啟用/停用 2FA 安全防護。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **修改密碼**
   呼叫 `POST /api/v1/security/change-password` 傳入舊密碼與新密碼。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/security/change-password`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://account.example.com/api/v1/security/change-password \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>" \
     -d '{
       "currentPassword": "OldPassword123",
       "newPassword": "NewPassword456",
       "confirmPassword": "NewPassword456"
     }'
   ```
   成功回傳 `200 OK`。

2. **查詢 2FA 狀態與產生 TOTP 金鑰**
   呼叫 `GET /api/v1/security/2fa/status` 取得是否啟用及 QRCode URI。
   ```bash
   curl -X GET https://account.example.com/api/v1/security/2fa/status \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   {
     "isEnabled": false,
     "sharedKey": "JBSWY3DPEHPK3PXP",
     "authenticatorUri": "otpauth://totp/OAuth:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=OAuth"
   }
   ```

3. **驗證並啟用 2FA**
   使用者以驗證器產生 6 位數 Code 後送出啟用。
   - **HTTP Method**: `POST`
   - **URL**: `https://account.example.com/api/v1/security/2fa/enable`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://account.example.com/api/v1/security/2fa/enable \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <USER_ACCESS_TOKEN>" \
     -d '{ "code": "123456" }'
   ```

4. **停用 2FA**
   呼叫 `POST /api/v1/security/2fa/disable` 解除雙步驟驗證。

## 常見錯誤與例外狀況

- **修改密碼時舊密碼錯誤** → 回傳 `400 Bad Request`，Body 為 `{"code": "invalid_current_password", "message": "目前密碼不正確"}` → 目前密碼驗證未通過 → 提示使用者確認目前密碼。
- **新密碼長度小於 8 碼或未達複雜度要求** → 回傳 `400 Bad Request` → 違反 Identity 密碼原則（需含數字與英文小寫） → 要求使用者設定符合規範的高強度密碼。
- **2FA 驗證碼 (TOTP Code) 錯誤或過期** → 回傳 `400 Bad Request` (`{"message": "驗證碼無效"}`) → 驗證碼計算不符或逾時（時差超過 30 秒） → 提示使用者輸入驗證器最新產生的 6 碼動態碼。

## 你現在可以做的下一步

- 檢視授權管理與 Token 安全：參閱 [授權管理與級聯撤銷指南](../consent-management-cascade-revocation/consent-management-cascade-revocation-for-developers.md)
- 執行帳號安全與 2FA 測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~2FA"`
