# Mermaid 語法錯誤二次記錄

## 失敗步驟
- **步驟**：修正 `doc/diagrams.md` 與 `doc/oauth2-oidc-sequence-diagrams.md` 中的 Mermaid 語法。
- **失敗方法**：
  1. 使用 `database DB as "資料庫 (Identity + OpenIddict)"` 語法。
  2. 在狀態遷移圖中，使用 `</note>` 閉合 note。
  3. 在狀態遷移圖中，遷移描述包含 `:` 字元且未加引號（如 `: remote 保持 https://host/repo.git`）。

## 失敗原因
1. **別名語法錯誤**：Mermaid 循序圖中，若要使用包含特殊字元或空格的顯示名稱，正確的語法格式為 `database "顯示名稱" as 別名`（即雙引號在 `as` 之前），而非 `database 別名 as "顯示名稱"`。
2. **Note 閉合錯誤**：狀態機圖（State Diagram）中的多行 note 必須使用 `end note` 閉合，使用 HTML 格式的 `</note>` 會導致 Lexical error。
3. **遷移描述解析錯誤**：狀態機圖的遷移描述（`:` 後方的文字）若包含 `:` 或 `/` 等特殊字元，必須將整個描述用雙引號括起來，否則解析器會誤判。

## 解決對策
1. 將循序圖中的資料庫宣告改為：
   - `database "資料庫 (Identity + OpenIddict)" as DB`
   - `database "資料庫 (Identity)" as DB`
   - `database "資料庫 (OpenIddict)" as DB`
2. 將狀態圖中的 `</note>` 改為 `end note`。
3. 將狀態圖中包含網址的遷移描述加上引號，例如：`: "remote 保持 https://host/repo.git"`。
