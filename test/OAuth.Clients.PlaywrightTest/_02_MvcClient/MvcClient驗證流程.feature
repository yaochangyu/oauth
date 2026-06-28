Feature: MVC Client 驗證流程

    Background:
        Given 開啟全新的瀏覽器視窗

    Scenario: 登入成功後停在 MVC Client 不發生 OIDC 迴圈
        When 使用者前往 MVC Client 個人資料頁
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 若顯示同意頁面則同意授權
        Then 應停在 MVC Client 頁面
        And URL 不應包含 AuthServer 位址

    Scenario: 個人資料頁顯示歡迎文字
        When 使用者前往 MVC Client 個人資料頁
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 若顯示同意頁面則同意授權
        Then 個人資料頁應顯示歡迎文字

    Scenario: 個人資料包含 admin role claim
        When 使用者前往 MVC Client 個人資料頁
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 若顯示同意頁面則同意授權
        Then 個人資料頁應包含 "role" 及 "admin" claim

    Scenario: Access Token 有值
        When 使用者前往 MVC Client 個人資料頁
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 若顯示同意頁面則同意授權
        Then 個人資料頁應顯示 Access Token 有值
