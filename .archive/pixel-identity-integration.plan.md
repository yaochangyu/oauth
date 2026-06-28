# pixel-identity 整合計畫

## 目標

將 [pixel-identity](https://github.com/Nfactor26/pixel-identity) 的 Blazor 管理後台 UI 整合至現有 OAuth2 + OIDC 專案，
提供 User、Role、OpenIddict Application（Client）、Scope 的 Web 管理介面。

## 整合策略

**新增 `OAuth.Admin.WebUI` 專案**（獨立 ASP.NET Core 10 Blazor 應用），
移植 pixel-identity 的管理 UI，升級至 .NET 10 + OpenIddict 7.5.0，
共用現有 PostgreSQL 資料庫（共用 ASP.NET Identity + OpenIddict tables）。

## 架構圖

```
┌─────────────────────────────────────────────────────┐
│                   PostgreSQL                        │
│  (ASP.NET Identity + OpenIddict tables)             │
└────────┬────────────────────┬───────────────────────┘
         │                    │
┌────────▼─────────┐  ┌───────▼──────────────────────┐
│ OAuth.AuthServer  │  │ OAuth.Admin.WebUI             │
│ (port 7001)       │  │ (port 7002)                   │
│ OAuth2/OIDC 流程  │  │ Blazor 管理後台               │
│ Token 簽發        │  │ 管理 User/Role/App/Scope      │
│ Social Login      │  │ (需登入，僅管理員可用)         │
└───────────────────┘  └───────────────────────────────┘
```

## 步驟

- [x] **Step 1 - Clone pixel-identity 原始碼並分析 API 差異**
  - 為什麼：需要了解 OpenIddict 5.x → 7.x 的 API 變更，以便正確移植
  - 將 pixel-identity 的 Core、UI.Client、Store.Sql.Shared、Store.PostgreSQL 複製到本地進行分析

- [x] **Step 2 - 建立 `OAuth.Admin.WebUI` 專案**
  - 為什麼：需要一個新的 ASP.NET Core 10 Blazor Server 專案作為管理後台
  - 加入 Solution（`OAuth.slnx`）
  - 加入 MudBlazor、OpenIddict.AspNetCore 相依

- [x] **Step 3 - 建立共用 DB 存取層（`OAuth.Admin.DB`）**
  - 為什麼：管理後台需要存取與 Authorization Server 相同的資料庫，但需要分離關注點
  - 移植 pixel-identity 的 `ApplicationUser`、`ApplicationRole`、User/Role Store
  - 評估是否可直接共用 `OAuth.AuthServer.DB` 專案

- [x] **Step 4 - 移植 Users 管理 UI**
  - 為什麼：這是 pixel-identity 最核心的功能之一
  - 移植 Blazor 元件：User 清單、新增、編輯、角色指派、刪除
  - 升級 API 至 OpenIddict 7.x 相容

- [x] **Step 5 - 移植 Roles 管理 UI**
  - 為什麼：角色管理是 User 管理的基礎
  - 移植 Blazor 元件：Role 清單、新增、編輯、權限指派

- [x] **Step 6 - 移植 Applications（OpenIddict Clients）管理 UI**
  - 為什麼：能透過 UI 動態新增/編輯 OAuth2 Client，不需要重啟 AuthServer
  - 移植 Blazor 元件：Application 清單、新增（含 Grant Types、Redirect URIs）、編輯、刪除
  - 升級至 OpenIddict 7.x `IOpenIddictApplicationManager` API

- [x] **Step 7 - 移植 Scopes 管理 UI**
  - 為什麼：動態管理 OAuth2 Scope，不需重啟 AuthServer
  - 移植 Blazor 元件：Scope 清單、新增、編輯、刪除

- [x] **Step 8 - 設定認證（只有管理員可登入後台）**
  - 為什麼：管理後台不能開放給一般使用者
  - 使用 OpenIdConnect 對接 AuthServer（port 7001）
  - 設定 Policy：需 `admin` Role

- [x] **Step 9 - 更新 docker-compose 與 Taskfile**
  - 為什麼：新服務需要加入開發環境
  - docker-compose 加入 `oauth-admin` 服務（選用）
  - Taskfile 加入 `admin-dev` task

- [x] **Step 10 - Build 驗證 + 更新文件**
  - 為什麼：確保整合後所有專案正常編譯
  - `dotnet build OAuth.slnx`
  - 更新 `tree.md`、`README.md`

## 版本對應

| 項目 | pixel-identity | 本專案 |
|---|---|---|
| .NET | 8.0 | 10.0 |
| OpenIddict | 5.7.0 | 7.5.0 |
| Identity | 8.x | 10.x |
| MudBlazor | 7.0.0 | 最新 |
| 資料庫 | PostgreSQL 14 | PostgreSQL 16 |

## 架構決策（更新）

| 項目 | 決策 | 理由 |
|---|---|---|
| PK 類型 | 維持 `string`，不改 Schema | 避免 Migration 風險；controllers 用泛型 `TUser, TKey` |
| UI 類型 | Blazor Server（非 WASM） | 避免 WASM 額外設定，Admin 後台不需要離線能力 |
| 資料庫存取 | 共用 `OAuth.AuthServer.DB` 的 DbContext | 不重複定義 EF Core 實體 |
| OpenIddict API | `IOpenIddictApplicationManager` / `IOpenIddictScopeManager` 抽象層，5→7 基本相容 | UI.Client 不直接依賴 OpenIddict |

## 風險

- OpenIddict 5.x → 7.x：`IOpenIddictApplicationManager.FindByClientIdAsync` 等方法簽名基本不變，但若有破壞性變更需逐一修正
- Blazor WASM → Server 轉換：pixel-identity 的元件可能有 WASM 特有的 AuthenticationStateProvider 依賴需更換
