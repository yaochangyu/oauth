# WebUI 跨模組真實場景 E2E 開發者指南

本功能定義透過 Playwright E2E 驅動之全鏈路整合場景，涵蓋註冊新會員並登入第三方 App、授權碼流程同意跳轉、拒絕跳轉、兩階段登入（PermanentAuthorization 略過同意頁）以及 SPA 透過 PKCE 登入之完整自動化流程。

## 流程架構圖

[檢視全鏈路 E2E 架構圖 (HTML)](./diagram.html)

## 主要操作流程

1. **場景一：真實使用者註冊新會員並透過第三方 App 成功登入**
   - 瀏覽器開啟第三方 App 觸發 OIDC 導向至認證站 `/login`。
   - 點擊「註冊」填寫真實信箱密碼送出。
   - 註冊成功後返回登入頁輸入新帳密登入，自動跳轉同意頁並授權成功返回第三方 App。

2. **場景三：第三方 App 發起授權碼流程，用戶同意後跳轉**
   - 發起 `/connect/authorize` 請求帶 `response_type=code`。
   - 進入 `/consent` 頁面顯示請求之 Scopes。
   - 點擊「同意」，認證站簽發 code 並 302 重導向至 `redirect_uri`，第三方 App 換取 Access Token 完成登入。

3. **場景四：使用者拒絕同意導回第三方並顯示授權失敗**
   - 於 Consent 頁面點擊「拒絕 (Deny)」。
   - 認證站重導向回第三方並附加 `error=access_denied`。
   - 第三方 App 正確捕捉錯誤並於畫面顯示「使用者已拒絕授權」。

4. **場景七：兩階段登入 (PermanentAuthorization) 略過同意頁**
   - 針對已建立 Permanent Authorization 的用戶與 Client，登入後系統自動跳過同意頁，直接發放授權碼。

5. **場景外加：SPA 應用程式透過 PKCE 登入授權**
   - SPA 於前端產製 `code_challenge` 並跳轉認證站。
   - 登入並完成同意後，SPA 於回調頁（Callback）使用 `code_verifier` 換取 JWT Token 並儲存於前端狀態。

## 常見錯誤與例外狀況

- **E2E 測試找不到畫面元素 (Selector Timeout)** → 測試腳本失敗 → 前端 DOM 結構變更或非同步載入延遲 → 使用 `data-testid` 穩定定位屬性，並搭配 `await page.WaitForSelectorAsync()`。
- **跳轉時發生 Cookie 未傳遞 (SameSite=Strict 跨域限制)** → 認證站將使用者判定為未登入並再次要求登入 → 檢查認證 Cookie 之 `SameSite` 設定應為 `Lax` 以支援 OIDC 頂層重導向。
- **SPA PKCE 換 Token 缺少 code_verifier** → Token 端點回傳 `400 Bad Request` (`{"error": "invalid_grant"}`) → SPA 回調處理器未自 SessionStorage 取回暫存之 verifier → 確保在發起跳轉前正確寫入本地暫存。

## 你現在可以做的下一步

- 執行完整的 WebUI E2E 測試套件：`dotnet test test/OAuth.AuthServer.WebUI.E2E`
- 檢閱後端 Consent 端點實作：參閱 [授權同意 API 指南](../consent-api/consent-api-for-developers.md)
