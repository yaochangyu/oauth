# language: en
Feature: PKCE Authorization Flow
  PKCE 授權流程測試

  Background:
    Given WebAPI 已啟動
    And 測試環境已準備
    And AuthServer 已啟動

  Scenario: Authorization Code + PKCE 流程成功
    Given 已產生 code_verifier 與 code_challenge
    When 向 Authorization Server 發送授權請求
    And 用 authorization code 換取 access token
    Then 應收到有效的 access token
    And 能用該 token 存取 WebAPI
