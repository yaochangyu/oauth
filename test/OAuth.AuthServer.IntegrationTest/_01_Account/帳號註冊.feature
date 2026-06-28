Feature: 帳號註冊

    Background:
        Given 初始化測試伺服器

    Scenario: 成功註冊新帳號
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "test@example.com",
          "password": "Test1234",
          "displayName": "測試使用者"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Then 預期回傳內容中路徑 "$.email" 的"字串等於" "test@example.com"

    Scenario: 重複 Email 註冊失敗
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "duplicate@example.com",
          "password": "Test1234"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "duplicate@example.com",
          "password": "Test1234"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "409"

    Scenario: 密碼不符規則
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "weak@example.com",
          "password": "123"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "400"
