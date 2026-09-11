# OAuth.Client.WebAPI.IntegrationTest 問題紀錄

## Issue #1: JWT Bearer Token 驗證返回 401 而非 200

**狀態：** BLOCKED  
**優先級：** HIGH  
**發現日期：** 2026-06-30

### 問題描述
- 有效的 Bearer Token 發送到 `/api/v1/me` 端點時，返回 **401 Unauthorized** 而非預期的 **200 OK**
- 無 Token 或無效 Token 的測試正確返回 401
- JWT token 生成與簽名邏輯本身正確（JwtDebugTest 通過）

### 測試證據
```
AuthDebugTest.BearerToken_ShouldBeAttachedToRequest: FAIL (預期 200，實際 401)
AuthDebugTest.NoBearerToken_ShouldReturn401: PASS
JwtDebugTest (token generation/validation in isolation): PASS (2/2)
```

### 已嘗試的方法 (失敗)
1. **PostConfigure JwtBearerOptions** — 未執行或執行太晚
   - 原因：中介軟體在 Program.Build() 時已建立，PostConfigure 無法覆蓋
2. **Configure + 移除舊配置** — 仍然 401
   - 原因：可能 DI 容器配置順序或中介軟體已鎖定
3. **設定 Authority=null，禁用 Discovery** — 仍然 401
   - 原因：Program.cs 第 13 行 `options.Authority = jwtConfig["Authority"]` 可能在某個地方被重新套用

### 根本原因假設
**最可能的原因：** ASP.NET Core JwtBearerHandler 在 WebApplicationFactory 環境中：
1. 優先讀取 `appsettings.json` 中的 `Jwt:Authority` 設定（指向不存在的 https://localhost:7001）
2. 嘗試從該 Authority 的 Discovery Endpoint 獲取簽名金鑰
3. 因為 Authority 不存在，驗證失敗，回傳 401
4. WebApiTestFactory.ConfigureServices 中的覆蓋配置沒有被正確套用或被程式碼的後續設定覆蓋

### 待嘗試的方法
1. **環境變數方案**
   - 修改 Program.cs：在測試環境 (`ASPNETCORE_ENVIRONMENT=Test`) 下，使用不同的 JWT 設定邏輯
   - Program.cs 應檢查環境變數或特殊旗標來決定是否從 Authority 讀取或使用本地密鑰
   - 示範：`if (environment.IsEnvironment("Test")) { /* use local RSA */ }`

2. **Mock IConfigurationManager**
   - 在 WebApiTestFactory 中注入一個 Mock `IConfigurationManager<OpenIdConnectConfiguration>`
   - 直接返回包含測試 RSA 公鑰的配置，繞過 Discovery Endpoint

3. **全局中介軟體順序檢查**
   - 確保 UseAuthentication() 在 Program.cs 中的位置是否被正確執行
   - 添加診斷中介軟體記錄認證結果

### 相關檔案
- `src/Clients/OAuth.Client.WebAPI/Program.cs` — 第 10-19 行 JWT 配置
- `test/OAuth.Client.WebAPI.IntegrationTest/WebApiTestFactory.cs` — 配置覆蓋嘗試
- `test/OAuth.Client.WebAPI.IntegrationTest/TestFixture.cs` — Token 生成
- `test/OAuth.Client.WebAPI.IntegrationTest/AuthDebugTest.cs` — 失敗測試案例
- `test/OAuth.Client.WebAPI.IntegrationTest/JwtDebugTest.cs` — 單元測試 (PASS)

### 建議恢復步驟
1. 暫時跳過 Bearer Token 驗證測試，用 `[Fact(Skip = "...")]` 標記
2. 完成其他測試場景結構
3. 在獨立時間重新進行此調查
4. 可考慮 Program.cs 修改以支援測試環境的本地密鑰模式

---

## 尚無其他已知問題
