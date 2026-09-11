# Graph Report - oauth  (2026-09-11)

## Corpus Check
- Corpus is ~38,120 words - fits in a single context window. You may not need a graph.

## Summary
- 1453 nodes · 1864 edges · 109 communities (85 shown, 12 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 76 edges (avg confidence: 0.87)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Playwright UI Testing
- Architecture & Specs
- Integration Test Suite
- Admin API Contracts
- MVC Client Sample
- AuthServer Legacy UI
- Playwright UI Testing
- Admin API Contracts
- AuthServer Legacy UI
- OpenIddict Applications
- Admin Application UI
- Admin API Contracts
- Admin API Contracts
- Admin User UI
- Admin User UI
- Admin API Contracts
- Admin User UI
- Admin Application UI
- Admin Scope UI
- Identity & Users
- Playwright UI Testing
- OpenIddict Applications
- Admin Scope UI
- OpenIddict Scopes
- MVC Client Sample
- Admin MudBlazor UI
- Identity & Users
- Admin API Contracts
- Admin API Contracts
- Tsconfig.Node.Json Compileroptions
- Applicationurl Commandname
- Identity & Users
- Applicationurl Commandname
- Applicationurl Commandname
- Applicationurl Commandname
- Applicationurl Commandname
- Identity & Users
- OAuth Token Service
- OpenIddict Applications
- Admin MudBlazor UI
- Playwright UI Testing
- Identity & Users
- Playwright UI Testing
- Playwright UI Testing
- (10.0.9) (7.5.0)
- Integration Test Suite
- Integration Test Suite
- Router Vue-Router
- MVC Client Sample
- MVC Client Sample
- AuthServer Legacy UI
- Identity & Users
- Admin API Contracts
- OAuth Client Integrations
- @Types/Node Typescript
- Identity & Users
- OAuth Client Integrations
- OAuth Token Service
- @Vue/Tsconfig/Tsconfig.Dom.Json Tsconfig.App.Json
- Identity & Users
- Admin MudBlazor UI
- Playwright UI Testing
- MVC Client Sample
- MVC Client Sample
- Playwright UI Testing
- OAuth Token Service
- Admin MudBlazor UI
- MVC Client Sample
- Admin MudBlazor UI
- MVC Client Sample
- OAuth Client Integrations
- Admin MudBlazor UI
- Identity & Users
- Devdependencies @Types/Node
- OAuth Client Integrations
- Admin MudBlazor UI
- OAuth Client Integrations
- OAuth Client Integrations
- Integration Test Suite
- Admin MudBlazor UI
- Admin API Contracts
- OAuth Client Integrations
- Scripts Build
- Worktree Admin
- Identity & Users
- OAuth Client Integrations
- AuthServer Legacy UI
- Admin MudBlazor UI
- Admin MudBlazor UI
- Admin MudBlazor UI
- Jquery License
- Tsconfig.Json Files
- Build Verification
- Filter-Branch Unstaged
- AuthServer Legacy UI
- Oauth.Authserver.Webapi.Pages.Connect.Authorizemodel Authorize.Cshtml
- OAuth Client Integrations

## God Nodes (most connected - your core abstractions)
1. `BaseStep` - 42 edges
2. `AdminUI管理介面Step` - 36 edges
3. `PlaywrightBaseStep` - 24 edges
4. `ApplicationViewModel` - 23 edges
5. `UserDetailsViewModel` - 20 edges
6. `OAuth.AuthServer.DB` - 19 edges
7. `ApplicationUser` - 17 edges
8. `OAuth.AuthServer.Admin.WebUI.ViewModels` - 16 edges
9. `ApplicationDbContext` - 16 edges
10. `同意頁面Step` - 16 edges

## Surprising Connections (you probably didn't know these)
- `RFC 9700 PKCE Enforcement Across All Clients` --semantically_similar_to--> `Authorization Code Flow with PKCE`  [INFERRED] [semantically similar]
  phase3_developer_portal.plan.md → README.md
- `Headless AuthServer Architecture Rationale` --rationale_for--> `Phase 1: AuthServer Headless Migration Plan`  [INFERRED]
  oauth_full_architecture_master_plan.plan.md → phase1_authserver_headless_migration.plan.md
- `Explicit vs Implicit Consent Authorization Flow` --conceptually_related_to--> `Consent Session Race Condition and IMemoryCache Token Fix`  [INFERRED]
  .archive/consent-page.plan.md → .issues/consent-e2e.issues.md
- `Mermaid Sequence Diagram Alias Quoting Rule` --conceptually_related_to--> `Mermaid Sequence Diagram Special Character Syntax Bug`  [INFERRED]
  .archive/fix-diagram-syntax.plan.md → .issues/diagram-syntax-error.issues.md
- `State Diagram Note and Transition Quoting Standards` --conceptually_related_to--> `Mermaid Database Alias Order and End Note Standards`  [INFERRED]
  .archive/fix-mermaid-syntax-2.plan.md → .issues/mermaid-syntax-fix.issues.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Admin UI Authorization and Role Claim Propagation Pipeline** — _archive_admin_ui_fix_plan_role_claim_token_propagation, _archive_setup_admin_plan_admin_seeding_configuration, _archive_client_oidc_verification_plan_centralized_admin_rbac, _archive_admin_ui_e2e_plan_admin_user_seeder [INFERRED 0.85]
- **OAuth Consent Page Flow and E2E Verification** — _archive_consent_page_plan_explicit_consent_workflow, _issues_consent_e2e_issues_consent_session_race_memory_cache, _archive_clients_e2e_plan_reqnroll_playwright_client_bdd [INFERRED 0.85]
- **Mermaid Diagram Syntax Standards and Token State Machine Specification** — _archive_diagrams_plan_token_lifecycle_state_machine, _archive_fix_diagram_syntax_plan_mermaid_alias_double_quoting, _archive_fix_mermaid_syntax_2_plan_state_diagram_syntax_standard, _issues_mermaid_syntax_fix_issues_mermaid_database_alias_order_and_end_note [INFERRED 0.85]
- **OAuth Platform Four-Phase Modernization Plan** — oauth_full_architecture_master_plan_plan_masterplan, phase1_authserver_headless_migration_plan_phase1, phase2_account_portal_plan_phase2, phase3_developer_portal_plan_phase3, phase4_admin_portal_plan_phase4 [EXTRACTED 1.00]
- **OAuth Security Hardening Architecture** — phase1_authserver_headless_migration_plan_open_redirect_defense, phase1_authserver_headless_migration_plan_login_rate_limiting, phase2_account_portal_plan_native_openiddict_authorizations, phase3_developer_portal_plan_dual_secret_rotation, phase3_developer_portal_plan_rfc9700_pkce_enforcement [INFERRED 0.85]
- **MVC Client-side Validation Libraries Stack** — src_clients_oauth_client_mvc_wwwroot_lib_jquery_license_mit, src_clients_oauth_client_mvc_wwwroot_lib_jquery_validation_license_mit, src_clients_oauth_client_mvc_wwwroot_lib_jquery_validation_unobtrusive_license_mit [EXTRACTED 1.00]
- **SPA Client Social Icons Set** — src_clients_oauth_client_spahost_clientapp_public_icons_github_icon, src_clients_oauth_client_spahost_clientapp_public_icons_discord_icon, src_clients_oauth_client_spahost_clientapp_public_icons_x_icon, src_clients_oauth_client_spahost_clientapp_public_icons_bluesky_icon [EXTRACTED 1.00]
- **SPA Client Branding and Graphic Assets** — src_clients_oauth_client_spahost_clientapp_src_assets_hero_illustration, src_clients_oauth_client_spahost_clientapp_src_assets_vite_logo, src_clients_oauth_client_spahost_clientapp_src_assets_vue_logo [INFERRED 0.85]

## Communities (109 total, 12 thin omitted)

### Community 0 - "Playwright UI Testing"
Cohesion: 0.08
Nodes (17): OAuth.Clients.PlaywrightTest, Given, IPage, ScenarioContext, Task, Then, When, AdminUI管理介面Step (+9 more)

### Community 1 - "Architecture & Specs"
Cohesion: 0.04
Nodes (49): OAuth OIDC Development Guide, Identity Password Policy Requirements, Development Service Port Allocations, TDD & BDD Workflow Guidelines, Environment Certificate Lifecycle & Management, OAuth2 OIDC Authorization Flow and Certificate Specification, OIDC Token State Machine Specifications, OAuth2 OIDC Sequence and State Diagrams (+41 more)

### Community 2 - "Integration Test Suite"
Cohesion: 0.05
Nodes (28): OAuth.AuthServer.IntegrationTest, OAuth.AuthServer.IntegrationTest._01_Account, OAuth.AuthServer.IntegrationTest._02_Token, OAuth.AuthServer.IntegrationTest._03_Security, Dictionary, PostgreSqlContainer, Steps, Table (+20 more)

### Community 3 - "Admin API Contracts"
Cohesion: 0.09
Nodes (8): HttpResponseMessage, AfterScenario, Given, ScenarioContext, Task, Then, When, BaseStep

### Community 4 - "MVC Client Sample"
Cohesion: 0.07
Nodes (27): AuthenticationProperties, Controller, OAuth.Client.Mvc.Controllers, OAuth.Client.Mvc.Models, ResponseCache, HttpGet, HttpPost, IActionResult (+19 more)

### Community 5 - "AuthServer Legacy UI"
Cohesion: 0.06
Nodes (29): OAuth.AuthServer.WebAPI.Pages.Account, IReadOnlyList, PageModel, CancellationToken, IActionResult, SignInManager, Task, UserManager (+21 more)

### Community 6 - "Playwright UI Testing"
Cohesion: 0.10
Nodes (18): Process, OnInitialized, PageTitle, System.Diagnostics, AfterScenario, AfterTestRun, BeforeScenario, BeforeTestRun (+10 more)

### Community 7 - "Admin API Contracts"
Cohesion: 0.07
Nodes (22): Action, AuthenticateResult, AuthenticationBuilder, AuthenticationHandler, AuthenticationSchemeOptions, AuthenticationTicket, ClaimsIdentity, OAuth.AuthServer.WebAPI.Infrastructure.Threads (+14 more)

### Community 8 - "AuthServer Legacy UI"
Cohesion: 0.09
Nodes (24): OpenIddictApplicationDescriptor, ApplicationViewModel, IEnumerable, IOpenIddictApplicationManager, Items, Task, Total, ApplicationAdminService (+16 more)

### Community 9 - "OpenIddict Applications"
Cohesion: 0.08
Nodes (18): OAuth.AuthServer.DB.Migrations, Migration, ModelSnapshot, DateTimeOffset, MigrationBuilder, DateTimeOffset, ModelBuilder, InitialCreate (+10 more)

### Community 10 - "Admin Application UI"
Cohesion: 0.07
Nodes (28): MudDivider, MudSelect, MudSelectItem, route:/applications/edit/{ClientId}, route:/applications/new, AddPostLogoutUri, AddRedirectUri, OnInitializedAsync (+20 more)

### Community 11 - "Admin API Contracts"
Cohesion: 0.09
Nodes (24): OAuth.AuthServer.Admin.WebUI.Responses, IDictionary, List, PagedList, CurrentPage, Items, ItemsCount, PageCount (+16 more)

### Community 12 - "Admin API Contracts"
Cohesion: 0.11
Nodes (19): AbstractValidator, OAuth.AuthServer.WebAPI.Account, IValidator, ProducesResponseType, ProducesResponseType&lt;RegisterResponse&gt;, Result, CancellationToken, HttpPost (+11 more)

### Community 13 - "Admin User UI"
Cohesion: 0.08
Nodes (24): route:/roles, CreateAsync, DeleteAsync, LoadDataAsync, OnSearch, HeaderContent, IDialogService, ISnackbar (+16 more)

### Community 14 - "Admin User UI"
Cohesion: 0.08
Nodes (23): route:/users, DeleteAsync, LoadDataAsync, OnSearch, HeaderContent, IDialogService, ISnackbar, MudIcon (+15 more)

### Community 15 - "Admin API Contracts"
Cohesion: 0.10
Nodes (17): OAuth.AuthServer.Admin.WebUI.Requests, GetApplicationsRequest, ApplicationFilter, GetRolesRequest, RoleFilter, GetScopesRequest, ScopesFilter, GetUsersRequest (+9 more)

### Community 16 - "Admin User UI"
Cohesion: 0.09
Nodes (22): MudAlert, MudCardHeader, route:/users/edit/{Id}, OnInitializedAsync, ISnackbar, MudButton, MudCard, MudCardActions (+14 more)

### Community 17 - "Admin Application UI"
Cohesion: 0.09
Nodes (22): route:/applications, DeleteAsync, LoadDataAsync, OnSearch, ApplicationAdminService, ApplicationViewModel, HeaderContent, IDialogService (+14 more)

### Community 18 - "Admin Scope UI"
Cohesion: 0.09
Nodes (22): route:/scopes, DeleteAsync, LoadDataAsync, OnSearch, HeaderContent, IDialogService, ISnackbar, MudIconButton (+14 more)

### Community 19 - "Identity & Users"
Cohesion: 0.11
Nodes (18): ControllerBase, IdentityUser, ApplicationUser, AvatarUrl, DisplayName, CancellationToken, HttpGet, IActionResult (+10 more)

### Community 20 - "Playwright UI Testing"
Cohesion: 0.12
Nodes (21): Admin UI E2E Test Plan, Admin User Seeder for Testing, Playwright Admin UI Test Suite, Admin UI Fix Plan, OIDC Redirect Loop Resolution, Role Claim Token Propagation Pipeline, Centralized Admin RBAC via AuthServer, Client OIDC Verification Plan (+13 more)

### Community 21 - "OpenIddict Applications"
Cohesion: 0.12
Nodes (15): OAuth.AuthServer.WebAPI.Infrastructure, IHostedService, CancellationToken, IdentityRole, IServiceProvider, RoleManager, Task, UserManager (+7 more)

### Community 22 - "Admin Scope UI"
Cohesion: 0.10
Nodes (19): route:/scopes/edit/{Id}, route:/scopes/new, OnInitializedAsync, ISnackbar, MudButton, MudCard, MudCardActions, MudCardContent (+11 more)

### Community 23 - "OpenIddict Scopes"
Cohesion: 0.14
Nodes (14): IEnumerable, IOpenIddictScopeManager, Items, ScopeViewModel, Task, Total, ScopeAdminService, List (+6 more)

### Community 24 - "MVC Client Sample"
Cohesion: 0.18
Nodes (8): Given, IPage, ScenarioContext, Task, Then, When, 同意頁面Step, Page

### Community 25 - "Admin MudBlazor UI"
Cohesion: 0.11
Nodes (18): AntiforgeryToken, Authorized, AuthorizeView, MudAppBar, MudContainer, MudDialogProvider, MudDrawer, MudLayout (+10 more)

### Community 26 - "Identity & Users"
Cohesion: 0.17
Nodes (16): oidc-client-ts, getUser(), isLoggedIn(), login(), logout(), POST_LOGOUT_URI, REDIRECT_URI, userManager (+8 more)

### Community 27 - "Admin API Contracts"
Cohesion: 0.12
Nodes (14): List, AddUserRolesRequest, RolesToAdd, UserName, List, RemoveUserRolesRequest, RolesToRemove, UserName (+6 more)

### Community 28 - "Admin API Contracts"
Cohesion: 0.12
Nodes (13): RemoveClaimRequest, ClaimToRemove, Owner, UpdateClaimRequest, Modified, Original, Owner, Claim (+5 more)

### Community 29 - "Tsconfig.Node.Json Compileroptions"
Cohesion: 0.12
Nodes (16): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+8 more)

