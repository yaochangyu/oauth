# Playwright Background Setup 計畫

## 目標

仿照 `OAuth.AuthServer.IntegrationTest` 的作法，讓 PlaywrightTest 的 `Background` 具備「先宣告測試目標、再開啟瀏覽器」的語意，使 `.feature` 檔成為自我描述的可執行規格。

## 變更摘要

| 目前 | 改後 |
|---|---|
| `BeforeScenario` 自動建立 browser/page | browser/page 由 `Given 開啟全新的瀏覽器視窗` 建立 |
| Background 只有 `Given 開啟全新的瀏覽器視窗`（no-op） | Background 先 `Given 初始化 XXX 測試環境`，再 `And 開啟全新的瀏覽器視窗` |
| 讀者看 feature 不知道依賴哪些服務 | 讀者一眼看出此測試需要哪些服務 |

## 步驟

- [x] **步驟 1** — 將 browser/page 建立邏輯從 `[BeforeScenario]` 移至 `Given開啟全新的瀏覽器視窗()`，`[BeforeScenario]` 保留空殼（AfterScenario 仍負責清理）

- [x] **步驟 2** — 在 `PlaywrightBaseStep.cs` 新增三個 Given steps：
  - `初始化 MVC Client 測試環境`（AuthServer + MvcClient 健康檢查）
  - `初始化 SPA Host 測試環境`（AuthServer + SpaHost 健康檢查）
  - `初始化 Admin UI 測試環境`（AuthServer + AdminUI 健康檢查）

- [x] **步驟 3** — 更新 `MvcClient驗證流程.feature`：Background 改為 `Given 初始化 MVC Client 測試環境` + `And 開啟全新的瀏覽器視窗`

- [x] **步驟 4** — 更新 `SpaHost驗證流程.feature`：Background 改為 `Given 初始化 SPA Host 測試環境` + `And 開啟全新的瀏覽器視窗`

- [x] **步驟 5** — 更新 `同意頁面.feature`：Background 改為 `Given 初始化 MVC Client 測試環境` + `And 開啟全新的瀏覽器視窗`

- [x] **步驟 6** — 更新 `AdminUI管理介面.feature`：Background 改為 `Given 初始化 Admin UI 測試環境` + `And 開啟全新的瀏覽器視窗` + `And 已登入 Admin UI 管理介面`

- [x] **步驟 7** — Build 驗證
