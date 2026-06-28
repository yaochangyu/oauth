# OAuth OIDC 專案開發指南

## 服務 Port 對照表

| 服務 | HTTPS Port | HTTP Port | 設定來源 |
|---|---|---|---|
| AuthServer | 7001 | 5265 | `launchSettings.json` → https profile |
| Admin UI | 7002 | 5279 | `appsettings.json` → Urls |
| MVC Client | 5101 | 5256 | `appsettings.json` → Urls |
| WebAPI Client | 5102 | 5160 | `appsettings.json` → Urls |
| SPA Client | 5173 | - | Vite dev server |
| PostgreSQL | 5432 | - | Docker Compose |
| Seq (Log UI) | 8081 | 5341 | Docker Compose |

## 啟動指令

```bash
# 1. 基礎設施（必須先啟動）
task docker-up

# 2. 套用 DB Migration（第一次或有新 migration 時）
task ef-database-update

# 3. 啟動各服務（各自開 terminal）
task authserver-dev           # AuthServer      https://localhost:7001
task authserver-admin-ui-dev  # Admin UI        https://localhost:7002
task client-mvc-dev       # MVC Client      https://localhost:5101
task client-api-dev       # WebAPI Client   https://localhost:5102
task client-spa-dev       # SPA (Vue)       http://localhost:5173
task client-spa-host-dev  # SPA Host (HTML) https://localhost:5200
```

## OIDC Client 管理

**DB 是唯一 source of truth。** Client 的 redirect URI、permissions 等設定存在 `OpenIddictApplications` 資料表，透過 Admin UI（Applications 頁面）管理。

`OpenIddictDataSeeder` 只在 DB **沒有**對應 client 時執行初始化（create if not exists），之後的任何修改請走 Admin UI，Seeder 不會覆蓋已存在的資料。

### 若變更 AuthServer Port（目前 7001）

| 要更新的位置 | 設定 |
|---|---|
| `src/AuthServer/OAuth.AuthServer.WebAPI/Properties/launchSettings.json` | `profiles.https.applicationUrl` |
| `src/Admin/OAuth.AuthServer.Admin.WebUI/appsettings.json` | `OpenIdConnect.Authority` |
| `src/Clients/OAuth.Client.Mvc/appsettings.json` | `OpenIdConnect.Authority` |
| `src/Clients/OAuth.Client.WebAPI/appsettings.json` | `Jwt.Authority` |

> Redirect URI 不含 AuthServer 自己的 port，無需更新 DB。

### 若變更 Admin UI / Client 的 Port

直接透過 **Admin UI → Applications** 修改對應 client 的 redirect URI。無需改 code。

僅 `launchSettings.json` 和 `appsettings.json:Urls` 需對齊新 port。

## 管理員帳號

- **帳號**：yao
- **密碼**：Aa123456
- **角色**：admin
- **建立方式**：`AdminUserSeeder`（AuthServer 啟動時自動 seed，已存在則跳過）

## 密碼政策

- RequireDigit: true
- RequireLowercase: true
- RequiredLength: 8
- RequireNonAlphanumeric: false
