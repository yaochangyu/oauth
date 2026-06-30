# language: en
Feature: Protected Resource
  受保護資源端點測試

  Background:
    Given WebAPI 已啟動
    And 測試環境已準備

  Scenario: 取得受保護資源成功
    Given 已生成有效的 JWT Token
    When 以該 Token 發送 GET 請求到 /api/v1/protected
    Then 應返回 200 OK
    And 回應包含 message 欄位
    And 回應包含 time 欄位

  Scenario: 無 Token 存取受保護資源應返回 401
    When 不提供 Token 發送 GET 請求到 /api/v1/protected
    Then 應返回 401 Unauthorized

  Scenario: 無效 Token 存取受保護資源應返回 401
    Given 已生成無效的 JWT Token（簽名錯誤）
    When 以該 Token 發送 GET 請求到 /api/v1/protected
    Then 應返回 401 Unauthorized
