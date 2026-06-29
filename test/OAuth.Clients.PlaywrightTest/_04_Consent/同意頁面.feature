Feature: 同意頁面

    Background:
        Given 初始化 Auth 伺服器
        And 初始化 MVC Client 測試環境
        And 開啟全新的瀏覽器視窗

    Scenario: Explicit Client 授權前顯示同意頁面
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面
        And 同意頁面應列出請求的 scopes

    Scenario: 用戶同意後完成授權跳轉
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 用戶點擊同意
        Then 不應顯示同意頁面
        And 應完成授權跳轉至 MVC Client

    Scenario: 用戶拒絕同意後應回傳授權失敗
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 用戶點擊拒絕
        Then 應顯示授權錯誤訊息 "access_denied"

    Scenario: Implicit Client 不顯示同意頁面，直接完成授權
        Given 使用者尚未登入
        When 使用者透過 "mvc-implicit" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 不應顯示同意頁面
        And 應完成授權跳轉至 MVC Client
