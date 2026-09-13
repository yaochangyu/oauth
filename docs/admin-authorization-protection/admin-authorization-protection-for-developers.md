# 管理員權限保護操作指南

本功能定義 Admin WebAPI 核心防護機制，確保僅有具備 `Administrator` 角色之管理員 Token 方可存取系統管理端點，並拒絕未登入與一般使用者之存取。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **以管理員身分取得 Access Token**
   管理員登入並換發含有 `role: Administrator` Claim 之 JWT Access Token。

2. **呼叫管理端點執行操作**
   在請求 Header 附帶管理員 Token。
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/scopes`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/scopes \
     -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIs..."
   ```
   驗證通過回傳 `200 OK` 與資源資料。

## 常見錯誤與例外狀況

- **未登入使用者存取管理端點 (未帶 Token)** → 回傳 `401 Unauthorized` → 缺少授權憑證 → 先行登入以取得 Token。
- **一般登入使用者存取管理端點 (無 Administrator 角色)** → 回傳 `403 Forbidden` → Token 雖有效但其 Claims 中缺少 `role: Administrator` → 拒絕非管理員之越權存取。
- **嘗試以未授權身分呼叫修改/刪除端點 (POST/PUT/DELETE)** → 回傳 `401 Unauthorized`（未登入）或 `403 Forbidden`（非管理員） → 修改端點受同等嚴格角色保護。

## 你現在可以做的下一步

- 進行第三方應用審核：參閱 [第三方應用審核流指南](../app-review-workflow/app-review-workflow-for-developers.md)
- 執行管理員權限保護測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~管理員權限保護"`
