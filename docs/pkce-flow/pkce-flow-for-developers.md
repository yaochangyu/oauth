# PKCE 授權流程開發者操作指南

本功能提供公共客戶端（SPA、Mobile App）或機密客戶端依據 RFC 7636 規範實作 Proof Key for Code Exchange (PKCE)，防止授權碼攔截攻擊。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **生成 Code Verifier 與 Code Challenge**
   客戶端本地隨機產生一組高強度字串 `code_verifier`（長度 43-128 字元），並計算其 SHA256 雜湊與 Base64Url 編碼得到 `code_challenge`。

2. **發起 PKCE 授權請求**
   引導瀏覽器前往 `/connect/authorize` 端點，帶入 `code_challenge` 與 `code_challenge_method=S256`。
   - **URL**: `https://auth.example.com/connect/authorize?client_id=spa-client&response_type=code&scope=openid%20profile&redirect_uri=https%3A%2F%2Fspa.example.com%2Fcallback&code_challenge=BASE64URL_SHA256_VERIFIER&code_challenge_method=S256`

3. **使用授權碼與 Code Verifier 換取 Token**
   在跳轉回調頁面取得 `code` 後，發送 POST 請求至 `/connect/token`，並附帶原始的 `code_verifier`。
   - **HTTP Method**: `POST`
   - **URL**: `https://auth.example.com/connect/token`
   - **Content-Type**: `application/x-www-form-urlencoded`

   ```bash
   curl -X POST https://auth.example.com/connect/token \
     -H "Content-Type: application/x-www-form-urlencoded" \
     -d "grant_type=authorization_code&client_id=spa-client&code=AUTHORIZATION_CODE&redirect_uri=https%3A%2F%2Fspa.example.com%2Fcallback&code_verifier=ORIGINAL_CODE_VERIFIER"
   ```
   回應包含 `access_token` 與 `id_token`。

## 常見錯誤與例外狀況

- **換 Token 時缺少 code_verifier** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 該 Client 啟用了 PKCE 強制要求，換 Token 必須提供 verifier → 在換 Token 請求中補上 `code_verifier`。
- **code_verifier 雜湊值與 code_challenge 不符** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 送出的 verifier 與發起授權時的 challenge 不匹配 → 檢查前端是否有多分頁覆蓋或編碼錯誤（必須使用 Base64Url 無 Padding 編碼）。
- **授權碼過期或重複使用** → 回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → 授權碼僅有效 5 分鐘且限單次使用 → 引導使用者重新發起登入。

## 你現在可以做的下一步

- 參閱開發者沙盒自動計算工具：[開發者帳號與沙盒指南](../developer-account-sandbox/developer-account-sandbox-for-developers.md)
- 執行 PKCE 整合測試：`dotnet test test/OAuth.Client.WebAPI.IntegrationTest --filter "FullyQualifiedName~PKCE"`