### Community 30 - "Applicationurl Commandname"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 31 - "Identity & Users"
Cohesion: 0.12
Nodes (16): DateTimeOffset, List, UserDetailsViewModel, AccessFailedCount, Email, EmailConfirmed, Id, IsLockedOut (+8 more)

### Community 32 - "Applicationurl Commandname"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 33 - "Applicationurl Commandname"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 34 - "Applicationurl Commandname"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 35 - "Applicationurl Commandname"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 36 - "Identity & Users"
Cohesion: 0.19
Nodes (8): IList, IEnumerable, Items, Task, Total, UserDetailsViewModel, UserManager, UserAdminService

### Community 37 - "OAuth Token Service"
Cohesion: 0.26
Nodes (7): IPage, ScenarioContext, Task, Then, When, SpaHost驗證流程Step, Page

### Community 38 - "OpenIddict Applications"
Cohesion: 0.14
Nodes (13): DbContextOptions, DbSet, IdentityDbContext, OpenIddictEntityFrameworkCoreApplication, OpenIddictEntityFrameworkCoreAuthorization, OpenIddictEntityFrameworkCoreScope, OpenIddictEntityFrameworkCoreToken, ModelBuilder (+5 more)

### Community 39 - "Admin MudBlazor UI"
Cohesion: 0.14
Nodes (12): Microsoft.AspNetCore.Components.Authorization, Microsoft.AspNetCore.Components.Forms, Microsoft.AspNetCore.Components.Routing, Microsoft.AspNetCore.Components.Web, Microsoft.AspNetCore.Components.Web.RenderMode, Microsoft.AspNetCore.Components.Web.Virtualization, Microsoft.JSInterop, MudBlazor (+4 more)

