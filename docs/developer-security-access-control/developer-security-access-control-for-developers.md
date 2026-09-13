# 開發者模組安全性與權限控制操作指南

本功能定義開發者中心在多租戶環境下的嚴格安全防禦邊界，包含防範偽造 HTTP Header 竄改身分、跨租戶越權存取（IDOR）攔截以及 Token 偽造防護。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **強制從 JWT Token 解析擁有者身分**
   開發者模組端點全面忽略客戶端傳入之自訂 Header（如 `X-Developer-UserId`），唯一信任 JWT Payload 中經過數位簽章的 `sub` Claim 作為資料歸屬擁有者。

2. **跨租戶資源隔離檢查**
   當開發者 A 嘗試查詢、修改、刪除屬於開發者 B 的應用程式或金鑰時，伺服器執行擁有權比對並回傳 `403 Forbidden`。

3. **金鑰與審核越權防禦**
   跨帳號請求金鑰輪替、廢止或送審均被嚴格攔截。

## 常見錯誤與例外狀況

- **未帶 Token 存取開發者 API** → 回傳 `401 Unauthorized` → 缺少身分驗證憑證 → 需先登入並在 Header 附帶 `Authorization: Bearer <token>`。
- **跨使用者存取他人 App (IDOR 越權存取)** → 回傳 `403 Forbidden`，Body 為 `{"message": "您無權存取或修改此應用程式"}` → 該 App 擁有者不是當前登入者 → 僅能操作自己帳號名下建立之應用程式。
- **偽造 X-Developer-UserId Header 嘗試冒用他人身分** → 系統忽略該 Header 並僅以 Token sub claim 建立資料 → 攻擊無效，資料正確歸屬於 Token 簽發者。
- **使用偽造 Issuer / Audience 之 Token 存取** → 回傳 `401 Unauthorized` → 簽章與屬性校驗失敗 → 確保使用官方 AuthServer 簽發之合法 Token。

## 你現在可以做的下一步

- 了解管理員後台權限控制：參閱 [管理員權限保護指南](../admin-authorization-protection/admin-authorization-protection-for-developers.md)
- 執行開發者安全性與權限測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~安全性與權限控制"`
