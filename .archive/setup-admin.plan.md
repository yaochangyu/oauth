# Setup Admin Account 實作計畫

## 目標
建立 yao/Aa123456 管理員帳號並確認 Admin UI 所有功能正常。

## 問題清單

| # | 位置 | 問題 |
|---|---|---|
| 1 | Admin UI `appsettings.Development.json` | DB 名稱錯誤：`oauth` 應為 `oauth_db` |
| 2 | AuthServer `appsettings.Development.json` | 缺少 `Urls` 設定，目前 launchSettings HTTPS 是 7098，但 Admin UI Authority 設為 7001 |
| 3 | AuthServer | 沒有管理員帳號 Seeder |

---

## 步驟

- [x] **Step 1 - 修正 Admin UI DB 連線字串**
  - 為什麼：`appsettings.Development.json` DB 名稱是 `oauth`，但實際 DB 是 `oauth_db`，Admin UI 啟動時會連線失敗
  - 將 `Database=oauth` 改為 `Database=oauth_db`

- [x] **Step 2 - 修正 AuthServer Urls（讓 AuthServer 跑在 7001）**
  - 為什麼：Admin UI 的 `Authority` 設定為 `https://localhost:7001`，但 AuthServer launchSettings 的 HTTPS port 是 7098；需要加入 `Urls` 覆寫讓兩者一致
  - 在 `AuthServer/appsettings.Development.json` 加入 `"Urls": "https://localhost:7001;http://localhost:5265"`

- [x] **Step 3 - 新增 AdminUserSeeder**
  - 為什麼：目前 `OpenIddictDataSeeder` 只建立 OIDC clients，沒有建立任何使用者；Admin UI 需要有 `admin` 角色的帳號才能登入
  - 新增 `AdminUserSeeder.cs`（IHostedService），確保以下項目存在：
    - Role: `admin`
    - User: `yao` / `Aa123456`，指派 `admin` role

- [x] **Step 4 - Build 驗證**
  - 為什麼：確認修改沒有編譯錯誤

- [x] **Step 5 - 啟動基礎設施（Docker）**
  - 為什麼：postgres + seq 需要先跑起來
  - `docker compose up -d`

- [x] **Step 6 - 套用 DB Migration**
  - 為什麼：確保資料表結構是最新的
  - `dotnet ef database update` in AuthServer project

- [x] **Step 7 - 啟動 AuthServer**
  - 背景執行 AuthServer，等待就緒

- [x] **Step 8 - 啟動 Admin UI**
  - 背景執行 Admin UI，等待就緒

- [x] **Step 9 - 驗證 Admin UI 所有功能**
  - 使用瀏覽器自動化確認以下頁面正常：
    - 登入（yao / Aa123456）
    - Applications 列表 & 新增
    - Scopes 列表 & 新增
    - Users 列表
    - Roles 列表