### Community 40 - "Playwright UI Testing"
Cohesion: 0.18
Nodes (11): IBrowser, IBrowserContext, IPage, IPlaywright, Task, AdminUIFixture, AdminPassword, AdminUIBase (+3 more)

### Community 41 - "Identity & Users"
Cohesion: 0.34
Nodes (4): Fact, IPage, Task, AdminUITests

### Community 42 - "Playwright UI Testing"
Cohesion: 0.18
Nodes (11): IBrowser, IBrowserContext, IPage, IPlaywright, Task, MvcClientFixture, AdminPassword, AdminUserName (+3 more)

### Community 43 - "Playwright UI Testing"
Cohesion: 0.18
Nodes (11): IBrowser, IBrowserContext, IPage, IPlaywright, Task, SpaHostFixture, AdminPass, AdminUser (+3 more)

### Community 44 - "(10.0.9) (7.5.0)"
Cohesion: 0.15
Nodes (12): AspNet.Security.OAuth.Line (10.0.0), CSharpFunctionalExtensions (3.7.0), FluentValidation.AspNetCore (11.3.1), Microsoft.AspNetCore.Authentication.Facebook (10.0.9), Microsoft.AspNetCore.Authentication.Google (10.0.9), Microsoft.AspNetCore.Authentication.MicrosoftAccount (10.0.9), net10.0, Microsoft.AspNetCore.OpenApi (10.0.2) (+4 more)

