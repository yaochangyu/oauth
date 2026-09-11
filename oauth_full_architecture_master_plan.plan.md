# OAuth 平台全景落地規劃 Master Plan (修訂版)

## Goal Description
為滿足 App 與 Web 第三方應用程式的整合需求，依照「**依業務需求劃分、前後端專案放在同一層目錄、後端單一 Host 聚合 Headless Web API、前端純 Vue 3 SPA（免 Node.js SSR 伺服器）**」的定案架構，規劃完整四階段實施藍圖。

---

## 審查修正核心定調

1. **後端架構定調：單一 Host 聚合核心（降低維運複雜度）**：
   - 避免讓資料層 `OAuth.AuthServer.DB` 產生跨模組幽靈依賴，後端以 `OAuth.AuthServer.WebAPI` 作為統一主程式 Host。
   - `Account`、`Developer`、`Admin` 的控制器與商務邏輯分別維護於各自的業務目錄中，共享統一的資料庫存取與 OpenIddict 驗證管線。
2. **前端認證模型定調：First-Party PKCE SPA + Bearer Token**：
   - 核心認證站 `OAuth.AuthServer.WebUI` 採同源靜態託管（與 AuthServer 共用同源，使用 HttpOnly Cookie 完成授權確認）。
   - 三大業務前台（`Account.WebUI`、`Developer.WebUI`、`Admin.WebUI`）正式註冊為 AuthServer 的**官方第一方客戶端（First-Party PKCE SPA）**，透過標準授權碼流程取得 Bearer Token，後端 API 統一透過 OpenIddict Validation 驗證，徹底消除跨網域 Cookie 阻擋與 CSRF 威脅！
3. **OpenIddict 原生授權與雙金鑰輪替規範**：
   - 揚棄自建 `UserConsent` 表，全面改用 OpenIddict 原生 `IOpenIddictAuthorizationManager`。
   - 機密客戶端實施 **雙金鑰輪替過渡期 (Dual-Secret Grace Period)** 與強制 PKCE。

---

## 全專案目錄結構藍圖

```
/mnt/d/lab/oauth/src/
├── AuthServer/                               # 【模組 1】核心認證與授權 (統一 Host)
│   ├── OAuth.AuthServer.WebAPI/              #    後端主程式：OpenIddict 核心端點、Token 簽發、Consent API
│   ├── OAuth.AuthServer.WebUI/               #    前端：Vue 3 核心認證視圖（/login、/consent、/register）
│   └── OAuth.AuthServer.DB/                  #    資料層：EF Core PostgreSQL、ApplicationUser、OpenIddict
│
├── Account/                                  # 【模組 2】會員自服務前台
│   ├── Controllers/                          #    會員 API (個資、安全設定、已授權 App 清單與撤銷)
│   └── OAuth.Account.WebUI/                  #    前端：Vue 3 會員中心 (First-Party PKCE SPA)
│
├── Developer/                                # 【模組 3】第三方開發者平台
│   ├── Controllers/                          #    開發者 API (App 建立、雙金鑰輪替、審核送出)
│   └── OAuth.Developer.WebUI/                #    前端：Vue 3 開發者工作台 (First-Party PKCE SPA)
│
└── Admin/                                    # 【模組 4】OAuth Server 管理後台
    ├── Controllers/                          #    管理員 API (審核 App、全域 Scope、使用者凍結)
    └── OAuth.Admin.WebUI/                    #    前端：Vue 3 管理員後台 (First-Party PKCE SPA，取代舊 Blazor)
```

---

## 四大模組詳細實施規劃 (Phase 1 ~ Phase 4)

### 階段一：AuthServer 核心 Headless 改造與認證站規劃 (最優先)
- **後端**：
  - 徹底移除舊 Razor Pages（刪除 `Pages/`）。
  - 配置 SPA 靜態託管 (`UseDefaultFiles`, `UseStaticFiles`, `MapFallbackToFile`)。
  - 掛載 ASP.NET Core Rate Limiter 防暴力破解。
  - 實作 `ConsentApiController`（嚴格檢查 `Url.IsLocalUrl()` 防 Open Redirect，使用 `IDistributedCache`）。
  - 擴充 `AccountApiController`（`login`, `logout`, `me`）。
- **前端 (`OAuth.AuthServer.WebUI`)**：
  - Vue 3 + Vite + TypeScript。
  - 四大視圖：`/login`、`/consent`、`/register`、`/error`。
  - 保留 Playwright 相容 DOM 元素。

---

### 階段二：Account 模組規劃（會員自服務前台）
- **後端**：
  - 全面採用 OpenIddict 原生 `IOpenIddictAuthorizationManager`。
  - 實作 `ConsentsController`：查詢用戶的 Permanent Authorizations，提供一鍵級聯撤銷（Revoke）。
- **前端 (`OAuth.Account.WebUI`)**：
  - First-Party PKCE SPA。
  - `AuthorizedAppsView.vue`：已授權應用管理卡片清單與「解除授權」按鈕。

---

### 階段三：Developer 模組規劃（第三方開發者平台）
- **後端**：
  - 支援 Web、SPA、Mobile 三種 App 類型，全類型預設/強制啟用 PKCE。
  - 實作**零停機雙金鑰輪替 (Dual-Secret Rotation)**：新金鑰生效、舊金鑰具備 7 天過渡期或可手動立即廢止。
- **前端 (`OAuth.Developer.WebUI`)**：
  - 應用程式管理面板、金鑰單次顯示視窗、過渡金鑰倒數警示、沙盒除錯工具。

---

### 階段四：Admin 模組規劃（OAuth Server 管理員後台）
- **後端**：
  - 第三方應用程式上線審核審批流（`AppReviewController`）。
  - 使用者狀態管理（帳號鎖定、角色授權、強制清除 Session）。
- **前端 (`OAuth.Admin.WebUI`)**：
  - 待審核應用審批工作台、全域應用目錄、使用者目錄。
  - 完成後平滑除役舊有 Blazor 專案。

---

## 驗證策略總體規劃

1. **整合測試 (BDD Reqnroll)**：
   - Phase 1：驗證 Headless Consent API 與 Open Redirect 阻擋。
   - Phase 2：驗證透過 OpenIddict 原生 Revoke 撤銷授權後，舊 Token 立即失效。
   - Phase 3：驗證金鑰輪替 7 天緩衝期內新舊 Secret 皆能換 Token，廢止後舊金鑰即刻失敗。
2. **Playwright E2E 測試**：
   - 驗證真實瀏覽器從第三方發起授權碼請求、Vue 3 登入、Consent 確認到順利回呼的完整閉環。
