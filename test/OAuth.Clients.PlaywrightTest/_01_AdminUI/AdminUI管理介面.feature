Feature: Admin UI 管理介面

    Background:
        Given 初始化 Auth 伺服器
        And 初始化 Admin UI 測試環境
        And 開啟全新的瀏覽器視窗
        And 已登入 Admin UI 管理介面

    Scenario: 首頁顯示 Dashboard
        When 開啟 Admin UI 首頁
        Then 應顯示 Dashboard 標題
        And 應顯示 Users、Roles、Scopes 導覽項目

    Scenario: Users 列表顯示 admin 帳號
        When 開啟 Users 管理頁面
        Then 應顯示至少一位使用者
        And 使用者列表應包含 "admin" 帳號

    Scenario: Users 搜尋 admin 可找到結果
        When 開啟 Users 管理頁面
        And 搜尋使用者名稱 "admin"
        Then 使用者列表應包含 "admin" 帳號

    Scenario: Users 點擊編輯進入編輯頁
        When 開啟 Users 管理頁面
        And 點擊第一筆使用者的編輯按鈕
        Then 頁面應跳轉至使用者編輯頁

    Scenario: Roles 列表顯示 admin 角色
        When 開啟 Roles 管理頁面
        Then 角色列表應包含 "admin" 角色

    Scenario: Applications 列表顯示 seeded 應用程式
        When 開啟 Applications 管理頁面
        Then 應顯示 OAuth Applications 標題

    Scenario: Scopes 列表顯示 api scope
        When 開啟 Scopes 管理頁面
        Then Scope 列表應包含 "api" scope

    # ── Users 編輯頁 ──────────────────────────────────────────────────────────

    Scenario: Users 編輯頁顯示使用者資訊
        When 開啟 Users 管理頁面
        And 點擊第一筆使用者的編輯按鈕
        Then 頁面應跳轉至使用者編輯頁
        And 編輯頁應顯示使用者名稱 "admin"

    Scenario: Users 編輯頁已勾選 admin 角色
        When 開啟 Users 管理頁面
        And 點擊第一筆使用者的編輯按鈕
        Then 頁面應跳轉至使用者編輯頁
        And 編輯頁 "admin" 角色核取方塊應為勾選

    # ── Roles CRUD ────────────────────────────────────────────────────────────

    Scenario: Roles 新增角色後出現在列表
        When 開啟 Roles 管理頁面
        And 新增角色 "test-role"
        Then 角色列表應包含 "test-role" 角色

    Scenario: Roles 刪除角色後從列表消失
        When 開啟 Roles 管理頁面
        And 新增角色 "to-delete-role"
        And 刪除角色 "to-delete-role"
        Then 角色列表應不包含 "to-delete-role" 角色

    # ── Applications ──────────────────────────────────────────────────────────

    Scenario: Applications 列表顯示 mvc-client
        When 開啟 Applications 管理頁面
        Then Application 列表應包含 "mvc-client"

    Scenario: Applications 點擊編輯進入編輯頁並顯示 Client Id
        When 開啟 Applications 管理頁面
        And 點擊 "mvc-client" 的 Application 編輯按鈕
        Then 頁面應跳轉至 Application 編輯頁
        And 編輯頁應顯示 Client Id "mvc-client"

    # ── Scopes ────────────────────────────────────────────────────────────────

    Scenario: Scopes 點擊編輯進入編輯頁並顯示 Scope Name
        When 開啟 Scopes 管理頁面
        And 點擊 "api" Scope 的編輯按鈕
        Then 頁面應跳轉至 Scope 編輯頁
        And 編輯頁應顯示 Scope Name "api"

    # ── 反向案例 ──────────────────────────────────────────────────────────────

    Scenario: Users 搜尋不存在的帳號應顯示空列表
        When 開啟 Users 管理頁面
        And 搜尋使用者名稱 "nonexistent-user-xyz"
        Then 使用者列表應為空

    Scenario: Roles 新增重複角色名稱應顯示錯誤訊息
        When 開啟 Roles 管理頁面
        And 新增角色 "admin"
        Then 應顯示操作失敗的錯誤訊息

    Scenario: Applications 搜尋不存在的 Client 應顯示空列表
        When 開啟 Applications 管理頁面
        And 搜尋 Application "nonexistent-client-xyz"
        Then Application 列表應為空
