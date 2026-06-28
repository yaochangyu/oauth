# OAuth2 + OIDC Authorization Server

自架 OAuth2 / OIDC Authorization Server，基於 **ASP.NET Core 10 + OpenIddict 7.5 + PostgreSQL**。

## 功能

| 功能 | 說明 |
|---|---|
| Authorization Code + PKCE | 適用 SPA、行動 App |
| Refresh Token Rotation | Access Token 15 分鐘，Refresh Token 30 天，每次換發自動輪替 |
| Client Credentials | 服務對服務驗證 |
| Social Login | Google、Microsoft、Line、Facebook/Instagram、Threads |
| 本地帳密 | 自有 User 系統，可與 Social Login 綁定 |
| Cookie SSO | MVC 示範客戶端（ASP.NET Core 10 MVC） |
| Bearer Token | Web API 示範客戶端（ASP.NET Core 10 Web API） |

---

## 目錄

- [前置需求](#前置需求)
- [快速開始](#快速開始)
- [環境設定](#環境設定)
  - [Social Login Credentials](#social-login-credentials)
  - [MVC Client Secret](#mvc-client-secret)
- [專案結構](#專案結構)
- [開發伺服器啟動](#開發伺服器啟動)
- [OAuth2 流程說明](#oauth2-流程說明)
- [API 端點](#api-端點)
- [整合測試](#整合測試)
- [Pre-seeded 客戶端](#pre-seeded-客戶端)
- [常見問題](#常見問題)

---

## 前置需求

| 工具 | 版本 |
|---|---|
| .NET SDK | 10.0+ |
| Docker Desktop / Docker Engine | 24+ |
| Task (Taskfile) | v3+ |
| dotnet-ef (EF Core CLI) | 10.0+ |

安裝 Task：

```bash
# macOS
brew install go-task

# Linux
sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -d -b ~/.local/bin

# Windows (scoop)
scoop install task
```

安裝 EF Core CLI：

```bash
dotnet tool install --global dotnet-ef
```

---

## 快速開始

```bash
# 1. 啟動 PostgreSQL 與 Seq（log 收集）
task docker-up

# 2. 建立資料庫並套用 Migrations
task ef-database-update

# 3. 啟動 Authorization Server（https://localhost:7001）
task api-dev
```

打開瀏覽器：

- Authorization Server：`https://localhost:7001`
- Discovery Document：`https://localhost:7001/.well-known/openid-configuration`
- Seq Log UI：`http://localhost:8081`

---

## 環境設定

### Social Login Credentials

Social Login 的 Client ID / Secret 放在 `appsettings.Development.json`（不進 git），於 Authorization Server 專案路徑：

```
src/AuthServer/OAuth.AuthServer.WebAPI/appsettings.Development.json
```

範本：

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "Microsoft": {
      "ClientId": "YOUR_MICROSOFT_CLIENT_ID",
      "ClientSecret": "YOUR_MICROSOFT_CLIENT_SECRET"
    },
    "Facebook": {
      "AppId": "YOUR_FACEBOOK_APP_ID",
      "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
    },
    "Line": {
      "ClientId": "YOUR_LINE_CLIENT_ID",
      "ClientSecret": "YOUR_LINE_CLIENT_SECRET"
    },
    "Threads": {
      "ClientId": "YOUR_THREADS_CLIENT_ID",
      "ClientSecret": "YOUR_THREADS_CLIENT_SECRET"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=oauth;Username=oauth;Password=oauth_pass"
  }
}
```

> 各 Provider 申請位置：
> - Google：[Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Credentials
> - Microsoft：[Azure Portal](https://portal.azure.com/) → App Registrations
> - Facebook/Instagram：[Meta for Developers](https://developers.facebook.com/) → Apps
> - Line：[Line Developers](https://developers.line.biz/) → LINE Login
> - Threads：[Meta for Developers](https://developers.facebook.com/) → Apps → Threads API

### MVC Client Secret

```
src/Clients/OAuth.Client.Mvc/appsettings.Development.json
```

開發環境預設值（與 DataSeeder 一致）：

```json
{
  "OpenIdConnect": {
    "ClientSecret": "mvc-client-secret"
  }
}
```

### PostgreSQL（docker-compose 預設值）

| 參數 | 值 |
|---|---|
| Host | localhost:5432 |
| Database | oauth |
| Username | oauth |
| Password | oauth_pass |

---

## 專案結構

```
/
├── src/
│   ├── AuthServer/
│   │   ├── OAuth.AuthServer.WebAPI/    # Authorization Server 主程式（port 7001）
│   │   └── OAuth.AuthServer.DB/        # EF Core + Migrations
│   └── Clients/
│       ├── OAuth.Client.Mvc/           # MVC 示範（Cookie SSO，port 5101）
│       └── OAuth.Client.WebAPI/        # API 示範（Bearer Token，port 5102）
├── test/
│   ├── OAuth.AuthServer.IntegrationTest/  # BDD 整合測試（Reqnroll + Testcontainers）
│   └── OAuth.E2E.WebwrightTest/           # E2E 測試（Webwright）
├── doc/
│   └── openapi.yml                     # OpenAPI 3.1 規格
├── docker-compose.yml                  # PostgreSQL 16 + Seq
└── Taskfile.yml                        # 開發指令集中管理
```

---

## 開發伺服器啟動

所有常用指令透過 `task` 執行：

```bash
# 查看所有可用指令
task --list

# 啟動 Docker（PostgreSQL + Seq）
task docker-up

# 停止 Docker
task docker-down

# 新增 EF Migration
task ef-migration-add -- MigrationName

# 套用 Migration 至資料庫
task ef-database-update

# 啟動 Authorization Server（https://localhost:7001）
task api-dev

# 啟動 MVC 示範客戶端（https://localhost:5101）
task client-mvc-dev

# 啟動 Web API 示範客戶端（https://localhost:5102）
task client-api-dev

# 建置全部專案
task build

# 執行整合測試（需要 Docker 啟動）
task test-integration
```

### 完整開啟三個服務

需開三個終端機：

```bash
# Terminal 1
task api-dev

# Terminal 2
task client-mvc-dev

# Terminal 3
task client-api-dev
```

---

## OAuth2 流程說明

### Authorization Code + PKCE（SPA / 行動 App）

```
1. 產生 code_verifier（隨機字串）與 code_challenge（SHA256 hash）
2. 導向 Authorization Endpoint：
   GET https://localhost:7001/connect/authorize
     ?response_type=code
     &client_id=spa-client
     &redirect_uri=https://your-app/callback
     &scope=openid profile email offline_access api
     &code_challenge=<S256 hash>
     &code_challenge_method=S256

3. 使用者登入、同意後，收到 authorization code

4. 用 code 換 token：
   POST https://localhost:7001/connect/token
   Content-Type: application/x-www-form-urlencoded

   grant_type=authorization_code
   &code=<authorization_code>
   &redirect_uri=https://your-app/callback
   &client_id=spa-client
   &code_verifier=<原始 code_verifier>
```

### Client Credentials（服務對服務）

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=webapi-client&client_secret=webapi-client-secret&scope=api"
```

### Refresh Token 換發

```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token&refresh_token=<token>&client_id=spa-client"
```

> 注意：每次換發都會回傳**新的** refresh token，舊的立即失效（Rotation）。

### MVC 客戶端 SSO 流程

1. 打開 `https://localhost:5101`
2. 點「Login」→ 自動導向 Authorization Server 登入頁
3. 輸入帳密或使用 Social Login
4. 成功後自動回到 MVC 客戶端，顯示使用者 Profile

### Social Login 流程

1. 從 Authorization Server 登入頁點擊 Social Provider 按鈕
2. 導向對應 Provider 的 OAuth 授權頁
3. 授權後回到 `/connect/authorize/external/callback`
4. 若此 Social 帳號首次登入，自動建立本地帳號並綁定
5. 完成 OIDC 流程，回傳 code 給原始 Client

---

## API 端點

完整規格見 `doc/openapi.yml`。

### Authorization Server

| 方法 | 路徑 | 說明 |
|---|---|---|
| `GET/POST` | `/connect/authorize` | Authorization Endpoint |
| `POST` | `/connect/token` | Token Endpoint |
| `GET` | `/connect/userinfo` | UserInfo Endpoint（需 Bearer Token） |
| `GET` | `/connect/endsession` | Logout Endpoint |
| `GET` | `/.well-known/openid-configuration` | Discovery Document |

### Account API

| 方法 | 路徑 | 說明 | 驗證 |
|---|---|---|---|
| `POST` | `/api/v1/account/register` | 本地帳密註冊 | 無 |
| `GET` | `/api/v1/account/me` | 取得目前使用者 | Bearer |
| `GET` | `/api/v1/account/external-logins` | 已綁定的 Social Provider | Bearer |
| `DELETE` | `/api/v1/account/external-logins/{provider}` | 解除 Social 綁定 | Bearer |
| `PUT` | `/api/v1/account/password` | 修改密碼 | Bearer |

---

## 整合測試

整合測試使用 **Testcontainers** 自動啟動 PostgreSQL（無需手動啟動 Docker）：

```bash
# 確保 Docker 在執行中
docker info

# 執行整合測試
task test-integration
```

測試情境（BDD Gherkin）：

- `_01_Account/帳號註冊.feature`：註冊成功、重複 Email 409、密碼格式 400
- `_02_Token/token交換.feature`：Discovery Document、Client Credentials、invalid_grant
- `_03_Security/安全性驗證.feature`：未帶 Bearer 401、無效 Token 401、PKCE 確認、拒絕 Implicit Flow

---

## Pre-seeded 客戶端

授權伺服器啟動時，`OpenIddictDataSeeder` 會自動建立以下 OpenIddict 客戶端：

| Client ID | 類型 | 用途 | Port |
|---|---|---|---|
| `spa-client` | Public（PKCE） | SPA / Postman 測試 | — |
| `mvc-client` | Confidential（PKCE） | MVC Cookie SSO | 5101 |
| `webapi-client` | Confidential | Client Credentials | — |
| `postman-client` | Public（PKCE） | Postman 手動測試 | — |

> `spa-client` 與 `postman-client` 為 Public Client，不需要 client_secret，使用 PKCE 代替。

---

## 常見問題

### HTTPS 開發憑證信任問題

```bash
dotnet dev-certs https --trust
```

若在 WSL 中無效，於 Windows 側執行上述指令。

### 執行測試時出現 Docker 相關錯誤

確認 Docker 正在執行，且目前使用者有權限存取 Docker socket：

```bash
docker info
# 若在 Linux，確認使用者在 docker 群組
sudo usermod -aG docker $USER
```

### Social Login Callback URL 設定

各 Provider 需在後台設定允許的 Redirect URI：

| Provider | Callback URL |
|---|---|
| Google | `https://localhost:7001/signin-google` |
| Microsoft | `https://localhost:7001/signin-microsoft` |
| Facebook | `https://localhost:7001/signin-facebook` |
| Line | `https://localhost:7001/signin-line` |
| Threads | `https://localhost:7001/signin-threads` |

### 更換資料庫 Connection String

修改 `src/AuthServer/OAuth.AuthServer.WebAPI/appsettings.Development.json` 的 `ConnectionStrings:DefaultConnection`，套用後執行：

```bash
task ef-database-update
```
