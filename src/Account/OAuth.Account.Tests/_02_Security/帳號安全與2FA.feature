Feature: 帳號安全與2FA

  Background:
    Given 初始化測試伺服器
    And 資料庫中存在使用者 "user_sec_01" 且密碼為 "OldPassword123" 暱稱為 "安全測試用戶"

  Scenario: 密碼修改成功
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "currentPassword": "OldPassword123",
        "newPassword": "NewPassword888",
        "confirmPassword": "NewPassword888"
      }
      """
    When 調用端發送 "POST" 請求至 "api/v1/account/security/change-password"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值               |
      | $.message         | 包含字串   | 成功                 |

  Scenario: 密碼修改時舊密碼錯誤應失敗
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "currentPassword": "WrongOldPassword",
        "newPassword": "NewPassword888",
        "confirmPassword": "NewPassword888"
      }
      """
    When 調用端發送 "POST" 請求至 "api/v1/account/security/change-password"
    Then 調用端應收到 HTTP 狀態碼為 "400"

  Scenario: 密碼長度不足或不符政策應失敗
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "currentPassword": "OldPassword123",
        "newPassword": "123",
        "confirmPassword": "123"
      }
      """
    When 調用端發送 "POST" 請求至 "api/v1/account/security/change-password"
    Then 調用端應收到 HTTP 狀態碼為 "400"

  Scenario: 查詢 2FA 狀態與產生 TOTP 密鑰 URI
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "api/v1/account/security/2fa-status"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑             | 驗證方式   | 預期值               |
      | $.isTwoFactorEnabled | 布林值等於 | false                |
    When 調用端發送 "POST" 請求至 "api/v1/account/security/2fa/generate-key"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值               |
      | $.sharedKey       | 不為空     | true                 |
      | $.authenticatorUri| 包含字串   | otpauth://totp/      |

  Scenario: 驗證並啟用 2FA TOTP 成功
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    When 調用端發送 "POST" 請求至 "api/v1/account/security/2fa/generate-key"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    When 產生 Authenticator 2FA 驗證碼並填入 Body 欄位 "code"
    And 調用端發送 "POST" 請求至 "api/v1/account/security/2fa/verify-and-enable"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值               |
      | $.message         | 包含字串   | 成功                 |
    And 驗證資料庫中使用者 "user_sec_01" 的雙層驗證狀態為 "true"

  Scenario: 停用 2FA 成功
    Given 調用端已使用使用者身分 "user_sec_01" 取得有效 JWT Token
    When 調用端發送 "POST" 請求至 "api/v1/account/security/2fa/disable"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 驗證資料庫中使用者 "user_sec_01" 的雙層驗證狀態為 "false"
