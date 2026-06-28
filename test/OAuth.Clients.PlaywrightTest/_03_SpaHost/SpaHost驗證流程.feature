Feature: SPA Host 驗證流程

    Background:
        Given 開啟全新的瀏覽器視窗

    Scenario: 登入成功後停在 SPA Host 不發生 OIDC 迴圈
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應停在 SPA Host 頁面

    Scenario: 首頁顯示已登入狀態
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 首頁應顯示已登入文字

    Scenario: 個人資料顯示 Email
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 使用者前往 SPA 個人資料頁
        Then SPA 個人資料頁應顯示 "admin@localhost"

    Scenario: Access Token 有效標記顯示
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 使用者前往 SPA 個人資料頁
        Then SPA 個人資料頁應顯示 Access Token 有效標記

    Scenario: Claims 表格包含 role 和 admin
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 使用者前往 SPA 個人資料頁
        Then SPA Claims 表格應包含 "role" 和 "admin"
