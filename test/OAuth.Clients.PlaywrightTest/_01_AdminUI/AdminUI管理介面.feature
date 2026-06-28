Feature: Admin UI 管理介面

    Background:
        Given 已登入 Admin UI 管理介面

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