### Community 45 - "Integration Test Suite"
Cohesion: 0.15
Nodes (12): JsonPath.Net (3.0.2), net10.0, coverlet.collector (6.0.4), FluentAssertions (8.10.0), Microsoft.AspNetCore.Mvc.Testing (10.0.9), Microsoft.NET.Test.Sdk (17.14.1), Reqnroll.xUnit (3.3.4), System.IdentityModel.Tokens.Jwt (8.19.1) (+4 more)

### Community 46 - "Integration Test Suite"
Cohesion: 0.15
Nodes (12): Microsoft.Extensions.Http (10.0.0), net10.0, coverlet.collector (10.0.1), FluentAssertions (8.10.0), Microsoft.AspNetCore.Mvc.Testing (10.0.9), Microsoft.NET.Test.Sdk (17.14.1), Reqnroll.xUnit (3.3.4), System.IdentityModel.Tokens.Jwt (8.19.1) (+4 more)

### Community 47 - "Router Vue-Router"
Cohesion: 0.19
Nodes (8): vue, vue-router, handleCallback(), count, router, error, loading, router

### Community 48 - "MVC Client Sample"
Cohesion: 0.26
Nodes (7): IPage, ScenarioContext, Task, Then, When, MvcClient驗證流程Step, Page

### Community 49 - "MVC Client Sample"
Cohesion: 0.23
Nodes (12): Multi-Archetype Client Support (MVC, API, SPA), OAuth2 OIDC Server Master Implementation Plan, OpenIddict and ASP.NET Core 10 Architecture, Social Login Federation Architecture, Client WebAPI Integration Test 401 Issue, JwtBearer Middleware 401 in WebApplicationFactory, AuthServer Integration Test Issues Record, EF Core Migration vs IHostedService Startup Race (+4 more)

