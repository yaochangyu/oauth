# SPA 專案合併計畫

## 目標
將 `OAuth.Client.Spa`（Vue 原始碼）與 `OAuth.Client.SpaHost`（ASP.NET Core 靜態主機）合併為一個專案，留下 Vue 範例，並建立 E2E 測試。

## 合併後結構
```
OAuth.Client.SpaHost/
  ClientApp/              ← Vue 原始碼（從 OAuth.Client.Spa 移入）
    src/
      auth/oidc.ts
      views/
      router/
      App.vue / main.ts
    .env.development       ← dev redirect_uri
    .env.production        ← prod redirect_uri
    index.html
    package.json
    vite.config.ts
    tsconfig*.json
  wwwroot/                ← npm run build 的輸出（.gitignore）
  Program.cs
  appsettings.json
  OAuth.Client.SpaHost.csproj

test/OAuth.E2E.WebwrightTest/
  SpaHostFixture.cs       ← 新增：Vue SPA OIDC 登入 Fixture
  SpaHostTests.cs         ← 新增：Vue SPA E2E 測試
```

## 開發 vs 生產
- **開發**：`cd ClientApp && npm run dev` → Vue SPA 跑在 `http://localhost:5173`
- **生產**：`npm run build`（輸出到 `wwwroot/`）→ `dotnet run` 在 `https://localhost:5200` 服務
- **測試**：針對已啟動的 SpaHost（`https://localhost:5200`）執行 Playwright 測試

## 實作步驟

- [x] **步驟 1：移動 Vue 原始碼**
  - 在 `OAuth.Client.SpaHost/` 底下建立 `ClientApp/` 資料夾
  - 將 `OAuth.Client.Spa/src/`、`index.html`、`package.json`、`vite.config.ts`、`tsconfig*.json`、`public/` 移入
  - 理由：保留 Vue 完整原始碼

- [x] **步驟 2：修正 `oidc.ts` 的設定**
  - `AUTHORITY` 改為 `https://localhost:7001`
  - 加入 `roles` scope
  - `REDIRECT_URI` / `POST_LOGOUT_URI` 改用 `import.meta.env.VITE_REDIRECT_URI`
  - 建立 `.env.development`（port 5173）與 `.env.production`（port 5200）

- [x] **步驟 3：調整 `vite.config.ts` 生產 build 輸出路徑**
  - 設定 `build.outDir = '../wwwroot'`、`build.emptyOutDir = true`

- [x] **步驟 4：刪除 SpaHost 的純 HTML 示範**
  - 刪除 `wwwroot/index.html`

- [x] **步驟 5：補充 `.gitignore`**
  - `node_modules/`、`wwwroot/` 排除

- [x] **步驟 6：更新 AuthServer seeder 的 spa-client redirect_uri**
  - 加入 `https://localhost:5200/callback`（保留 `http://localhost:5173/callback`）

- [x] **步驟 7：npm install 並 build**
  - `cd ClientApp && npm install && npm run build`
  - 驗證 `wwwroot/` 有 Vue 打包產出

- [x] **步驟 8：新增 `SpaHostFixture.cs`（OIDC 登入 Fixture）**
  - 導覽到 SpaHost → 觸發 OIDC → 在 AuthServer 登入 → 等待 callback 完成

- [x] **步驟 9：新增 `SpaHostTests.cs`（E2E 測試）**
  - 5 項測試：登入後不發生 OIDC 迴圈、首頁已登入狀態、顯示 email、Access Token 有效、role claim

- [x] **步驟 10：執行所有 E2E 測試**
  - 19/19 Passed（Admin UI 10 + MVC 4 + SpaHost 5）

- [x] **步驟 11：刪除 `OAuth.Client.Spa` 目錄**
  - 移除已合併完畢的舊 Vue 專案目錄

- [x] **步驟 12：更新 Taskfile 與 tree.md**
  - `client-spa-dev` / `client-spa-host-dev` → `spa-dev` / `spa-build` / `spa-host-dev`
  - tree.md 新增 `OAuth.Client.SpaHost/ClientApp/` 結構，移除舊 `OAuth.Client.Spa`
