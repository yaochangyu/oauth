Feature: 授權管理與級聯撤銷

  Background:
    Given 初始化測試伺服器
    And 資料庫中存在使用者 "user_consent_01" 且密碼為 "Password123" 暱稱為 "授權用戶甲"
    And 資料庫中存在使用者 "user_consent_02" 且密碼為 "Password123" 暱稱為 "授權用戶乙"
    And 系統中存在第三方應用程式 ClientId "third-party-app-1" 顯示名稱 "第三方看板 App"
    And 系統中存在第三方應用程式 ClientId "third-party-app-2" 顯示名稱 "數據分析平台"

  Scenario: 查詢已授權第三方應用程式清單
    Given 使用者 "user_consent_01" 已授權第三方應用程式 "third-party-app-1" 擁有 Scopes "profile email offline_access" 並取得授權識別碼儲存至 "AuthId1"
    And 調用端已使用使用者身分 "user_consent_01" 取得有效 JWT Token
    When 調用端發送 "GET" 請求至 "api/v1/account/consents"
    Then 調用端應收到 HTTP 狀態碼為 "200"
    And 回應內容驗證
      | 欄位路徑                  | 驗證方式   | 預期值           |
      | $[0].clientId             | 字串等於   | third-party-app-1|
      | $[0].clientDisplayName    | 字串等於   | 第三方看板 App   |

  Scenario: 撤銷授權後級聯作廢 Refresh Token 且後續換發失敗
    Given 使用者 "user_consent_01" 已授權第三方應用程式 "third-party-app-1" 擁有 Scopes "profile email offline_access" 並取得授權識別碼儲存至 "AuthIdToRevoke"
    And 為授權 "AuthIdToRevoke" 簽發 Refresh Token 儲存至 "UserRefreshToken"
    # 撤銷前：Refresh Token 換發應成功
    When 使用 Refresh Token "UserRefreshToken" 透過 OpenIddict 換發 Token
    Then 換發結果應成功並取得新的 Access Token
    # 使用者透過 Account Portal 撤銷授權
    Given 調用端已使用使用者身分 "user_consent_01" 取得有效 JWT Token
    When 調用端發送 "DELETE" 請求至 "api/v1/account/consents/{{AuthIdToRevoke}}"
    Then 調用端應收到 HTTP 狀態碼為 "204"
    # 撤銷後：Refresh Token 換發應收到 invalid_grant
    When 使用 Refresh Token "UserRefreshToken" 透過 OpenIddict 換發 Token
    Then 換發結果應失敗並帶有錯誤碼 "invalid_grant"

  Scenario: 嘗試撤銷他人授權應被拒絕以防止 IDOR 攻擊
    Given 使用者 "user_consent_02" 已授權第三方應用程式 "third-party-app-2" 擁有 Scopes "profile email" 並取得授權識別碼儲存至 "VictimAuthId"
    # 攻擊者 (user_consent_01) 試圖刪除被害者 (user_consent_02) 的授權
    Given 調用端已使用使用者身分 "user_consent_01" 取得有效 JWT Token
    When 調用端發送 "DELETE" 請求至 "api/v1/account/consents/{{VictimAuthId}}"
    Then 調用端應收到 HTTP 狀態碼為 "403"
