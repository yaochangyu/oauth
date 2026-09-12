Feature: 跨模組真實場景整合測試

    Background:
        Given 初始化測試伺服器

    # 場景 1：建立會員（註冊流程）
    Scenario: 場景一_建立會員註冊流程與登入驗證
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.3.1        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "scenario_user_01@example.com",
            "email": "scenario_user_01@example.com",
            "password": "Password123!",
            "displayName": "真實場景用戶一",
            "phoneNumber": "0912345678"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        And 預期回傳內容中路徑 "$.email" 的"字串等於" "scenario_user_01@example.com"
        And 預期回傳內容中路徑 "$.displayName" 的"字串等於" "真實場景用戶一"
        # 驗證新註冊會員可正常登入
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "scenario_user_01@example.com",
            "password": "Password123!"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        And 預期回傳內容中路徑 "$.success" 的"布林值等於" "true"

    # 場景 2：第三方應用程式註冊（Developer 設定要求 Scope）
    Scenario: 場景二_第三方應用程式註冊與Scope設定
        Given 調用端以開發者身分 "dev_user_scenario_02" 呼叫 Developer WebAPI
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "displayName": "機密型電商整合應用程式",
            "appType": "Web",
            "description": "真實場景測試用機密型 App",
            "redirectUris": ["https://ecommerce.example.com/signin-oidc"],
            "postLogoutRedirectUris": ["https://ecommerce.example.com/signout-callback-oidc"],
            "requestedScopes": ["openid", "profile", "email", "phone"]
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/developer/apps"
        Then 預期得到 HttpStatusCode 為 "201"
        And 預期回傳內容中路徑 "$.displayName" 的"字串等於" "機密型電商整合應用程式"
        And 預期回傳內容中路徑 "$.appType" 的"字串等於" "Web"
        And 預期回傳內容中路徑 "$.clientType" 的"字串等於" "confidential"
        And 預期回傳內容中路徑 "$.requiresPkce" 的"布林值等於" "true"
        And 從回應中儲存變數 "CreatedAppId" 為 JSON 欄位 "$.id"
        And 從回應中儲存變數 "CreatedClientId" 為 JSON 欄位 "$.clientId"
        # 重新查詢該 App 確認 Scope 設定確實儲存
        When 調用端發送 "GET" 請求至 "api/v1/developer/apps/{{CreatedAppId}}"
        Then 預期得到 HttpStatusCode 為 "200"
        And 預期回傳內容中路徑 "$.requestedScopes" 包含字串清單
            | scope   |
            | openid  |
            | profile |
            | email   |
            | phone   |

    # 場景 3：使用者登入並同意授權（顯示對應 Scope、換取 Token、UserInfo 資料與 Scope 相符）
    Scenario: 場景三_使用者登入並同意授權換取Token與UserInfo驗證
        Given 調用端呼叫 AuthServer
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.3.3        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "admin",
            "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        # 查詢同意頁面資訊
        When 調用端發送 "GET" 請求至 "api/v1/connect/consent-info?returnUrl=%2Fconnect%2Fauthorize%3Fclient_id%3Dmvc-client%26scope%3Dopenid%2520profile%2520email%26response_type%3Dcode%26redirect_uri%3Dhttps%253A%252F%252Flocalhost%253A5101%252Fsignin-oidc%26code_challenge%3DE9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM%26code_challenge_method%3DS256"
        Then 預期得到 HttpStatusCode 為 "200"
        And 預期回傳內容中路徑 "$.clientId" 的"字串等於" "mvc-client"
        # 使用者同意授權
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid profile email&response_type=code&redirect_uri=https://localhost:5101/signin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256",
            "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "AcceptRedirectUrl" 為 JSON 欄位 "$.redirectUrl"
        # 跟隨同意後的跳轉取得授權碼
        When 調用端發送 "GET" 請求至 "{{AcceptRedirectUrl}}"
        Then 預期得到 HttpStatusCode 為 "302"
        And 回應標頭 "Location" 應包含 "https://localhost:5101/signin-oidc?code="
        And 從回應標頭 "Location" 擷取 query 參數 "code" 儲存至 "AuthCode"
        # 呼叫 Token 端點以授權碼換發 Token
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=authorization_code&client_id=mvc-client&client_secret=mvc-client-secret&code={{AuthCode}}&redirect_uri=https://localhost:5101/signin-oidc&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "200"
        And 回傳的 Access Token 格式為 JWT
        And 從回應中儲存變數 "IssuedAccessToken" 為 JSON 欄位 "$.access_token"
        # 呼叫 UserInfo 端點驗證 Claims 符合同意的 Scopes（openid/profile/email）
        Given 調用端已準備 Header 參數
            | Authorization |
            | Bearer {{IssuedAccessToken}} |
        When 調用端發送 "GET" 請求至 "connect/userinfo"
        Then 預期得到 HttpStatusCode 為 "200"
        And 預期回傳內容中路徑 "$.email" 的"字串等於" "admin@localhost"
        And 預期回傳內容中路徑 "$.name" 的"字串等於" "admin@localhost"
        And 預期回傳內容中路徑 "$.address" 為空或不存在
        And 預期回傳內容中路徑 "$.phone_number" 為空或不存在

    # 場景 4：使用者拒絕同意（Deny，導回第三方並帶 access_denied，不核發 Token）
    Scenario: 場景四_使用者拒絕同意導回access_denied
        Given 調用端呼叫 AuthServer
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.3.4        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "admin",
            "password": "Admin@123456"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        # 使用者拒絕授權
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid profile&response_type=code&redirect_uri=https://localhost:5101/signin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256",
            "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-deny"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "DenyRedirectUrl" 為 JSON 欄位 "$.redirectUrl"
        # 跟隨拒絕後的跳轉
        When 調用端發送 "GET" 請求至 "{{DenyRedirectUrl}}"
        Then 預期得到 HttpStatusCode 為 "302"
        And 回應標頭 "Location" 應包含 "error=access_denied"
        And 回應標頭 "Location" 不應包含 "code="
        # 驗證無法換發 Token（因未核發 Authorization Code）
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=authorization_code&client_id=mvc-client&client_secret=mvc-client-secret&code=fake_denied_code&redirect_uri=https://localhost:5101/signin-oidc&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "400"
        And 預期回傳內容中路徑 "$.error" 的"字串等於" "invalid_grant"

    # 場景 5：第三方要求的 Scope 超出已核准範圍（拒絕或不核發超出範圍之 Token）
    Scenario: 場景五_第三方要求的Scope超出範圍被拒絕
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=client_credentials&client_id=webapi-client&client_secret=webapi-client-secret&scope=unauthorized_scope_xyz
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "400"
        And 預期回傳內容中路徑 "$.error" 的"字串等於" "invalid_scope"

    # 場景 6：同一使用者對多個第三方 App 授權與級聯撤銷管理
    Scenario: 場景六_多個第三方App授權管理與撤銷驗證
        Given 調用端呼叫 AuthServer
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.3.6        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "email": "multi_app_user@localhost",
            "displayName": "multi_app_user",
            "password": "Password123!"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "multi_app_user@localhost",
            "password": "Password123!"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        # App A (mvc-client) 同意授權並取得 Refresh Token A
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid profile offline_access&response_type=code&redirect_uri=https://localhost:5101/signin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256",
            "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "AppARedirectUrl" 為 JSON 欄位 "$.redirectUrl"
        When 調用端發送 "GET" 請求至 "{{AppARedirectUrl}}"
        Then 預期得到 HttpStatusCode 為 "302"
        And 從回應標頭 "Location" 擷取 query 參數 "code" 儲存至 "AppACode"
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=authorization_code&client_id=mvc-client&client_secret=mvc-client-secret&code={{AppACode}}&redirect_uri=https://localhost:5101/signin-oidc&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "RefreshTokenA" 為 JSON 欄位 "$.refresh_token"
        # App B (postman-client) 同意授權並取得 Refresh Token B
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "returnUrl": "/connect/authorize?client_id=postman-client&scope=openid profile offline_access&response_type=code&redirect_uri=https://oauth.pstmn.io/v1/callback&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256",
            "clientId": "postman-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "AppBRedirectUrl" 為 JSON 欄位 "$.redirectUrl"
        When 調用端發送 "GET" 請求至 "{{AppBRedirectUrl}}"
        Then 預期得到 HttpStatusCode 為 "302"
        And 從回應標頭 "Location" 擷取 query 參數 "code" 儲存至 "AppBCode"
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=authorization_code&client_id=postman-client&code={{AppBCode}}&redirect_uri=https://oauth.pstmn.io/v1/callback&code_verifier=dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "RefreshTokenB" 為 JSON 欄位 "$.refresh_token"
        # 查詢已授權 App 清單，取得 App A 的授權 ID 並執行撤銷
        Given 以使用者 "multi_app_user@localhost" 的身分呼叫 Account WebAPI
        When 調用端發送 "GET" 請求至 "api/v1/account/consents"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "AppAAuthId" 為 JSON 欄位 "$[?(@.clientId=='mvc-client')].authorizationId"
        When 調用端發送 "DELETE" 請求至 "api/v1/account/consents/{{AppAAuthId}}"
        Then 預期得到 HttpStatusCode 為 "204"
        # 驗證 App A 的 Refresh Token 換發失敗 (invalid_grant)
        Given 調用端呼叫 AuthServer
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=refresh_token&client_id=mvc-client&client_secret=mvc-client-secret&refresh_token={{RefreshTokenA}}
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "400"
        And 預期回傳內容中路徑 "$.error" 的"字串等於" "invalid_grant"
        # 驗證 App B 的 Refresh Token 換發仍然成功（證明撤銷互不干擾）
        Given 調用端已準備 Body 參數(Form)
        """
        grant_type=refresh_token&client_id=postman-client&refresh_token={{RefreshTokenB}}
        """
        When 調用端發送 Form "POST" 請求至 "connect/token"
        Then 預期得到 HttpStatusCode 為 "200"
        And 回傳的 Access Token 格式為 JWT

    # 場景 7：Authorization Code + Permanent Authorization 略過同意頁面
    Scenario: 場景七_PermanentAuthorization直接授權略過同意頁面
        Given 調用端呼叫 AuthServer
        Given 調用端已準備 Header 參數
            | X-Forwarded-For |
            | 10.0.3.7        |
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "email": "perm_user@localhost",
            "displayName": "perm_user",
            "password": "Password123!"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/register"
        Then 預期得到 HttpStatusCode 為 "201"
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "userName": "perm_user@localhost",
            "password": "Password123!"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/account/login"
        Then 預期得到 HttpStatusCode 為 "200"
        # 第一次發起授權（尚未同意過）：導向 /consent 頁面
        When 調用端發送 "GET" 請求至 "connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256"
        Then 預期得到 HttpStatusCode 為 "302"
        And 回應標頭 "Location" 應包含 "/consent?returnUrl="
        # 使用者完成同意並建立 Permanent Authorization
        Given 調用端已準備 Body 參數(Json)
        """
        {
            "returnUrl": "/connect/authorize?client_id=mvc-client&scope=openid profile&response_type=code&redirect_uri=https://localhost:5101/signin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256",
            "clientId": "mvc-client"
        }
        """
        When 調用端發送 "POST" 請求至 "api/v1/connect/consent-accept"
        Then 預期得到 HttpStatusCode 為 "200"
        And 從回應中儲存變數 "FirstAcceptRedirectUrl" 為 JSON 欄位 "$.redirectUrl"
        When 調用端發送 "GET" 請求至 "{{FirstAcceptRedirectUrl}}"
        Then 預期得到 HttpStatusCode 為 "302"
        And 回應標頭 "Location" 應包含 "https://localhost:5101/signin-oidc?code="
        # 第二次發起相同的授權請求：驗證 Permanent Authorization 生效，直接拿到 Code 略過同意
        When 調用端發送 "GET" 請求至 "connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256"
        Then 預期得到 HttpStatusCode 為 "302"
        And 回應標頭 "Location" 應包含 "https://localhost:5101/signin-oidc?code="
        And 回應標頭 "Location" 不應包含 "/consent"

