# Me 端點開發者操作指南

本功能提供已取得授權之客戶端應用程式查詢當前登入使用者的個人身分基本資訊（如 User ID、Email、名稱等）。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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
   伺服器解析 Token 內宣告之 Claim 並回傳使用者資訊。
   ```json
   {
     "sub": "usr_9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d",
     "email": "test@example.com",
     "name": "測試使用者"
   }
   ```

## 常見錯誤與例外狀況

- **未帶 Token 存取 /api/v1/me** → 回傳 `401 Unauthorized` → 缺少身分驗證標頭 → 在請求中附帶 `Authorization: Bearer <token>`。
- **使用無效或偽造 Token 存取** → 回傳 `401 Unauthorized` → Token 簽名校驗失敗 → 確保使用由 AuthServer 發行之有效 Token。
- **Token 缺少 profile 或 email scope** → 回傳 `403 Forbidden` 或欄位為 null → 用戶授權時未允許讀取個人資料 → 請於授權請求發起時包含 `scope=openid profile email`。

## 你現在可以做的下一步

- 存取一般受保護業務端點：參閱 [受保護資源操作指南](../protected-resources/protected-resources-for-developers.md)
- 執行 Me 端點整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~MeEndpoint"`
