# 雙金鑰輪替與零停機過渡開發者操作指南

本功能提供機密型應用程式在金鑰即將到期或需定期輪替時，透過雙金鑰（Primary 與 Retiring Secret）並存機制實現零停機（Zero-downtime）無縫平滑切換。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **觸發金鑰輪替 (產生新金鑰並保留舊金鑰過渡)**
   呼叫 `POST /api/v1/developer/apps/{id}/rotate-secret`。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/rotate-secret`
   - **Header**: `Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/rotate-secret \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```
   回應範例：
   ```json
   {
     "primarySecret": "sec_NEW_KEY_77a98b2c...",
     "retiringSecret": "sec_OLD_KEY_11e23f4d...",
     "retiringExpiresAt": "2026-09-20T12:00:00Z"
   }
   ```
   *此時處於雙金鑰並存狀態：客戶端無論使用新金鑰或舊金鑰請求 `/connect/token` 均能正常通過驗證。*

2. **客戶端線上服務部署更新為新金鑰**
   逐步更新所有生產環境微服務節點的 `client_secret` 為 `primarySecret`。

3. **正式廢止舊金鑰 (Revoke Retiring Secret)**
   確認所有服務節點皆已完成更新後，呼叫廢止端點。
   - **HTTP Method**: `POST`
   - **URL**: `https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/revoke-retiring-secret`

   ```bash
   curl -X POST https://developer.example.com/api/v1/developer/apps/app_5e97bdf0/revoke-retiring-secret \
     -H "Authorization: Bearer <DEVELOPER_ACCESS_TOKEN>"
   ```
   回應狀態為 `200 OK`，此後舊金鑰立即失效，僅新金鑰有效。

## 常見錯誤與例外狀況

- **使用已廢止的舊金鑰換 Token** → 回傳 `401 Unauthorized` (`{"error": "invalid_client"}`) → 舊金鑰已從資料庫完全刪除作廢 → 檢查微服務是否仍有殘留節點使用舊金鑰配置。
- **非 App 擁有者嘗試觸發金鑰輪替** → 回傳 `403 Forbidden` → 越權存取他人 App 金鑰 → 確保操作者為 App 登記之合法開發者。
- **在尚未處於輪替過渡狀態時呼叫廢止舊金鑰** → 回傳 `400 Bad Request` (`{"message": "當前無處於退役中之金鑰"}`) → 目前僅有單一 Primary 金鑰 → 無須執行廢止動作。

## 你現在可以做的下一步

- 於沙盒工具驗證新金鑰：參閱 [開發者帳號與沙盒指南](../developer-account-sandbox/developer-account-sandbox-for-developers.md)
- 執行雙金鑰輪替測試：`dotnet test src/Developer/OAuth.Developer.Tests --filter "FullyQualifiedName~雙金鑰輪替"`
