# language: en
Feature: Bearer Token Authentication
  Bearer Token 認證測試

  Background:
    Given WebAPI 已啟動
    And 測試環境已準備

  Scenario: 有效 Bearer Token 應返回 200
    Given 已生成有效的 JWT Token
    When 以該 Token 存取 /api/v1/me 端點
    Then 應返回 200 OK
    And 回應包含使用者資訊

  Scenario: 無 Bearer Token 應返回 401
    When 不提供 Token 存取 /api/v1/me 端點
    Then 應返回 401 Unauthorized

  Scenario: 無效 Bearer Token 應返回 401
    Given 已生成無效的 JWT Token（簽名錯誤）
    When 以該 Token 存取 /api/v1/me 端點
    Then 應返回 401 Unauthorized

  Scenario: 過期 Bearer Token 應返回 401
    Given 已生成過期的 JWT Token
    When 以該 Token 存取 /api/v1/me 端點
    Then 應返回 401 Unauthorized
