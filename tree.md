# 專案資料夾結構

```
/mnt/d/lab/oauth/
├── src/
│   ├── AuthServer/
│   │   ├── OAuth.AuthServer.WebAPI/                  # Authorization Server 主程式（ASP.NET Core 10）
│   │   │   ├── Account/
│   │   │   │   ├── AccountApiController.cs           # POST /api/v1/account/register
│   │   │   │   ├── AccountHandler.cs                 # 帳號業務邏輯（Result Pattern）
│   │   │   │   ├── AccountManagementController.cs    # GET /api/v1/account/me 等
│   │   │   │   ├── AccountModels.cs                  # DTO：RegisterRequest / RegisterResponse / Failure
│   │   │   │   └── RegisterRequestValidator.cs       # FluentValidation
│   │   │   ├── Connect/
│   │   │   │   ├── AuthorizationController.cs        # GET/POST /connect/authorize（passthrough）
│   │   │   │   ├── ExternalLoginController.cs        # Social Login Challenge & Callback
│   │   │   │   ├── TokenController.cs                # POST /connect/token
│   │   │   │   └── UserInfoController.cs             # GET /connect/userinfo
│   │   │   ├── Infrastructure/
│   │   │   │   ├── OpenIddictDataSeeder.cs            # Seed：SPA/MVC/WebAPI/Postman/Admin Client
│   │   │   │   └── Threads/
│   │   │   │       ├── ThreadsAuthenticationDefaults.cs
│   │   │   │       ├── ThreadsAuthenticationExtensions.cs
│   │   │   │       ├── ThreadsAuthenticationHandler.cs
│   │   │   │       └── ThreadsAuthenticationOptions.cs
│   │   │   ├── Pages/
│   │   │   │   └── Connect/
│   │   │   │       ├── Authorize.cshtml              # 登入頁面（Razor Page）
│   │   │   │       └── Authorize.cshtml.cs
│   │   │   ├── appsettings.Development.json
│   │   │   └── Program.cs
│   │   └── OAuth.AuthServer.DB/                      # EF Core 資料存取層（PostgreSQL）
│   │       ├── Migrations/                           # EF Core Migrations
│   │       ├── ApplicationDbContext.cs               # IdentityDbContext + OpenIddict
│   │       └── ApplicationUser.cs                    # IdentityUser + DisplayName + AvatarUrl
│   ├── Admin/
│   │   └── OAuth.Admin.WebUI/                        # Blazor Server 管理後台（port 7002）
│   │       ├── Components/
│   │       │   ├── Layout/                           # MainLayout（MudBlazor）+ NavMenu
│   │       │   └── Pages/
│   │       │       ├── Applications/                 # Application 清單、新增、編輯
│   │       │       ├── Scopes/                       # Scope 清單、新增、編輯
│   │       │       ├── Users/                        # User 清單、編輯（鎖定/角色）
│   │       │       └── Roles/                        # Role 清單、新增、刪除
│   │       ├── Services/                             # Admin 服務（直接呼叫 OpenIddict managers）
│   │       │   ├── ApplicationAdminService.cs
│   │       │   ├── ScopeAdminService.cs
│   │       │   ├── UserAdminService.cs
│   │       │   └── RoleAdminService.cs
│   │       ├── ViewModels/                           # 移植自 pixel-identity
│   │       └── Program.cs
│   └── Clients/
│       ├── OAuth.Client.Mvc/                         # MVC 示範客戶端（Cookie SSO，port 5101）
│       │   ├── Controllers/
│       │   │   ├── AccountController.cs              # Login / Logout
│       │   │   └── ProfileController.cs              # /profile（需登入）
│       │   ├── Views/Profile/
│       │   │   └── Index.cshtml
│       │   ├── appsettings.json
│       │   └── Program.cs
│       ├── OAuth.Client.WebAPI/                      # Web API 示範客戶端（Bearer Token，port 5102）
│       │   ├── Controllers/
│       │   │   └── MeController.cs                   # GET /api/v1/me、/api/v1/protected
│       │   ├── appsettings.json
│       │   └── Program.cs
│       └── OAuth.Client.SpaHost/                     # Vue 3 SPA + ASP.NET Core 靜態主機（port 5200）
│           ├── ClientApp/                            # Vue 3 原始碼（oidc-client-ts, Vite）
│           │   ├── src/
│           │   │   ├── auth/oidc.ts                  # OIDC 設定（PKCE, roles scope）
│           │   │   ├── views/                        # Home / Profile / Callback
│           │   │   └── router/                       # Vue Router
│           │   ├── .env.development                  # VITE_REDIRECT_URI=http://localhost:5173/callback
│           │   ├── .env.production                   # VITE_REDIRECT_URI=https://localhost:5200/callback
│           │   ├── vite.config.ts                    # build.outDir=../wwwroot
│           │   └── package.json
│           ├── wwwroot/                              # npm run build 輸出（.gitignore）
│           ├── appsettings.json
│           └── Program.cs
├── test/
│   ├── OAuth.AuthServer.IntegrationTest/             # BDD 整合測試（Reqnroll + Testcontainers）
│   │   ├── _01_Account/
│   │   │   ├── 帳號註冊.feature
│   │   │   └── 帳號註冊Step.cs
│   │   ├── _02_Token/
│   │   │   ├── token交換.feature                    # Discovery / Client Credentials / invalid_grant
│   │   │   └── token交換Step.cs
│   │   ├── _03_Security/
│   │   │   ├── 安全性驗證.feature                   # 401 / PKCE / no implicit flow
│   │   │   └── 安全性驗證Step.cs
│   │   ├── BaseStep.cs                              # 共用 BDD Steps（中文）
│   │   ├── TestAssistant.cs                         # Testcontainers 工具
│   │   └── TestServer.cs                            # WebApplicationFactory
│   └── OAuth.E2E.WebwrightTest/                     # E2E 測試（Webwright SSO 互動驗證）
├── doc/
│   ├── diagrams.md                                  # 核心授權流程與憑證狀態設計文件
│   ├── oauth2-oidc-sequence-diagrams.md             # OAuth2 + OIDC 驗證循序圖（導出檔）
│   └── openapi.yml                                  # OpenAPI 規格（完整）
├── .archive/                                        # 已完成的計畫書封存
│   ├── AdminWorktree.plan.md                        # Git Worktree 與 Admin UI 問題調查計畫（已完成，封存）
│   ├── diagrams.plan.md                             # 循序圖與憑證狀態機設計計畫（已完成，封存）
│   ├── fix-diagram-syntax.plan.md                   # 修正 Mermaid 語法錯誤計畫書（已完成，封存）
│   ├── fix-mermaid-syntax-2.plan.md                 # 修正 Mermaid 語法錯誤計畫書(二)（已完成，封存）
│   ├── oauth-oidc-server.plan.md                    # 實作計畫書（已完成，封存）
│   └── RemoveCoAuthoredBy.plan.md                   # 移除 Co-authored-by 實作計畫書（已完成，封存）
├── .issues/                                         # 問題記錄
│   ├── diagrams.issues.md                           # 建置驗證失敗記錄
│   └── RemoveCoAuthoredBy.issues.md                 # 移除 Co-authored-by 執行問題紀錄
├── docker-compose.yml                               # PostgreSQL 16 + Seq
├── Taskfile.yml                                     # 開發指令集中管理
├── .gitignore
├── OAuth.slnx                                       # Solution（.NET 10 新格式）
└── tree.md                                          # 本檔案

```
