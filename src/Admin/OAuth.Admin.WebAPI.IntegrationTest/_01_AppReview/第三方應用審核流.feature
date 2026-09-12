Feature: 第三方應用審核流

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 取得待審核應用清單
  Given 資料庫已存在待審核第三方應用 "pending-app-01" 其開發者為 "dev@example.com" 狀態為 "InReview"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/pending"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$[?(@.clientId == 'pending-app-01')].status" 的"字串等於" "InReview"

Scenario: 02. 管理員核准待審核應用
  Given 資料庫已存在待審核第三方應用 "app-to-approve" 其開發者為 "dev2@example.com" 狀態為 "InReview"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/app-to-approve/approve"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/app-to-approve"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Approved"

Scenario: 03. 管理員駁回待審核應用
  Given 資料庫已存在待審核第三方應用 "app-to-reject" 其開發者為 "dev3@example.com" 狀態為 "InReview"
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "reason": "Redirect URI 不符合 HTTPS 規範，請修正後重新送審"
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/app-to-reject/reject"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/app-to-reject"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Rejected"
  And 預期回傳內容中路徑 "$.rejectReason" 的"字串等於" "Redirect URI 不符合 HTTPS 規範，請修正後重新送審"

Scenario: 04. 管理員強制停用違規應用並吊銷流通Token
  Given 資料庫已存在已核准第三方應用 "violating-app" 其開發者為 "dev4@example.com" 狀態為 "Approved"
  And 應用 "violating-app" 擁有活躍授權與Token
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/violating-app/suspend"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/violating-app"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Suspended"
  Then 應用 "violating-app" 的所有流通Token皆已被吊銷
