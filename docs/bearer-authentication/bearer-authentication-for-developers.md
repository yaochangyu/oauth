# Bearer Token 驗證開發者操作指南

本功能定義客戶端呼叫受保護資源 WebAPI 時，在 HTTP Header 攜帶 Bearer Token 進行身分驗證與權限檢核的標準流程。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

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

## 常見錯誤與例外狀況

- **未帶 Authorization Header** → 回傳 `401 Unauthorized` → 請求中缺少身分識別憑證 → 在 HTTP 標頭中加入 `Authorization: Bearer <token>`。
- **Token 格式錯誤或簽章無效 (Invalid Signature)** → 回傳 `401 Unauthorized` → Token 遭竄改或非本系統認證站簽發 → 重新自授權伺服器取得合法 Token。
- **Token 已過期 (Expired Token)** → 回傳 `401 Unauthorized` → JWT 中的 `exp` 時間已過期 → 使用 Refresh Token 換發新 Access Token 後再重試。

## 你現在可以做的下一步

- 呼叫使用者資訊端點：參閱 [Me 端點操作指南](../me-endpoint/me-endpoint-for-developers.md)
- 執行 Bearer 驗證整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~Bearer"`
