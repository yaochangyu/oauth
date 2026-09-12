Feature: 第三方應用審核流

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 取得待審核應用清單
  Given 資料庫已存在待審核第三方應用 "pending-app-01" 其開發者為 "dev@example.com" 狀態為 "InReview"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/pending"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$[?(@.clientId == 'pending-app-01')].status" 的"字串等於" "InReview"

Scenario: 02. 管理員核准待審核應用（合法轉換：InReview -> Approved）
  Given 資料庫已存在待審核第三方應用 "app-to-approve" 其開發者為 "dev2@example.com" 狀態為 "InReview"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/app-to-approve/approve"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/app-to-approve"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Approved"

Scenario: 03. 管理員駁回待審核應用（合法轉換：InReview -> Rejected）
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

Scenario: 04. 管理員強制停用違規應用並吊銷流通Token（合法轉換：Approved -> Suspended）
  Given 資料庫已存在已核准第三方應用 "violating-app" 其開發者為 "dev4@example.com" 狀態為 "Approved"
  And 應用 "violating-app" 擁有活躍授權與Token
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/violating-app/suspend"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/violating-app"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Suspended"
  Then 應用 "violating-app" 的所有流通Token皆已被吊銷

Scenario: 05. 管理員恢復已停用應用（合法轉換：Suspended -> Approved）
  Given 資料庫已存在第三方應用 "suspended-app" 其開發者為 "dev5@example.com" 狀態為 "Suspended"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/suspended-app/restore"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/suspended-app"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.status" 的"字串等於" "Approved"

Scenario: 06. 非法狀態轉換 - 嘗試核准非 InReview 狀態應用應回傳 400 錯誤
  Given 資料庫已存在第三方應用 "sandbox-app-1" 其開發者為 "dev6@example.com" 狀態為 "Sandbox"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/sandbox-app-1/approve"
  Then 預期得到 HttpStatusCode 為 "400"

Scenario: 07. 非法狀態轉換 - 嘗試駁回非 InReview 狀態應用應回傳 400 錯誤
  Given 資料庫已存在第三方應用 "approved-app-1" 其開發者為 "dev7@example.com" 狀態為 "Approved"
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "reason": "不合法駁回"
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/approved-app-1/reject"
  Then 預期得到 HttpStatusCode 為 "400"

Scenario: 08. 非法狀態轉換 - 嘗試停用非 Approved 狀態應用應回傳 400 錯誤
  Given 資料庫已存在第三方應用 "sandbox-app-2" 其開發者為 "dev8@example.com" 狀態為 "Sandbox"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/sandbox-app-2/suspend"
  Then 預期得到 HttpStatusCode 為 "400"

Scenario: 09. 非法狀態轉換 - 嘗試恢復非 Suspended 狀態應用應回傳 400 錯誤
  Given 資料庫已存在第三方應用 "inreview-app-1" 其開發者為 "dev9@example.com" 狀態為 "InReview"
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps/inreview-app-1/restore"
  Then 預期得到 HttpStatusCode 為 "400"

Scenario: 10. 管理員建立第三方應用
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "clientId": "created-app-01",
    "displayName": "Created App 01",
    "clientType": "confidential",
    "clientSecret": "Secret123!",
    "consentType": "explicit",
    "developer": "dev@test.com",
    "redirectUris": ["https://created.example.com/callback"],
    "postLogoutRedirectUris": ["https://created.example.com/logout"],
    "permissions": ["ept:authorization", "ept:token", "gt:authorization_code", "gt:refresh_token", "scp:api"]
  }
  """
  When 管理員發送 "POST" 請求至 "/api/v1/admin/apps"
  Then 預期得到 HttpStatusCode 為 "201"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/created-app-01"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.clientId" 的"字串等於" "created-app-01"
  And 預期回傳內容中路徑 "$.displayName" 的"字串等於" "Created App 01"
  And 預期回傳內容中路徑 "$.clientType" 的"字串等於" "confidential"

Scenario: 11. 管理員編輯第三方應用設定
  Given 資料庫已存在第三方應用 "app-to-edit" 其開發者為 "dev-edit@example.com" 狀態為 "Sandbox"
  Given 管理員已準備 Body 參數(Json)
  """
  {
    "displayName": "Updated Display Name",
    "clientType": "public",
    "consentType": "implicit",
    "developer": "dev-updated@example.com",
    "redirectUris": ["https://updated.example.com/callback"],
    "postLogoutRedirectUris": ["https://updated.example.com/logout"],
    "permissions": ["ept:authorization", "gt:authorization_code"]
  }
  """
  When 管理員發送 "PUT" 請求至 "/api/v1/admin/apps/app-to-edit"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/app-to-edit"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.displayName" 的"字串等於" "Updated Display Name"
  And 預期回傳內容中路徑 "$.clientType" 的"字串等於" "public"
  And 預期回傳內容中路徑 "$.consentType" 的"字串等於" "implicit"

Scenario: 12. 管理員刪除第三方應用
  Given 資料庫已存在第三方應用 "app-to-delete" 其開發者為 "dev-del@example.com" 狀態為 "Sandbox"
  When 管理員發送 "DELETE" 請求至 "/api/v1/admin/apps/app-to-delete"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/apps/app-to-delete"
  Then 預期得到 HttpStatusCode 為 "404"

