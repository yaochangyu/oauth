# 個人資料維護開發者操作指南

本功能提供使用者在帳號管理模組（Account Module）查詢自身資料並更新暱稱（Nickname）與個人頭像網址（AvatarUrl）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **查詢個人資料**
   呼叫 `GET /api/v1/profile` 取得當前使用者檔案。
   - **HTTP Method**: `GET`
   - **URL**: `https://account.example.com/api/v1/profile`
   - **Header**: `Authorization: Bearer <USER_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://account.example.com/api/v1/profile \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   回應範例：
   ```json
   {
     "userId": "usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
     "email": "user@example.com",
     "nickname": "原始暱稱",
     "avatarUrl": "https://example.com/avatar.png"
   }
   ```

2. **更新個人資料**
   發送 PUT 請求更新暱稱與頭像網址。
   - **HTTP Method**: `PUT`
   - **URL**: `https://account.example.com/api/v1/profile`
   - **Content-Type**: `application/json`

   ```bash
   curl -X PUT https://account.example.com/api/v1/profile \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..." \
     -d '{
       "nickname": "新暱稱",
       "avatarUrl": "https://example.com/new-avatar.png"
     }'
   ```
   回應狀態碼為 `200 OK` 並回傳更新後的個人資料物件。

## 常見錯誤與例外狀況

- **未帶 Token 存取個人資料** → 回傳 `401 Unauthorized` → 端點缺少有效的使用者驗證 Token → 在請求中加入 `Authorization: Bearer <token>`。
- **暱稱長度超過系統上限 (如 50 字元)** → 回傳 `400 Bad Request` → 欄位驗證不符合規範 → 調整暱稱長度後重新送出。
- **頭像網址格式錯誤 (非合法 URL)** → 回傳 `400 Bad Request` → 傳入的 avatarUrl 未通過 URL 格式檢查 → 提供正確的 HTTP/HTTPS 圖片連結。

## 你現在可以做的下一步

- 進行安全防護設定：參閱 [帳號安全與 2FA 操作指南](../account-security-2fa/account-security-2fa-for-developers.md)
- 執行個人資料維護測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~個人資料維護"`
