Feature: 角色管理

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 查詢角色清單
  Given 資料庫已存在角色 "ContentEditor"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/roles"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$[?(@.name == 'ContentEditor')].name" 的"字串等於" "ContentEditor"

Scenario: 02. 新增角色成功
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "name": "AuditManager"
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/roles"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/roles"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$[?(@.name == 'AuditManager')].name" 的"字串等於" "AuditManager"

Scenario: 03. 重複新增角色應回傳 400 錯誤
  Given 資料庫已存在角色 "DuplicateRole"
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "name": "DuplicateRole"
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/roles"
  Then 預期得到 HttpStatusCode 為 "400"

Scenario: 04. 刪除角色成功
  Given 資料庫已存在角色 "RoleToDelete"
  When 管理員發送 "DELETE" 請求至 "/api/v1/admin/roles/RoleToDelete"
  Then 預期得到 HttpStatusCode 為 "200"

Scenario: 05. 刪除不存在之角色應回傳 404 錯誤
  When 管理員發送 "DELETE" 請求至 "/api/v1/admin/roles/NonExistentRole"
  Then 預期得到 HttpStatusCode 為 "404"
