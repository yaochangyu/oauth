Feature: 開發者帳號與沙盒工具

  Background: 初始化測試環境
    Given 初始化測試伺服器
    And 調用端已準備 Header 參數
      | X-Developer-UserId   |
      | dev_user_test_003    |

  Scenario: 啟用開發者身分與取得狀態
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "organizationName": "Acme Software Inc",
        "contactEmail": "developer@acme.com",
        "acceptAgreement": true
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/account/enable"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值             |
      | $.isDeveloperEnabled  | 布林值等於 | true               |
      | $.organizationName    | 字串等於   | Acme Software Inc  |
    When 調用端發送 "GET" 請求至 "/api/v1/developer/account/status"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值 |
      | $.isDeveloperEnabled  | 布林值等於 | true   |

  Scenario: 沙盒工具自動生成 PKCE 授權網址
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "test_client_id",
        "redirectUri": "https://myapp.com/callback",
        "scope": "openid profile email",
        "codeChallenge": "E9Melhoa2OwvFrGMTJguCH5rtx64LxU93US3rKR3b7U",
        "codeChallengeMethod": "S256",
        "state": "xyz123"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/generate-authorize-url"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑            | 驗證方式   | 預期值                                |
      | $.authorizeUrl      | 包含字串   | code_challenge_method=S256            |
      | $.authorizeUrl      | 包含字串   | response_type=code                    |
