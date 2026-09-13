# 系統角色管理維運操作指南

本功能提供管理員查詢系統角色清單、新增角色以及刪除指定角色，支援基於角色之存取控制（RBAC）體系。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **查詢角色清單**
   呼叫 `GET /api/v1/admin/roles` 取得所有角色。
   - **HTTP Method**: `GET`
   - **URL**: `https://admin.example.com/api/v1/admin/roles`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X GET https://admin.example.com/api/v1/admin/roles \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   [
     { "id": "rol_01", "name": "Administrator" },
     { "id": "rol_02", "name": "Auditor" }
   ]
   ```

2. **新增自訂角色**
   呼叫 `POST /api/v1/admin/roles` 建立新角色。
   - **HTTP Method**: `POST`
   - **URL**: `https://admin.example.com/api/v1/admin/roles`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`
   - **Content-Type**: `application/json`

   ```bash
   curl -X POST https://admin.example.com/api/v1/admin/roles \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
     -d '{ "roleName": "SupportEngineer" }'
   ```
   回應 `201 Created`。

3. **刪除角色**
   發送 DELETE 請求移除指定角色。
   - **HTTP Method**: `DELETE`
   - **URL**: `https://admin.example.com/api/v1/admin/roles/SupportEngineer`
   - **Header**: `Authorization: Bearer <ADMIN_ACCESS_TOKEN>`

   ```bash
   curl -X DELETE https://admin.example.com/api/v1/admin/roles/SupportEngineer \
     -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>"
   ```
   回應 `204 No Content`。

## 常見錯誤與例外狀況

- **重複新增已存在之角色** → 回傳 `400 Bad Request` (`{"message": "角色名稱已存在"}`) → 角色名稱具備唯一性約束 → 檢查是否已存在同名角色。
- **刪除不存在之角色** → 回傳 `404 Not Found` (`{"message": "找不到指定之角色"}`) → 目標角色不存在 → 確認角色名稱無誤。
- **未授權身分操作角色管理** → 回傳 `401 Unauthorized` 或 `403 Forbidden` → 僅限 Administrator 角色執行。

## 你現在可以做的下一步

- 驗證管理員權限保護機制：參閱 [管理員權限保護指南](../admin-authorization-protection/admin-authorization-protection-for-developers.md)
- 執行角色管理測試：`dotnet test src/Admin/OAuth.Admin.WebAPI.IntegrationTest --filter "FullyQualifiedName~角色管理"`