### Community 50 - "AuthServer Legacy UI"
Cohesion: 0.24
Nodes (4): OAuth.AuthServer.DB, OAuth.AuthServer.WebAPI.Pages.Connect, OAuth.AuthServer.WebAPI.Connect, Microsoft.AspNetCore.Authorization

### Community 51 - "Identity & Users"
Cohesion: 0.18
Nodes (6): OAuth.AuthServer.Admin.WebUI.ViewModels, OAuth.AuthServer.Admin.WebUI.Services, SwitchItemViewModel, DisplayName, IsSelected, ItemValue

### Community 52 - "Admin API Contracts"
Cohesion: 0.27
Nodes (8): HttpDelete, HttpPut, HttpGet, IActionResult, Task, UserManager, AccountManagementController, ChangePasswordRequest

### Community 53 - "OAuth Client Integrations"
Cohesion: 0.20
Nodes (9): IAsyncLifetime, IConfiguration, BeforeScenario, HttpClient, Task, TestFixture, Config, TestFactory (+1 more)

### Community 54 - "@Types/Node Typescript"
Cohesion: 0.18
Nodes (10): @types/node, typescript, vite, @vitejs/plugin-vue, vue-tsc, @vue/tsconfig, name, private (+2 more)

### Community 55 - "Identity & Users"
Cohesion: 0.20
Nodes (8): IdentityRole, IEnumerable, Items, RoleManager, Task, Total, UserRoleViewModel, RoleAdminService

