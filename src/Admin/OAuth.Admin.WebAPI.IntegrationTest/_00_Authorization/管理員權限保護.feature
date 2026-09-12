Feature: 管理員權限保護

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 未登入使用者呼叫管理端點應回傳 401 Unauthorized
  When 未登入使用者發送 "GET" 請求至 "/api/v1/admin/apps/pending"
  Then 預期得到 HttpStatusCode 為 "401"
  When 未登入使用者發送 "GET" 請求至 "/api/v1/admin/users"
  Then 預期得到 HttpStatusCode 為 "401"
  When 未登入使用者發送 "GET" 請求至 "/api/v1/admin/scopes"
  Then 預期得到 HttpStatusCode 為 "401"
  When 未登入使用者發送 "GET" 請求至 "/api/v1/admin/audit-logs"
  Then 預期得到 HttpStatusCode 為 "401"

Scenario: 02. 一般登入使用者（非管理員）呼叫管理端點應回傳 403 Forbidden
  Given 使用者已登入為一般用戶 "normal_user" 角色為 "User"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/apps/pending"
  Then 預期得到 HttpStatusCode 為 "403"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/users"
  Then 預期得到 HttpStatusCode 為 "403"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/scopes"
  Then 預期得到 HttpStatusCode 為 "403"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/audit-logs"
  Then 預期得到 HttpStatusCode 為 "403"

Scenario: 03. Administrator 角色之管理員呼叫管理端點可正常存取 200 OK
  Given 使用者已登入為管理員 "admin_user" 角色為 "Administrator"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/apps/pending"
  Then 預期得到 HttpStatusCode 為 "200"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/users"
  Then 預期得到 HttpStatusCode 為 "200"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/scopes"
  Then 預期得到 HttpStatusCode 為 "200"
  When 使用者發送 "GET" 請求至 "/api/v1/admin/audit-logs"
  Then 預期得到 HttpStatusCode 為 "200"
