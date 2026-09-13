# 帳號註冊開發者操作指南

本功能提供外部系統或前端應用程式透過 RESTful API 註冊全新的使用者帳號，建立身分認證基礎資料。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **重複 Email 註冊** → 回傳 `409 Conflict`，回應 Body 為 `{"code": "email_already_exists", "message": "Email already exists"}` → 系統中已存在相同電子信箱的使用者 → 提示使用者改用該信箱直接登入，或更換其他電子信箱註冊。
- **密碼複雜度不足或長度小於 8 碼** → 回傳 `400 Bad Request`，回應 Body 包含驗證錯誤清單 → 密碼未符合安全原則（需至少 8 個字元、包含至少 1 個小寫英文字母與 1 個數字） → 前端在提交前進行表單檢核，要求使用者輸入符合規定的密碼。
- **缺少必填欄位 (Email 或 Password 為空)** → 回傳 `400 Bad Request` → 請求內容未通過 FluentValidation 驗證器檢驗 → 確保送出的 JSON 結構包含有效格式之 `email` 與 `password`。

## 你現在可以做的下一步

- 執行整合測試確認行為：`dotnet test --filter "FullyQualifiedName~帳號註冊"`
- 前往帳號登入端點驗證剛註冊成功的帳號：參閱 [帳號登入登出操作指南](../account-login-logout/account-login-logout-for-developers.md)
