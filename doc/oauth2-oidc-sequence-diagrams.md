# OAuth2 + OIDC 驗證循序圖

本文件記錄了系統中核心 OAuth2/OIDC 驗證與授權流程的循序圖。

---

## 1. 本地帳密登入（Authorization Code Flow + PKCE）

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Client as 客戶端 (SPA/MVC)
    participant AS as 授權伺服器 (AuthServer)
    database "資料庫 (Identity + OpenIddict)" as DB

    User->>Client: 點擊登入
    Note over Client: 產生 code_verifier<br/>與 code_challenge (S256)
    Client->>AS: 導向 /connect/authorize?code_challenge=...&code_challenge_method=S256
    Note over AS: 偵測使用者未登入
    AS-->>Client: 302 重導至 /Account/Login
    Client->>User: 顯示登入頁面 (Login.cshtml)
    User->>Client: 輸入本地帳號密碼並送出
    Client->>AS: POST /Account/Login
    AS->>DB: 驗證使用者帳密 (Identity)
    DB-->>AS: 驗證成功 (建立 Cookie)
    AS-->>Client: 302 重導回原始授權請求 (/connect/authorize)
    Client->>AS: 再次導向 /connect/authorize (帶有 Cookie)
    AS->>DB: 驗證客戶端與 Scope 授權 (OpenIddict)
    DB-->>AS: 驗證成功
    AS-->>Client: 302 重導並回傳 Authorization Code
    Client->>AS: POST /connect/token (grant_type=authorization_code, code, code_verifier)
    AS->>AS: 驗證 code_verifier 與挑戰碼是否相符
    AS->>DB: 記錄授權與發放 Token (OpenIddict)
    DB-->>AS: 寫入成功
    AS-->>Client: 回傳 Access Token, ID Token & Refresh Token
```

---

## 2. 外部 Social Login 聯邦登入流程

```mermaid
sequenceDiagram
    autonumber
    actor User as 使用者
    participant Client as 客戶端 (SPA/MVC)
    participant AS as 授權伺服器 (AuthServer)
    participant Provider as 外部 Identity Provider (Google等)
    database "資料庫 (Identity)" as DB

    User->>Client: 點擊使用外部帳號登入
    Client->>AS: 導向 /connect/authorize
    AS->>User: 顯示登入頁，點擊「以 Google 繼續」
    User->>AS: 觸發 Social Login Challenge
    AS-->>Provider: 302 重導至 Provider 授權頁
    User->>Provider: 輸入憑證並授權
    Provider-->>AS: 302 重導回 /signin-google (帶有 Authorization Code)
    AS->>Provider: 交換外部 Access Token 並取得 User Profile
    Provider-->>AS: 回傳 User Profile (email, name, picture)
    AS->>DB: 查詢 AspNetUserLogins 是否已綁定此外部帳號
    alt 已綁定
        DB-->>AS: 返回對應的本地 User
    else 未綁定但 Email 已存在
        AS->>DB: 自動與現有本地帳號綁定
        DB-->>AS: 綁定成功
    else 全新使用者
        AS->>DB: 建立新 ApplicationUser + 建立外部登入綁定 (AspNetUserLogins)
        DB-->>AS: 建立成功
    end
    AS-->>Client: 302 重導並回傳 OIDC Authorization Code
    Client->>AS: POST /connect/token
    AS-->>Client: 回傳本地發放的 Token (Access, Refresh, ID Token)
```
