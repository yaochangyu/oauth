# 循序圖與憑證狀態機設計計畫

本計畫旨在根據當前專案的實作現況，為 OAuth2 / OIDC Authorization Server 以及開發環境中的憑證管理，撰寫完整的循序圖與狀態機設計文件。

## 計畫步驟

- [x] **Step 1 - 建立設計文件骨架**
  - **說明**：在 `doc/` 目錄下建立新文件 `doc/diagrams.md`，做為存放循序圖與狀態機的載體。
- [x] **Step 2 - 撰寫 OIDC 核心授權流程循序圖**
  - **說明**：使用 Mermaid 語法撰寫完整的 OIDC Authorization Code Flow + PKCE 以及 Social Login 登入與本地帳號綁定的交互循序圖，反映現有 Code 控制器的行為。
- [x] **Step 3 - 撰寫 OIDC Token 狀態機 (A 部分)**
  - **說明**：使用 Mermaid 語法撰寫 OpenIddict 所發放的 Authorization Code、Access Token 以及 Refresh Token (包含 Rotation) 的生命週期與失效狀態機。
- [x] **Step 4 - 撰寫開發與測試環境憑證狀態機 (B 部分)**
  - **說明**：使用 Mermaid 語法撰寫專案開發、測試與 git 所用憑證（如 `~/.claude/creds/.creds`、git credential helper、Social Login Secrets 等）在安全流轉、輪替與撤銷時的狀態機。
- [x] **Step 5 - 更新專案資料夾結構 tree.md**
  - **說明**：將新增 of `doc/diagrams.md` 文件路徑加入至 `tree.md` 中，維持專案目錄結構的同步與精確度。
- [x] **Step 6 - 專案建置驗證與收尾**
  - **說明**：執行 `task build` 驗證專案，待使用者確認所有圖表後，將此計畫檔案封存移動至 `.archive/` 目錄。
