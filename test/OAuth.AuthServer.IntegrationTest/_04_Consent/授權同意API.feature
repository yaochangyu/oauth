Feature: 授權同意 API

    Background:
        Given 初始化測試伺服器

    Scenario: 未登入呼叫 consent-info 應回傳 401
        When 調用端發送 "GET" 請求至 "api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client"
        Then 預期得到 HttpStatusCode 為 "401"

    Scenario: 登入後以合法 returnUrl 可取得同意資訊
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.2.1        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        When 調用端發送 "GET" 請求至 "api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client%26scope%3Dopenid%2520profile"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 預期回傳內容中路徑 "$.clientId" 的"字串等於" "mvc-client"

    Scenario: 登入後以非法 returnUrl 應被拒絕（防止 Open Redirect）
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.2.2        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        When 調用端發送 "GET" 請求至 "api/v1/connect/consent-info?returnUrl=https%3A%2F%2Fevil.example.com"
        Then 預期得到 HttpStatusCode 為 "400"

    Scenario: 同意後應回傳含 __ct token 的跳轉網址
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.2.3        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid",
          "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 回傳內容中路徑 "$.redirectUrl" 應包含子字串 "__ct="

    Scenario: 拒絕同意應回傳含 __ct token 的跳轉網址
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.2.4        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid",
          "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-deny"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 回傳內容中路徑 "$.redirectUrl" 應包含子字串 "__ct="

    Scenario: 同意端點使用非法 returnUrl 應被拒絕
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.2.5        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "userName": "admin",
          "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        Given 調用端已準備 Body 參數(Json)
        """
        {
          "returnUrl": "https://evil.example.com/steal",
          "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "400"
