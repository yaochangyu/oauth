# Token 驗證與安全性開發者操作指南

本功能定義系統在驗證傳入之 JWT Token 時所執行的嚴格加密校驗規則，包含對稱金鑰偽造防禦、非信任 RSA 金鑰防禦以及 Issuer、Audience 聲明檢驗。

## 流程架構圖

[檢視架構與流程圖 (HTML)](./diagram.html)

## 主要操作流程

1. **發送帶有 JWT 之 API 請求**
   客戶端呼叫受保護 API 時夾帶 Token。
   - **Header**: `Authorization: Bearer <TOKEN>`

2. **中介軟體執行四大安全性校驗**
   - **簽章金鑰校驗**：比對 JWT Header 之 `kid` 與 AuthServer 公鑰集合，拒絕自製對稱金鑰（HMAC-SHA256）與未知 RSA 私鑰簽發之 Token。
   - **Issuer (簽發者) 檢驗**：宣告之 `iss` 必須完全等於合法認證站網址（如 `https://auth.example.com/`）。
   - **Audience (受眾) 檢驗**：宣告之 `aud` 必須包含目標資源伺服器之 Client ID 或 Resource 識別碼。
   - **效期檢驗**：檢查 `nbf`（生效時間）與 `exp`（過期時間）。

## 常見錯誤與例外狀況

- **使用自製對稱金鑰 (HS256) 偽造 Token 存取** → 回傳 `401 Unauthorized` → 系統嚴格要求使用 RSA256 非對稱簽章，拒絕 HS256 演算法 → 僅接受官方 AuthServer RSA 私鑰簽發之 Token。
- **使用未知第三方 RSA 私鑰簽發之 Token** → 回傳 `401 Unauthorized` → 金鑰指紋 (kid) 未在認證伺服器 JWKS 清單中 → 嚴格使用授權伺服器頒發之 Token。
- **偽造 Issuer (iss Claim 不匹配)** → 回傳 `401 Unauthorized` → Token 中的簽發者宣告與系統設定之權威認證站不符 → 確保連線至正確之環境認證伺服器。
- **偽造 Audience (aud Claim 不匹配)** → 回傳 `401 Unauthorized` → Token 未授權存取當前資源伺服器 → 申請授權時需加入目標資源之 Scope。

## 你現在可以做的下一步

- 了解應用程式生命週期管理：參閱 [應用程式生命週期與 PKCE 指南](../application-lifecycle-pkce/application-lifecycle-pkce-for-developers.md)
- 執行 Token 安全性檢驗測試：`dotnet test src/Account/OAuth.Account.Tests --filter "FullyQualifiedName~Token驗證與安全性"`
