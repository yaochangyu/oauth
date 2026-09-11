Feature: 帳號登入與登出

    Background:
        Given 初始化測試伺服器

    Scenario: 使用正確帳密登入成功
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.1.1        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456",
          "returnUrl": "/connect/authorize?client_id=mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 預期回傳內容中路徑 "$.success" 的"布林值等於" "True"

    Scenario: 密碼錯誤應回傳 401
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.1.2        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "login-wrongpw@example.com",
          "password": "Test1234"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "login-wrongpw@example.com",
          "password": "WrongPassword1"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "401"

    Scenario: 非法 returnUrl 應被拒絕（防止 Open Redirect）
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.1.3        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456",
          "returnUrl": "https://evil.example.com/phishing"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "400"

    Scenario: 登入後可查詢目前使用者，登出後應失效
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.1.4        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        When 調用端發送 "GET" 請求至 "api/v1/account/me"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 預期回傳內容中路徑 "$.email" 的"字串等於" "admin@localhost"
        When 調用端發送 "POST" 請求至 "api/v1/account/logout"
        Then 預期得到 HttpStatusCode 為 "204"
        When 調用端發送 "GET" 請求至 "api/v1/account/me"
        Then 預期得到 HttpStatusCode 為 "401"

    Scenario: 超過每分鐘 5 次登入嘗試應回傳 429
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.1.99       |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "email": "login-ratelimit@example.com",
          "password": "Test1234"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "login-ratelimit@example.com",
          "password": "WrongPassword1"
        }
        """
        When 調用端連續發送 6 次 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "429"
