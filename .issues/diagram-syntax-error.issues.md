# 循序圖語法錯誤記錄

## 失敗步驟
- **步驟**：將修正後的循序圖寫入 `doc/diagrams.md` 與 `doc/oauth2-oidc-sequence-diagrams.md`。
- **失敗方法**：在 Mermaid 循序圖的 database 別名中，直接使用 `database DB as 資料庫 (Identity + OpenIddict)`。

## 失敗原因
- **原因**：Mermaid 循序圖（Sequence Diagram）的參與者別名（alias）若包含特殊字元（如 `+`、`(`、`)` 或空格），必須使用雙引號將名稱括起來（例如 `"資料庫 (Identity + OpenIddict)"`）。直接寫入會導致 Mermaid 解析器報錯：`Expecting 'NEWLINE', ..., got '+'`。

## 解決對策
- **對策**：在 `doc/diagrams.md` 與 `doc/oauth2-oidc-sequence-diagrams.md` 中，將 `database DB as 資料庫 (Identity + OpenIddict)` 修改為 `database DB as "資料庫 (Identity + OpenIddict)"`。
