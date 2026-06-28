# Client OIDC 流程驗證計畫

## 目標
1. 驗證各 Client 專案不會有 OIDC 無限迴圈問題
2. 修復 MVC Client 缺少 `roles` scope 的問題，確保集中管理 admin 生效

## 已知根因
- OIDC 迴圈只會發生在同時使用 `AddIdentity`（設 `DefaultAuthenticateScheme = Identity.Application`）
  + `AddAuthentication`（Cookie scheme）的衝突場景
- MVC Client 只用 Cookie + OIDC，不涉及 Identity，**不會迴圈**
- WebAPI Client 用 JWT Bearer，無 OIDC 流程

## 集中管理 Admin 架構

```
AuthServer（唯一 user store）
  └── admin 帳號 + admin role（已由 AdminUserSeeder 建立）
  └── 發 JWT 時：若 client 請求 roles scope → 嵌入 role claims（已實作）
      ↓
各 Client App
  └── 驗 JWT → 讀 role claims → [Authorize(Roles = "admin")]
  └── 不需要自己的 user store，不需要 seed admin
```

## 前置條件
- AuthServer（https://localhost:7001）運行中
- MVC Client（https://localhost:5101）運行中

## 實作步驟

- [x] **步驟 1：MVC Client 加入 `roles` scope**
  - 在 `OAuth.Client.Mvc/Program.cs` 的 OIDC scope 列表加入 `options.Scope.Add("roles")`
  - 修正 `appsettings.Development.json` Authority 從 `7098` 改為 `7001`
  - 加入 `OnUserInformationReceived` event，把 UserInfo 的 `role` 手動加到 ClaimsPrincipal
  - 設定 `TokenValidationParameters.RoleClaimType = "role"` 讓 ID token roles 被正確識別

- [x] **步驟 2：啟動 MVC Client 並建立 Fixture**
  - 在既有 `OAuth.E2E.WebwrightTest` 專案加入 `MvcClientFixture.cs`

- [x] **步驟 3：建立 MVC Client E2E 測試**
  - `Login_成功後無OIDC迴圈_停在MvcClient`
  - `Profile_顯示使用者名稱`
  - `Profile_JWT包含role_admin_claim`
  - `Profile_AccessToken_有值`

- [x] **步驟 4：Build 並執行測試**
  - 14/14 全部通過（Admin UI + MVC Client）

## 完成條件（已達成）
- ✅ MVC Client 登入流程無迴圈（驗證：URL 停在 5101，不在 7001）
- ✅ MVC Client JWT 包含 admin role claim
- ✅ 全部 14 個測試通過（2026-06-28）
