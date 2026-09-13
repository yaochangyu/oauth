# 個人資料維護開發者操作指南

本功能提供使用者在帳號管理模組（Account Module）查詢自身個人資料並更新暱稱/顯示名稱（DisplayName）與個人頭像網址（AvatarUrl）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **未帶 Token 存取個人資料** → 回傳 `401 Unauthorized` → 端點缺少有效的使用者驗證 Token → 在請求中加入 `Authorization: Bearer <token>`。
- **使用者不存在** → 回傳 `404 Not Found` (`{"message": "找不到使用者"}`) → Token 解析出之 sub 或 NameIdentifier 找不到對應的資料庫使用者帳號。
- **更新失敗** → 回傳 `400 Bad Request` (`{"message": "更新個人資料失敗", "errors": [...]}`) → 違反資料庫或模型驗證條件。

## 你現在可以做的下一步

- 執行個人資料維護測試：
  ```bash
  dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~個人資料維護"
  ```
- 進行帳號安全防護設定：參閱 [帳號安全與 2FA 操作指南](../account-security-2fa/account-security-2fa-for-developers.md)
- 前往會員中心畫面測試更新：參閱 [個人基本資料維護操作指南 (使用者版)](./profile-maintenance-for-users.md)
