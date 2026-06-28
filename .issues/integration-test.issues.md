# Integration Test 問題紀錄

## 問題 1：relation "OpenIddictApplications" does not exist（migration 競爭）

**症狀**：`[BeforeTestRun]` 執行 `Factory.InitializeDatabaseAsync()` 後，所有測試皆報 `42P01: relation "OpenIddictApplications" does not exist`。

**失敗原因**：
`WebApplicationFactory.Services` 第一次被存取時會觸發 host START（包括 hosted services）。
`OpenIddictDataSeeder` 作為 `IHostedService` 立即嘗試查詢 `OpenIddictApplications` 資料表，
但此時 `MigrateAsync()` 尚未執行，資料表不存在。

**已失敗方法**：
- `using var scope = Services.CreateScope(); await dbContext.Database.MigrateAsync();`（hosted services 先於 migration 啟動）

**正確作法**：
`InitializeDatabaseAsync()` 直接以 connection string 建立 `DbContext`（不經 factory 的 `Services`），
避免觸發 host start，確保 migration 在 seeder 前執行。

---

## 問題 2：OpenIddict 拒絕 HTTP 請求（ID2083）

**症狀**：修正 migration 後，token endpoint / discovery endpoint 測試返回 `"This server only accepts HTTPS requests."` (ID2083)。

**失敗原因**：`WebApplicationFactory` 的 `TestServer` 使用 HTTP 協議，OpenIddict 預設強制 HTTPS。

**已失敗方法**：
- 環境設為 "Testing"（沒有 dev certs 且沒有 ephemeral keys）
- `DisableTransportSecurityRequirement()` → 可解決 HTTPS 問題，但 discovery document URL 生成為 `http://` 而非 `https://`

**正確作法**：
1. 環境設為 "Development"（OpenIddict 會自動加 dev encryption/signing cert）
2. 測試客戶端設 `BaseAddress = new Uri("https://localhost/")`（TestServer 支援 HTTPS scheme，不做真正 TLS）

---

## 問題 3：invalid_client 預期 HTTP 400 但得到 401

**症狀**：`使用無效 client_id 換 token` 測試預期 HTTP 400，實際得到 401。

**原因**：RFC 6749 §5.2 允許 `invalid_client` 回傳 HTTP 401，OpenIddict 新版（7.x）遵循此規範改為 401。

**修正**：更新 feature 測試預期為 `401`。
