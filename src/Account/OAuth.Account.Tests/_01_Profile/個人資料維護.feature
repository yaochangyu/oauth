Feature: 個人資料維護

  Background:
    Given 初始化測試伺服器
    And 資料庫中存在使用者 "user_acc_01" 且密碼為 "Password123" 暱稱為 "測試小明"

  Scenario: 取得個人資料成功
    Given 調用端已使用使用者身分 "user_acc_01" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值               |
      | $.userId          | 字串等於   | user_acc_01          |
      | $.email           | 字串等於   | user_acc_01@example.com |
      | $.displayName     | 字串等於   | 測試小明             |

  Scenario: 修改個人暱稱與頭像成功
    Given 調用端已使用使用者身分 "user_acc_01" 取得有效 JWT Token
    And 調用端已準備 Body 參數(Json)
      """
      {
        "displayName": "小明改名卡",
        "avatarUrl": "https://example.com/avatar_new.png"
      }
      """
    When 調用端發送 "PUT" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑          | 驗證方式   | 預期值               |
      | $.displayName     | 字串等於   | 小明改名卡           |
      | $.avatarUrl       | 字串等於   | https://example.com/avatar_new.png |
    And 驗證資料庫中使用者 "user_acc_01" 的暱稱為 "小明改名卡"

  Scenario: 未攜帶 Token 查詢個人資料應被拒絕
    Given 調用端未帶入任何認證 Token
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "401"
