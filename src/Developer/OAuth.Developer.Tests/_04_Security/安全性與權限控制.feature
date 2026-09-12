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
    # Alice 登入並取得有效 JWT，但在 Header 惡意帶入 X-Developer-UserId: dev_user_header_bob
    Given 調用端已使用開發者身分 "dev_user_header_alice" 取得有效 JWT Token
    And 調用端已準備 Header 參數
      | X-Developer-UserId  |
      | dev_user_header_bob |
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Alice Private Header App",
        "appType": "Web",
        "redirectUris": ["https://alice.com/cb"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "AliceHeaderCreatedAppId" 為 JSON 欄位 "$.id"

    # Bob 登入並嘗試存取 Alice 建立的 App，應收到 403（因為 App 擁有者為 Alice，而非被偽造的 Bob）
    Given 調用端已使用開發者身分 "dev_user_header_bob" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{AliceHeaderCreatedAppId}}"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Alice 登入並查詢自己建立的 App，應該成功取得且 displayName 相符
    Given 調用端已使用開發者身分 "dev_user_header_alice" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{AliceHeaderCreatedAppId}}"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑      | 驗證方式   | 預期值                   |
      | $.displayName | 字串等於   | Alice Private Header App |

  # =========================================================================
  # 【新 Blocker 驗證】偽造對稱金鑰或非 AuthServer 簽發之 Token 必須被拒絕 (401)
  # =========================================================================

  Scenario: 使用自製對稱金鑰簽發的偽造 Token 存取受保護端點應回傳 401
    Given 調用端使用自製對稱金鑰簽發偽造 JWT Token 冒充 "dev_user_attacker"
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 使用非 AuthServer 的未知 RSA 金鑰簽發之偽造 Token 存取受保護端點應回傳 401
    Given 調用端使用未知 RSA 私鑰簽發偽造 JWT Token 冒充 "dev_user_attacker"
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 使用偽造 Issuer 的 JWT Token 存取受保護端點應回傳 401
    Given 調用端使用偽造 Issuer 的 JWT Token 冒充 "dev_user_attacker"
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 使用偽造 Audience 的 JWT Token 存取受保護端點應回傳 401
    Given 調用端使用偽造 Audience 的 JWT Token 冒充 "dev_user_attacker"
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  # =========================================================================
  # 【Blocker 3 驗證】跨使用者操作 App 資源應回傳 403 Forbidden 防止 IDOR 越權存取
  # =========================================================================

  Scenario: 跨使用者查詢、修改、刪除 App 詳細資訊應回傳 403
    # Alice 建立專屬 App
    Given 調用端已使用開發者身分 "dev_user_alice" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Alice Confidential Web App",
        "appType": "Web",
        "redirectUris": ["https://alice.com/oauth/cb"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "AliceAppId" 為 JSON 欄位 "$.id"

    # Bob 嘗試查詢 Alice 的 App -> 403
    Given 調用端已使用開發者身分 "dev_user_bob" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{AliceAppId}}"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Bob 嘗試修改 Alice 的 App -> 403
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Hacked Display Name By Bob"
      }
      """
    When 調用端發送 "PUT" 請求至 "/api/v1/developer/apps/{{AliceAppId}}"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Bob 嘗試刪除 Alice 的 App -> 403
    When 調用端發送 "DELETE" 請求至 "/api/v1/developer/apps/{{AliceAppId}}"
    Then 調用端應收到 HTTP 狀態碼為 "403"

  Scenario: 跨使用者存取金鑰、輪替金鑰、作廢舊金鑰應回傳 403
    # Alice 建立專屬 App
    Given 調用端已使用開發者身分 "dev_user_alice" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Alice Payment Portal App",
        "appType": "Web",
        "redirectUris": ["https://alice.com/cb"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "AliceKeyAppId" 為 JSON 欄位 "$.id"

    # Bob 嘗試讀取 Alice 的金鑰 -> 403
    Given 調用端已使用開發者身分 "dev_user_bob" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{AliceKeyAppId}}/credentials"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Bob 嘗試觸發 Alice 的金鑰輪替 -> 403
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{AliceKeyAppId}}/rotate-secret"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Bob 嘗試作廢 Alice 的舊金鑰 -> 403
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{AliceKeyAppId}}/revoke-retiring-secret"
    Then 調用端應收到 HTTP 狀態碼為 "403"

  Scenario: 跨使用者送出審核申請與查詢審核狀態應回傳 403
    # Alice 建立專屬 App
    Given 調用端已使用開發者身分 "dev_user_alice" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Alice App For Review",
        "appType": "Web",
        "redirectUris": ["https://alice.com/cb"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "AliceReviewAppId" 為 JSON 欄位 "$.id"

    # Bob 嘗試替 Alice 送出審核申請 -> 403
    Given 調用端已使用開發者身分 "dev_user_bob" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "notes": "Bob is trying to submit review"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{AliceReviewAppId}}/submit-review"
    Then 調用端應收到 HTTP 狀態碼為 "403"

    # Bob 嘗試查詢 Alice 的審核狀態 -> 403
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{AliceReviewAppId}}/review-status"
    Then 調用端應收到 HTTP 狀態碼為 "403"

  # =========================================================================
  # 【Major 1 驗證】開發者帳號狀態落地 PostgreSQL 資料庫與持久化
  # =========================================================================

  Scenario: 開發者啟用狀態持久化至資料庫
    Given 調用端已使用開發者身分 "dev_user_persisted" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "organizationName": "Persistent Cloud Corp",
        "contactEmail": "admin@cloudcorp.com",
        "acceptAgreement": true
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/account/enable"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值                |
      | $.isDeveloperEnabled  | 布林值等於 | true                  |
      | $.organizationName    | 字串等於   | Persistent Cloud Corp |
      | $.contactEmail        | 字串等於   | admin@cloudcorp.com   |
    When 調用端發送 "GET" 請求至 "/api/v1/developer/account/status"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值                |
      | $.isDeveloperEnabled  | 布林值等於 | true                  |
      | $.organizationName    | 字串等於   | Persistent Cloud Corp |
      | $.contactEmail        | 字串等於   | admin@cloudcorp.com   |
    And 驗證資料庫中存在開發者 "dev_user_persisted" 狀態紀錄

