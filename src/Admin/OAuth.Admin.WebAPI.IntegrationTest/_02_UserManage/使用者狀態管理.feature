Feature: 使用者狀態管理

Background:
  Given 初始化管理員測試伺服器

Scenario: 01. 管理員凍結違規用戶帳號
  Given 資料庫已存在一般用戶 "user_to_lock" 帳號正常
  When 管理員發送 "PUT" 請求至 "/api/v1/admin/users/user_to_lock/lockout"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/users/user_to_lock"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.isLocked" 的"布林值等於" "true"

Scenario: 02. 管理員解除用戶帳號凍結
  Given 資料庫已存在被凍結用戶 "user_to_unlock"
  When 管理員發送 "PUT" 請求至 "/api/v1/admin/users/user_to_unlock/unlock"
  Then 預期得到 HttpStatusCode 為 "200"
  When 管理員發送 "GET" 請求至 "/api/v1/admin/users/user_to_unlock"
  Then 預期得到 HttpStatusCode 為 "200"
  And 預期回傳內容中路徑 "$.isLocked" 的"布林值等於" "false"

Scenario: 03. 管理員強制登出用戶工作階段
  Given 資料庫已存在一般用戶 "user_to_revoke" 帳號正常
  When 管理員發送 "POST" 請求至 "/api/v1/admin/users/user_to_revoke/revoke-sessions"
  Then 預期得到 HttpStatusCode 為 "200"
  Then 用戶 "user_to_revoke" 的安全戳記已被更新
