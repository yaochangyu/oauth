Feature: 安全性驗證

    Background:
        Given 初始化測試伺服器

    Scenario: 未帶 Bearer Token 存取受保護端點應回傳 401
        When 調用端發送 "GET" 請求至 "connect/userinfo"
        Then 預期得到 HttpStatusCode 為 "401"

    Scenario: 帶無效 Bearer Token 存取受保護端點應回傳 401
        Given 調用端已準備 Header 參數
            | Authorization         |
            | Bearer invalid.token  |
        When 調用端發送 "GET" 請求至 "connect/userinfo"
        Then 預期得到 HttpStatusCode 為 "401"

    Scenario: PKCE 設定已正確啟用於 SPA Client
        When 調用端發送 "GET" 請求至 ".well-known/openid-configuration"
        Then 預期得到 HttpStatusCode 為 "200"
        Then Discovery Document 包含 PKCE 支援聲明

    Scenario: Token Endpoint 不接受 implicit flow
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=implicit&client_id=spa-client
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "400"
        Then 預期回傳內容中路徑 "$.error" 的"字串等於" "unsupported_grant_type"
