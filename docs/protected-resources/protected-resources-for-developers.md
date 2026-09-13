# 受保護資源 API 開發者操作指南

本功能定義資源伺服器（Resource Server）對各類業務 API 端點的存取控制規範與權限校驗。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **發送受保護 API 請求**
   - **HTTP Method**: `GET`
   - **URL**: `https://api.example.com/api/v1/protected`
   - **Header**: `Authorization: Bearer <ACCESS_TOKEN>`

   ```bash
   curl -X GET https://api.example.com/api/v1/protected \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```

2. **驗證回應狀態**
   驗證通過後回傳 `200 OK`：
   ```json
   {
     "message": "Hello from protected API resource",
     "timestamp": "2026-09-13T12:00:00Z"
   }
   ```

## 常見錯誤與例外狀況

- **無 Token 存取受保護端點** → 回傳 `401 Unauthorized` → 端點受到 `[Authorize]` 屬性保護 → 在 Header 中附帶有效的 Bearer Token。
- **偽造 Token 存取** → 回傳 `401 Unauthorized` → 簽名不合法 → 禁止使用未經認證站簽發之自製 Token。
- **權限不足 (缺少特定 Scope)** → 回傳 `403 Forbidden` → Token 未包含存取此資源所需之 Scope（如 `scope=api`） → 客戶端需向使用者申請對應權限授權。

## 你現在可以做的下一步

- 了解 Scope 全域管理配置：參閱 [Scope 與審計日誌管理指南](../scope-and-audit-logs/scope-and-audit-logs-for-developers.md)
- 執行受保護資源整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~ProtectedResource"`
