Feature: Scope與審計日誌管理

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 全域 Scope 建立與敏感權限標記
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "name": "financial_records",
    "displayName": "財務紀錄讀取權限",
    "description": "允許讀取用戶之高敏感財務紀錄",
    "resources": ["financial_api"],
    "isSensitive": true
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/scopes"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/scopes/financial_records"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.name" 的"字串等於" "financial_records"
  And 預期回傳內容中路徑 "$.isSensitive" 的"布林值等於" "true"

Scenario: 02. 查詢授權審計日誌
  Given 系統中存在一筆審計日誌事件 "AppSuspended" 針對用戶 "admin"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/audit-logs?eventType=AppSuspended"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$[?(@.eventType == 'AppSuspended')].actor" 的"字串等於" "admin"
