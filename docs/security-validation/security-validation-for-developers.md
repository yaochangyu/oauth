# 安全性驗證防禦開發者操作指南

本功能定義認證伺服器在面對開放重導向（Open Redirect）、Token 偽造、CSRF 跨站請求與金鑰外洩等攻擊情境下的防護機制與檢驗規則。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **白名單 Redirect URI 檢核驗證**
   發起 OAuth 授權請求時，傳入的 `redirect_uri` 必須與 Developer Portal 註冊之有效網址完全比對一致。
   - **HTTP Method**: `GET`
   - **URL**: `https://auth.example.com/connect/authorize?client_id=mvc-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Flocalhost%3A5101%2Fsignin-oidc`

2. **驗證 Access Token 簽章與過期時間**
   資源伺服器必須使用認證站的公開 RSA 金鑰檢核 JWT 簽名有效性，禁止接受偽造金鑰或對稱簽名 Token。

3. **檢查 Cookie 安全屬性**
   確認驗證站回應的認證 Cookie 皆具備 `HttpOnly`、`Secure` 與 `SameSite=Lax` 或 `SameSite=Strict` 標籤，防止 XSS 盜取與 CSRF 攻擊。

## 常見錯誤與例外狀況

- **傳入未註冊的 redirect_uri (Open Redirect 攻擊)** → 回傳 `400 Bad Request`，Body 為 `{"error": "invalid_request", "error_description": "The specified 'redirect_uri' is not valid for this client application."}` → 請求中的跳轉網址不在該 Client 的白名單內 → 在開發者管理平台為該 Client 新增合法的 Redirect URI。
- **偽造或竄改 JWT 簽章 (Signature Invalid)** → 資源伺服器驗證中介軟體攔截並回傳 `401 Unauthorized` → Token 的數位簽章與伺服器 JWKS 公鑰不匹配 → 確保 Token 係由官方 AuthServer 簽發，且未遭傳輸竄改。
- **客戶端金鑰密碼錯誤 (Invalid Client Secret)** → `/connect/token` 回傳 `401 Unauthorized` (`{"error": "invalid_client"}`) → 傳入的 client_secret 與資料庫雜湊值不符 → 登入開發者平台確認目前有效的 Client Secret 或執行金鑰輪替。

## 你現在可以做的下一步

- 參閱應用程式金鑰輪替機制：[雙金鑰輪替操作指南](../secret-rotation/secret-rotation-for-developers.md)
- 執行安全性自動化驗證測試：`dotnet test --filter "FullyQualifiedName~安全性驗證"`
