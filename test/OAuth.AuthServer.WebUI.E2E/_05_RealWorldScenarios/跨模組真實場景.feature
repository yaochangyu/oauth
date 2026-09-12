Feature: 跨模組真實場景 E2E 測試

    Background:
        Given 初始化 Auth 伺服器
        And 初始化 MVC Client 測試環境
        And 開啟全新的瀏覽器視窗

    # 場景 1：建立會員（註冊流程）並透過第三方 App 登入授權
    Scenario: 場景一_真實使用者註冊新會員並透過第三方App成功登入
        When 使用者前往註冊頁
        And 使用者填寫隨機會員註冊表單密碼 "Password123!"
        Then 應顯示註冊成功訊息
        When 使用者透過 "mvc-client" 發起授權
        And 使用者以新註冊的帳號登入
        Then 應顯示同意頁面

    # 場景 2 & 3：使用者登入並同意授權（顯示對應 Scope、換取 Token、導回 MVC Client）
    Scenario: 場景三_第三方App發起授權碼流程用戶同意後成功跳轉
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面
        And 同意頁面應列出請求的 scopes
        When 用戶點擊同意
        Then 不應顯示同意頁面
        And 應完成授權跳轉至 MVC Client
        And 驗證 UserInfo 包含同意之 Claims 且不包含未同意之 Claims

    # 場景 4：使用者拒絕同意（Deny，導回第三方並帶 access_denied，不核發 Token）
    Scenario: 場景四_使用者拒絕同意導回第三方並顯示授權失敗
        Given 使用者尚未登入
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面
        When 用戶點擊拒絕
        Then 應顯示授權錯誤訊息 "access_denied"

    # 場景 7：Permanent Authorization 略過同意（兩階段登入驗證）
    Scenario: 場景七_PermanentAuthorization兩階段登入直接授權略過同意頁面
        Given 使用者尚未登入
        # 第一階段：首次發起授權，需顯示同意頁面並完成授權
        When 使用者透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應顯示同意頁面
        When 用戶點擊同意
        Then 應完成授權跳轉至 MVC Client
        # 第二階段：結束 session 後再次登入同一 App，驗證 Permanent Authorization 生效略過同意頁面
        When 使用者結束工作階段並再次透過 "mvc-client" 發起授權
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 不應顯示同意頁面
        And 應完成授權跳轉至 MVC Client

    # SPA Host 跨模組 PKCE 整合
    Scenario: 場景外加_SPA應用程式透過PKCE登入授權
        Given 初始化 SPA Host 測試環境
        When 使用者前往 SPA Host 首頁
        And 使用者點擊 SPA 登入按鈕
        And 使用者輸入帳號 "admin" 密碼 "Admin@123456" 登入
        Then 應停在 SPA Host 頁面
