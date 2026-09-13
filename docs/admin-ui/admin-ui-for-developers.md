# AdminUI 管理後台操作指南

本功能提供系統管理員與維運人員透過 Web 管理介面進行第三方應用程式審核、使用者帳號狀態維護（凍結/解除）、Scope 權限配置與安全審計。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **以 Administrator 身分登入 AdminUI**
   打開 AdminUI 後台網址（如 `https://admin.example.com`），輸入具備管理員權限之帳密登入，取得 Admin Session。

2. **進入「應用審核 (App Review)」頁籤進行審核**
   - 點擊左側選單「應用程式審核」，檢視待審核（InReview）清單。
   - 點選特定 App 查看申請人、Redirect URI 與要求的 Scope 權限。
   - 點擊「核准 (Approve)」將狀態變更為 Approved，或點擊「駁回 (Reject)」填寫退回原因。

3. **使用者狀態管理與凍結 (User Management)**
   - 進入「使用者管理」列表，使用搜尋框過濾使用者 Email 或 Id。
   - 針對違規帳號點擊「凍結帳號 (Lockout)」，該使用者將立即無法登入。
   - 點擊「終止所有會話 (Revoke Sessions)」強制登出該使用者所有設備。

4. **查看授權審計日誌 (Audit Logs)**
   進入「審計日誌」頁籤，可依時間區間查詢所有用戶的 Consent 授權與撤銷歷史記錄。

## 常見錯誤與例外狀況

- **以非管理員帳號登入 AdminUI** → 畫面顯示「403 Forbidden 存取被拒」或導向錯誤頁 → 該帳號缺少 `Administrator` 角色宣告 → 請在資料庫或透過 Super Admin 賦予該帳號管理員角色。
- **審核時對已核准 (Approved) 的 App 重複點擊核准** → 後端回傳 `400 Bad Request`，介面彈出錯誤提示 → 狀態機不允許從 Approved 再次轉移至 Approved → 重新整理頁面獲取最新狀態。
- **搜尋不存在的 Client 或 User** → 列表顯示「查無相符資料（空列表）」 → 關鍵字無匹配項目 → 檢查輸入之關鍵字是否正確。

## 你現在可以做的下一步

- 執行 AdminUI E2E 自動化測試：`dotnet test test/OAuth.AuthServer.WebUI.E2E --filter "FullyQualifiedName~AdminUI"`
- 檢閱後端 API 實作細節：參閱 [第三方應用審核流指南](../app-review-workflow/app-review-workflow-for-developers.md)
