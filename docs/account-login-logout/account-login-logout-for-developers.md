# 帳號登入登出開發者操作指南

本功能提供 Headless 前端認證介面進行使用者帳號密碼驗證、簽發身分 Session Cookie 以及安全清除登入狀態（登出）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **帳號不存在或密碼錯誤** → 回傳 `401 Unauthorized`，Body 為 `{"message": "帳號或密碼錯誤"}` → 登入憑證不符 → 提示使用者檢查帳號密碼重新輸入。
- **短時間內連續登入失敗超過 5 次** → 回傳 `429 Too Many Requests` → 觸發登入端點 Rate Limiter 防暴力破解保護（每 IP 每分鐘上限 5 次） → 等待 1 分鐘後再重新嘗試。
- **傳入非法或跨域 returnUrl (Open Redirect 攻擊)** → 回傳 `400 Bad Request`，Body 為 `{"message": "無效或非法的 returnUrl"}` → returnUrl 未通過本機相對路徑或合法授權端點檢查 → 確保 returnUrl 為站內相對路徑。

## 你現在可以做的下一步

- 結合授權碼跳轉流程：參閱 [授權同意 API 開發者指南](../consent-api/consent-api-for-developers.md)
- 執行登入登出整合測試：`dotnet test --filter "FullyQualifiedName~帳號登入登出"`