### Community 56 - "OAuth Client Integrations"
Cohesion: 0.18
Nodes (9): Program, IWebHostBuilder, Task, AuthServerTestFactory, HttpClient, IWebHostBuilder, WebApiTestFactory, WebApiClient (+1 more)

### Community 57 - "OAuth Token Service"
Cohesion: 0.20
Nodes (11): Sequence Diagrams & State Machine Plan, OIDC Authorization Code with PKCE Sequence Diagram, Token Lifecycle and Rotation State Machine, Fix Diagram Syntax Plan, Mermaid Sequence Diagram Alias Quoting Rule, Fix Mermaid Syntax Plan Take 2, State Diagram Note and Transition Quoting Standards, Diagram Syntax Error Issue Record (+3 more)

### Community 58 - "@Vue/Tsconfig/Tsconfig.Dom.Json Tsconfig.App.Json"
Cohesion: 0.18
Nodes (10): @vue/tsconfig/tsconfig.dom.json, compilerOptions, erasableSyntaxOnly, noFallthroughCasesInSwitch, noUnusedLocals, noUnusedParameters, tsBuildInfoFile, types (+2 more)

### Community 59 - "Identity & Users"
Cohesion: 0.20
Nodes (9): CancellationToken, Claim, HttpPost, IActionResult, IEnumerable, SignInManager, Task, UserManager (+1 more)

