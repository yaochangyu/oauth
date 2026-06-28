Feature: Token 交換

    Background:
        Given 初始化測試伺服器

    Scenario: Discovery Document 可正常取得
        When 調用端發送 "GET" 請求至 ".well-known/openid-configuration"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 預期回傳內容中路徑 "$.token_endpoint" 的"字串等於" "https://localhost/connect/token"
        Then 預期回傳內容中路徑 "$.authorization_endpoint" 的"字串等於" "https://localhost/connect/authorize"
        Then 預期回傳內容中路徑 "$.userinfo_endpoint" 的"字串等於" "https://localhost/connect/userinfo"

    Scenario: 使用無效 client_id 換 token 應回傳 401
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=authorization_code&code=fake_code&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&client_id=invalid-client&code_verifier=dummyverifier
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "401"
        Then 預期回傳內容中路徑 "$.error" 的"字串等於" "invalid_client"

    Scenario: Client Credentials 取得 Access Token
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=client_credentials&client_id=webapi-client&client_secret=webapi-client-secret&scope=api
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "200"
        Then 預期回傳內容中路徑 "$.token_type" 的"字串等於" "Bearer"
        Then 回傳的 Access Token 格式為 JWT

    Scenario: 使用過期的 Refresh Token 應回傳 400
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=refresh_token&refresh_token=invalid_refresh_token&client_id=mvc-client&client_secret=mvc-client-secret
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "400"
        Then 預期回傳內容中路徑 "$.error" 的"字串等於" "invalid_grant"
