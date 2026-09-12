Feature: 安全性與權限控制

  Background: 初始化測試環境
    Given 初始化測試伺服器

  # =========================================================================
  # 【Blocker 1 驗證】未經授權請求存取 API 端點必須回傳 401 Unauthorized
  # =========================================================================

  Scenario: 未帶 JWT Token 存取應用程式清單 API 應回傳 401
    Given 調用端未帶入任何認證 Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 使用無效 JWT Token 存取應用程式清單 API 應回傳 401
    Given 調用端使用無效的 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 未帶 JWT Token 嘗試建立應用程式應回傳 401
    Given 調用端未帶入任何認證 Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Unauthorized App",
        "appType": "Web"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 未帶 JWT Token 存取開發者帳號端點應回傳 401
    Given 調用端未帶入任何認證 Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/account/status"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  # =========================================================================
  # 【Blocker 2 驗證】身分辨識不可被 X-Developer-UserId Header 偽造
  # =========================================================================

  Scenario: 偽造 X-Developer-UserId Header 無法竄改 App 擁有者身分
    # Alice 登入並取得有效 JWT，但在 Header 惡意帶入 X-Developer-UserId: dev_user_bob
    Given 調用端已使用開發者身分 "dev_user_alice" 取得有效 JWT Token
    And 調用端已準備 Header 參數
      | X-Developer-UserId |
      | dev_user_bob       |
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Alice Private App",
        "appType": "Web",
        "redirectUris": ["https://alice.com/cb"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "AliceCreatedAppId" 為 JSON 欄位 "$.id"

    # Bob 登入並查詢自己的 App 清單，不應看到 Alice 建立的 App（因為 App 擁有者為 Alice，而非被偽造的 Bob）
    Given 調用端已使用開發者身分 "dev_user_bob" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    # Alice 登入並查詢自己的 App 清單，應該看到自己建立的 App
    Given 調用端已使用開發者身分 "dev_user_alice" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值            |
      | $[0].displayName  | 字串等於   | Alice Private App |