### Community 60 - "Admin MudBlazor UI"
Cohesion: 0.20
Nodes (9): MudButton, MudCard, MudCardActions, MudCardContent, MudGrid, MudIcon, MudItem, MudText (+1 more)

### Community 61 - "Playwright UI Testing"
Cohesion: 0.20
Nodes (9): net10.0, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), Microsoft.Playwright (1.61.0), Reqnroll.xUnit (3.3.4), Testcontainers.PostgreSql (4.12.0), xunit (2.9.3), xunit.runner.visualstudio (3.1.4) (+1 more)

### Community 62 - "MVC Client Sample"
Cohesion: 0.20
Nodes (8): TestSettings, AdminPassword, AdminUIBase, AdminUserName, AuthServerBase, MvcClientBase, SpaHostBase, WebApiBase

### Community 63 - "MVC Client Sample"
Cohesion: 0.39
Nodes (5): IClassFixture, Fact, IPage, Task, MvcClientTests

### Community 64 - "Playwright UI Testing"
Cohesion: 0.22
Nodes (8): net10.0, coverlet.collector (6.0.4), Microsoft.NET.Test.Sdk (17.14.1), Microsoft.Playwright (1.61.0), Reqnroll.xUnit (3.3.4), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk

### Community 65 - "OAuth Token Service"
Cohesion: 0.44
Nodes (4): Fact, IPage, Task, SpaHostTests

### Community 66 - "Admin MudBlazor UI"
Cohesion: 0.25
Nodes (7): AuthorizeRouteView, CascadingAuthenticationState, FocusOnNavigate, Found, RedirectToLogin, Router, NotAuthorized

### Community 67 - "MVC Client Sample"
Cohesion: 0.25
Nodes (5): net10.0, Microsoft.AspNetCore.Authentication.OpenIdConnect (10.0.9), Microsoft.NET.Sdk.Web, net10.0, Microsoft.NET.Sdk.Web

### Community 68 - "Admin MudBlazor UI"
Cohesion: 0.32
Nodes (6): handleReconnectStateChanged(), reconnectModal, resumeButton, retry(), retryButton, retryWhenDocumentBecomesVisible()

### Community 70 - "OAuth Client Integrations"
Cohesion: 0.38
Nodes (4): OAuth.Client.WebAPI.Controllers, HttpGet, IActionResult, MeController

### Community 71 - "Admin MudBlazor UI"
Cohesion: 0.29
Nodes (6): AutoMapper (16.1.1), MudBlazor (7.*), net10.0, Microsoft.AspNetCore.Authentication.OpenIdConnect (10.0.9), OpenIddict.AspNetCore (7.5.0), Microsoft.NET.Sdk.Web

### Community 72 - "Identity & Users"
Cohesion: 0.29
Nodes (6): Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.9), Npgsql.EntityFrameworkCore.PostgreSQL (10.0.2), net10.0, Microsoft.EntityFrameworkCore.Design (10.0.9), OpenIddict.EntityFrameworkCore (7.5.0), Microsoft.NET.Sdk

### Community 73 - "Devdependencies @Types/Node"
Cohesion: 0.29
Nodes (7): devDependencies, @types/node, typescript, vite, @vitejs/plugin-vue, vue-tsc, @vue/tsconfig

