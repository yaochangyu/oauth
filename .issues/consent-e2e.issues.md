# Consent E2E 測試問題紀錄

## 問題 1：Playwright WaitForURLAsync 超時（glob pattern 無法跨斜線）

**症狀**：`WaitForURLAsync("*5101*")` 超時 20 秒，即使瀏覽器確實導向到 5101。

**失敗原因**：Playwright glob 模式中 `*` 不匹配 `/`，無法匹配 `https://localhost:5101/...`（包含斜線）。

**已失敗方法**：
- `*5101*` → 不匹配 `https://localhost:5101/...`

**正確作法**：使用完整 URL 前綴 `${MvcClientBase}/**` 或 `**5101**`

---

## 問題 2：WaitForURLAsync 等 Load state 超時

**症狀**：改用 `**5101**` 後，Playwright log 顯示 "navigated to https://localhost:5101/..." 但 WaitForURLAsync 仍超時。

**失敗原因**：預設 `WaitUntilState.Load` 需要等所有 sub-resources 載入完成。Error 頁面的 layout 載入 Bootstrap/jQuery 時因環境因素導致 Load 事件遲遲未觸發。

**已失敗方法**：
- 預設 `WaitUntil = Load`

**正確作法**：`WaitUntil = WaitUntilState.DOMContentLoaded`

---

## 問題 3：OnRemoteFailure 無法偵測 access_denied

**症狀**：`ctx.Failure?.Message` 不包含 `"access_denied"`，導致 `/Home/Error?code=error` 而非 `?code=access_denied`。

**失敗原因**：OIDC middleware 對 `access_denied` 有特殊處理路徑（`HandleAccessDeniedErrorAsync`），exception message 的格式在不同版本可能不同。

**已失敗方法**：
- 只檢查 `ctx.Failure?.Message.Contains("access_denied")`

**正確作法**：同時檢查 `ctx.Request.Query["error"]`，直接讀 OAuth error response 的 query string。

---

## 問題 4：Session race condition（已在前一 context 解決）

**症狀**：同意後 redirect chain 不完整，infinite loop 回到 Consent 頁面。

**失敗原因**：`HttpContext.Session.SetString()` 是 async commit，redirect 到 `/connect/authorize` 時 session 尚未寫入。

**已失敗方法**：
- Cookie 方案（原因不明）
- Session 方案

**正確作法**：IMemoryCache + GUID token 附加在 URL query string（`?__ct=TOKEN`）

---

## 問題 5：formaction button 未觸發正確 handler（已在前一 context 解決）

**症狀**：使用 `<button asp-page-handler="Accept">` 生成 `formaction` attribute，Playwright 未正確路由。

**正確作法**：改為兩個獨立 `<form>` 各自設定 `action="/Connect/Consent?handler=Accept"` 和 `handler=Deny`
