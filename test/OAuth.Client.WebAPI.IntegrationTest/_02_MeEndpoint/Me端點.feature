# language: en
Feature: Me Endpoint
  /api/v1/me 端點測試

  Background:
    Given WebAPI 已啟動
    And 測試環境已準備

  Scenario: 取得目前使用者資訊成功
    Given 已生成有效的 JWT Token
    When 以該 Token 發送 GET 請求到 /api/v1/me
    Then 應返回 200 OK
    And 回應包含 sub 欄位
    And 回應包含 name 欄位
    And 回應包含 email 欄位

  Scenario: 無 Token 存取 /api/v1/me 應返回 401
    When 不提供 Token 發送 GET 請求到 /api/v1/me
    Then 應返回 401 Unauthorized

  Scenario: 無效 Token 存取 /api/v1/me 應返回 401
    Given 已生成無效的 JWT Token（簽名錯誤）
    When 以該 Token 發送 GET 請求到 /api/v1/me
    Then 應返回 401 Unauthorized