### Community 74 - "OAuth Client Integrations"
Cohesion: 0.48
Nodes (7): Bluesky Icon, Discord Icon, Documentation Icon, GitHub Icon, Social Icon, SPA Client SVG Icon Sprite, X (Twitter) Icon

### Community 75 - "Admin MudBlazor UI"
Cohesion: 0.33
Nodes (5): HeadOutlet, ImportMap, ReconnectModal, ResourcePreloader, Routes

### Community 76 - "OAuth Client Integrations"
Cohesion: 0.40
Nodes (4): Microsoft.AspNetCore.Authentication.JwtBearer (10.0.9), net10.0, Microsoft.AspNetCore.OpenApi (10.0.2), Microsoft.NET.Sdk.Web

### Community 77 - "OAuth Client Integrations"
Cohesion: 0.50
Nodes (5): Admin WebUI Blazor Favicon, SPA Client Vite Favicon, SPA Client Isometric Hero Graphic, Vite Logo, Vue Logo

### Community 79 - "Admin MudBlazor UI"
Cohesion: 0.50
Nodes (3): MudNavGroup, MudNavLink, MudNavMenu

### Community 80 - "Admin API Contracts"
Cohesion: 0.50
Nodes (3): AddClaimRequest, ClaimToAdd, Owner

### Community 81 - "OAuth Client Integrations"
Cohesion: 0.50
Nodes (4): dependencies, oidc-client-ts, vue, vue-router

### Community 82 - "Scripts Build"
Cohesion: 0.50
Nodes (4): scripts, build, dev, preview

### Community 83 - "Worktree Admin"
Cohesion: 1.00
Nodes (3): Admin Worktree Investigation Plan, Git Worktree Workspace Isolation, Subagent Task Dispatch Protocol

### Community 84 - "Identity & Users"
Cohesion: 1.00
Nodes (3): Blazor OAuth Admin WebUI, Pixel Identity Integration Plan, Shared PostgreSQL Schema Integration Strategy

### Community 85 - "OAuth Client Integrations"
Cohesion: 1.00
Nodes (3): oidc-client-ts Authentication Integration, Vue ClientApp and ASP.NET Core SpaHost Consolidation, SPA Project Merge Plan

### Community 90 - "Jquery License"
Cohesion: 0.67
Nodes (3): jQuery Core MIT License, jQuery Validation Plugin MIT License, jQuery Validation Unobtrusive MIT License

## Knowledge Gaps
- **551 isolated node(s):** `ResourcePreloader`, `ImportMap`, `HeadOutlet`, `Routes`, `ReconnectModal` (+546 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 782 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **12 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `OAuth.AuthServer.DB` connect `AuthServer Legacy UI` to `Integration Test Suite`, `AuthServer Legacy UI`, `Admin MudBlazor UI`, `Admin API Contracts`, `OpenIddict Applications`, `Admin API Contracts`, `Identity & Users`, `Identity & Users`, `OpenIddict Applications`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **Why does `TestFixture` connect `OAuth Client Integrations` to `OAuth Client Integrations`, `Admin API Contracts`, `Integration Test Suite`?**
  _High betweenness centrality (0.090) - this node is a cross-community bridge._
- **Why does `WebApiTestFactory` connect `OAuth Client Integrations` to `OAuth Client Integrations`, `Integration Test Suite`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **What connects `ResourcePreloader`, `ImportMap`, `HeadOutlet` to the rest of the system?**
  _551 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Playwright UI Testing` be split into smaller, more focused modules?**
  _Cohesion score 0.07676767676767676 - nodes in this community are weakly interconnected._
- **Should `Architecture & Specs` be split into smaller, more focused modules?**
  _Cohesion score 0.04421768707482993 - nodes in this community are weakly interconnected._
- **Should `Integration Test Suite` be split into smaller, more focused modules?**
  _Cohesion score 0.047619047619047616 - nodes in this community are weakly interconnected._