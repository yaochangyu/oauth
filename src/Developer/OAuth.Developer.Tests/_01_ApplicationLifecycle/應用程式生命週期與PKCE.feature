Feature: 應用程式生命週期與PKCE強制防護

  Background: 初始化測試環境
    Given 初始化測試伺服器
    And 調用端已準備 Header 參數
      | X-Developer-UserId   |
      | dev_user_test_001    |

  Scenario: 建立機密型 Web 應用程式並強制要求 PKCE
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "My Enterprise Web App",
        "appType": "Web",
        "description": "測試用企業 Web 應用程式",
        "redirectUris": ["https://myclient.com/callback"],
        "postLogoutRedirectUris": ["https://myclient.com/logout-callback"],
        "requestedScopes": ["openid", "profile", "email"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值                  |
      | $.displayName     | 字串等於   | My Enterprise Web App   |
      | $.appType         | 字串等於   | Web                     |
      | $.clientType      | 字串等於   | confidential            |
      | $.status          | 字串等於   | Sandbox                 |
      | $.requiresPkce    | 布林值等於 | true                    |
      | $.clientSecret    | 不為空     |                         |
    And 從回應中儲存變數 "SavedAppId" 為 JSON 欄位 "$.id"
    And 從回應中儲存變數 "SavedClientId" 為 JSON 欄位 "$.clientId"

  Scenario: 建立公開型 SPA 應用程式並強制要求 PKCE
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "My Vue SPA App",
        "appType": "SPA",
        "description": "測試用 SPA 應用程式",
        "redirectUris": ["https://myspa.com/callback"],
        "requestedScopes": ["openid", "profile"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值            |
      | $.displayName     | 字串等於   | My Vue SPA App    |
      | $.appType         | 字串等於   | SPA               |
      | $.clientType      | 字串等於   | public            |
      | $.requiresPkce    | 布林值等於 | true              |

  Scenario: 建立公開型 Mobile 應用程式並強制要求 PKCE
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "My Mobile iOS App",
        "appType": "Mobile",
        "description": "測試用 Mobile 應用程式",
        "redirectUris": ["com.mycompany.app://oauth-callback"],
        "requestedScopes": ["openid", "profile"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值              |
      | $.displayName     | 字串等於   | My Mobile iOS App   |
      | $.appType         | 字串等於   | Mobile              |
      | $.clientType      | 字串等於   | public              |
      | $.requiresPkce    | 布林值等於 | true                |

  Scenario: 查詢應用程式清單與單一 App 詳細資訊
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值   |
      | $[0].displayName  | 不為空     |          |

  Scenario: 送出 App 上線審核
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "App For Review",
        "appType": "Web",
        "redirectUris": ["https://example.com/callback"],
        "requestedScopes": ["openid", "profile"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 從回應中儲存變數 "SavedAppId" 為 JSON 欄位 "$.id"
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{SavedAppId}}/submit-review"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值      |
      | $.status          | 字串等於   | InReview    |
      | $.submittedAt     | 不為空     |             |
