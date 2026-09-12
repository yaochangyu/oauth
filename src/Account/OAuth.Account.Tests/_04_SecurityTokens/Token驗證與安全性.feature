Feature: Token驗證與安全性

  Background:
    Given 初始化測試伺服器
    And 資料庫中存在使用者 "user_sec_tok_01" 且密碼為 "Password123" 暱稱為 "安全性驗證用戶"

  Scenario: 偽造對稱金鑰簽發的 Token 應被拒絕
    Given 調用端使用自製對稱金鑰簽發偽造 JWT Token 冒充 "user_sec_tok_01"
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 偽造未知 RSA 私鑰簽發的 Token 應被拒絕
    Given 調用端使用未知 RSA 私鑰簽發偽造 JWT Token 冒充 "user_sec_tok_01"
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 偽造 Issuer 的 Token 應被拒絕
    Given 調用端使用偽造 Issuer 的 JWT Token 冒充 "user_sec_tok_01"
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "401"

  Scenario: 偽造 Audience 的 Token 應被拒絕
    Given 調用端使用偽造 Audience 的 JWT Token 冒充 "user_sec_tok_01"
    When 調用端發送 "GET" 請求至 "api/v1/account/profile"
    Then 調用端應收到 HTTP 狀態碼為 "401"
