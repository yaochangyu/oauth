Feature: AuthServer 認證站（Headless Login / Consent / Register / Error）

    Background:
        Given 初始化 Auth 伺服器
        And 初始化 MVC Client 測試環境
        And 開啟全新的瀏覽器視窗

    Scenario: 登入成功後導向同意頁面（完成授權碼流程的第一步）
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面

    Scenario: 登入失敗顯示錯誤訊息
        When 使用者前往 AuthServer 登入頁
        And 使用者輸入帳號 "admin" 密碼 "WrongPassword1" 登入
        Then 登入頁應顯示錯誤訊息

    Scenario: 同意頁顯示 Client 名稱與請求的 scopes
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 同意頁應顯示應用程式名稱 "MVC Client"
        And 同意頁面應列出請求的 scopes

    Scenario: 同意後完成授權跳轉回 MVC Client（完整流程）
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 用戶點擊同意
        Then 不應顯示同意頁面
        And 應完成授權跳轉至 MVC Client

    Scenario: 拒絕同意後應回傳授權失敗
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        And 用戶點擊拒絕
        Then 應顯示授權錯誤訊息 "access_denied"

    Scenario: 非法 returnUrl 登入應被擋下且不會跳轉離開登入頁
        When 使用者前往帶有非法 returnUrl 的 AuthServer 登入頁
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 登入頁應顯示錯誤訊息
        And 頁面不應跳轉離開登入頁

    Scenario: 註冊新帳號成功
        When 使用者前往 AuthServer 註冊頁
        And 使用者填寫註冊表單並送出
        Then 應顯示註冊成功訊息

    Scenario: Error 頁面顯示錯誤代碼
        When 使用者前往 AuthServer 錯誤頁 "access_denied"
        Then Error 頁面應顯示錯誤代碼 "access_denied"
