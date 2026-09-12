Feature: 雙金鑰輪替與零停機過渡機制

  Background: 初始化測試環境
    Given 初始化測試伺服器
    And 調用端已準備 Header 參數
      | X-Developer-UserId   |
      | dev_user_test_002    |

  Scenario: 完整雙金鑰輪替生命週期驗證（建立 -> 輪替 -> 雙金鑰並存驗證 -> 廢止舊金鑰 -> 舊金鑰失效驗證）
    # 步驟 1: 建立機密型 Web App，取得初始金鑰
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "Payment Gateway Service",
        "appType": "Web",
        "description": "生產環境金流串接系統",
        "redirectUris": ["https://pay.example.com/oauth/callback"],
        "requestedScopes": ["openid", "profile", "api"]
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps"
    Then 調用端應收到 HTTP 狀態碼為 "201"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值                  |
      | $.clientType      | 字串等於   | confidential            |
      | $.clientSecret    | 不為空     |                         |
    And 從回應中儲存變數 "SavedAppId" 為 JSON 欄位 "$.id"
    And 從回應中儲存變數 "SavedClientId" 為 JSON 欄位 "$.clientId"
    And 從回應中儲存變數 "SavedSecret" 為 JSON 欄位 "$.clientSecret"

    # 步驟 2: 驗證初始金鑰有效
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "{{SavedClientId}}",
        "clientSecret": "{{SavedSecret}}"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/validate-credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值 |
      | $.isValid         | 布林值等於 | true   |

    # 步驟 3: 查詢金鑰狀態（無 Retiring 金鑰）
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{SavedAppId}}/credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值 |
      | $.hasActiveSecret     | 布林值等於 | true   |
      | $.hasRetiringSecret   | 布林值等於 | false  |

    # 步驟 4: 觸發金鑰輪替 (Rotate Secret)
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{SavedAppId}}/rotate-secret"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑                  | 驗證方式   | 預期值 |
      | $.newSecret               | 不為空     |        |
      | $.retiringSecretExpiresAt | 不為空     |        |
    And 從回應中儲存變數 "SavedNewSecret" 為 JSON 欄位 "$.newSecret"

    # 步驟 5: 查詢金鑰狀態（已有 Retiring 金鑰與過期時間）
    When 調用端發送 "GET" 請求至 "/api/v1/developer/apps/{{SavedAppId}}/credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑              | 驗證方式   | 預期值 |
      | $.hasActiveSecret     | 布林值等於 | true   |
      | $.hasRetiringSecret   | 布林值等於 | true   |

    # 步驟 6: 零停機保證：使用新金鑰驗證成功
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "{{SavedClientId}}",
        "clientSecret": "{{SavedNewSecret}}"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/validate-credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值 |
      | $.isValid         | 布林值等於 | true   |

    # 步驟 7: 零停機保證：在 7 天緩衝期內使用舊金鑰亦能成功通過驗證！
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "{{SavedClientId}}",
        "clientSecret": "{{SavedSecret}}"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/validate-credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值 |
      | $.isValid         | 布林值等於 | true   |

    # 步驟 8: 線上伺服器更新完成，手動發送立即作廢舊金鑰 (Revoke Retiring Secret)
    When 調用端發送 "POST" 請求至 "/api/v1/developer/apps/{{SavedAppId}}/revoke-retiring-secret"
    Then 調用端應收到 HTTP 狀態碼為 "200"

    # 步驟 9: 驗證舊金鑰立即失效 (invalid_client)
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "{{SavedClientId}}",
        "clientSecret": "{{SavedSecret}}"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/validate-credentials"
    Then 調用端應收到 HTTP 狀態碼為 "400"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值         |
      | $.isValid         | 布林值等於 | false          |
      | $.error           | 字串等於   | invalid_client |

    # 步驟 10: 驗證新金鑰依然有效
    Given 調用端已準備 Body 參數(Json)
      """
      {
        "clientId": "{{SavedClientId}}",
        "clientSecret": "{{SavedNewSecret}}"
      }
      """
    When 調用端發送 "POST" 請求至 "/api/v1/developer/sandbox/validate-credentials"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值 |
      | $.isValid         | 布林值等於 | true   |
