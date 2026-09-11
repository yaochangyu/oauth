# OAuth.Client.WebAPI.IntegrationTest Issues

## Issue 1: TestFixture 未啟動 WebAPI 伺服器
**症狀**: 測試執行時 `connection refused (localhost:5102)`
**根本原因**: TestFixture.cs 只啟動 PostgreSQL 容器，未啟動 WebAPI 伺服器進程
**已失敗的方法**: 
- 嘗試用簡單的 HttpClient BaseAddress 指向本機 URL

**正確做法（參考 OAuth.AuthServer.IntegrationTest）**:
- 使用 `WebApplicationFactory<Program>` 啟動伺服器
- 在 ConfigureWebHost 中注入測試用 connection string
- 伺服器在測試期間保持運行

**計畫修復**:
- 建立 `WebApiTestFactory : WebApplicationFactory<Program>`
- 在 TestFixture 中使用 factory 的 HttpClient
- 修改 BaseStep 以使用 factory client

## Issue 2: Reqnroll 步驟綁定文化設定
**症狀**: 原始嘗試用 `# language: en` + 英文 binding 不匹配
**根本原因**: Feature 檔用繁體中文 step，但 binding 用英文正則導致無法匹配
**已失敗的方法**:
- 用英文正則 pattern 在 `[Given]` 等屬性

**正確做法**:
- specflow.json 設定 `bindingCulture: "zh-TW"`
- Feature 檔不用 `# language:` 指令（預設英文 Gherkin）
- Binding 屬性使用繁體中文正則

**現狀**: 已應用上述修正，需驗證

