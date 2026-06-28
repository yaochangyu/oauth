# Admin UI 修復計畫

## 根因分析

Admin UI 完全無法操作，核心原因是**角色 (role) claim 從未被加入 token**，導致 `AdminOnly` 政策永遠無法通過，所有頁面都陷入 OIDC 登入重導迴圈。

### 問題清單

| # | 問題位置 | 問題描述 |
|---|---|---|
| 1 | `Authorize.cshtml.cs` | 從未呼叫 `userManager.GetRolesAsync()`，roles claim 從未被加入 identity |
| 2 | `TokenController.cs` | `GetDestinations()` 未處理 `Claims.Role`，即使角色存在也只會進 AccessToken，不進 IdentityToken |
| 3 | `UserInfoController.cs` | `/connect/userinfo` 回應不含 roles，`GetClaimsFromUserInfoEndpoint = true` 無法取得角色 |
| 4 | Admin UI `Program.cs` | OIDC options 未請求 `roles` scope；未設定 `RoleClaimType`，claim mapping 不正確 |

---

## 步驟

- [x] **Step 1 - 修正 Authorize.cshtml.cs：加入 roles claim**
  - 為什麼：Authorization Code Grant 的起點，roles 若不在此加入，後續 token 流程完全拿不到角色
  - 在 `principal.SetScopes()` 之前，呼叫 `userManager.GetRolesAsync(user)`
  - 當 `request.HasScope(Scopes.Roles)` 時，為每個 role 建立 `Claims.Role` claim，設 AccessToken + IdentityToken 目的地

- [x] **Step 2 - 修正 TokenController.cs：GetDestinations 加入 Claims.Role**
  - 為什麼：`TokenController.Exchange` 在 code 兌換時呼叫 `identity.SetDestinations(GetDestinations)` 會覆蓋所有 claim 的目的地；若未處理 `Claims.Role`，roles 只進 AccessToken 不進 IdentityToken
  - 在 switch 加入 `Claims.Role => [Destinations.AccessToken, Destinations.IdentityToken]`

- [x] **Step 3 - 修正 UserInfoController.cs：加入 roles 到 UserInfo 回應**
  - 為什麼：Admin UI 設定 `GetClaimsFromUserInfoEndpoint = true`，OIDC 中介軟體會從 `/connect/userinfo` 取回額外 claim；若 UserInfo 不含 roles，即使 ID Token 有也可能被 ClaimActions 過濾
  - 當 access token 含有 `Scopes.Roles` scope 時，呼叫 `userManager.GetRolesAsync(user)` 並加入 `Claims.Role` 陣列

- [x] **Step 4 - 修正 Admin UI Program.cs：OIDC scope + claim mapping**
  - 為什麼：未請求 `roles` scope → AuthServer 不會包含 roles；未設 `MapInboundClaims = false` + `RoleClaimType = "role"` → `RequireRole("admin")` 永遠找不到對應的 claim
  - 加入 `options.Scope.Add("roles")`
  - 設定 `options.MapInboundClaims = false`
  - 設定 `options.TokenValidationParameters.RoleClaimType = "role"` 與 `NameClaimType = "name"`
  - 加入 `options.ClaimActions.MapJsonArrayKey("role", "role")` 處理 UserInfo 回應的 roles

- [x] **Step 5 - Build 驗證**
  - 為什麼：確保所有修改編譯正確，無語法或型別錯誤
  - `dotnet build OAuth.slnx`
