---
計畫模板版本: 2026-07-12
用途: AI Agent 逐步實作任務，避免混亂
---

# 錄製操作介面（Playwright 影片）

**建立日期**: 2026-09-15 GMT+8
**狀態**: [完成]

⚠️ **檔案命名**: `record-ui-walkthrough-2026-09-15.plan.md`

## 概覽

- **目標**：用 Playwright 錄製「登入 + 同意授權」場景的完整操作流程，輸出 `.webm` 影片。
  同時把錄影腳本設計成可重用骨架，讓之後要加錄其他場景時可以直接複製一份平行跑，不用等前一支錄完。
- **關鍵決策**：
  1. Runtime 選擇：目前環境有 `npx playwright`（Node，v1.63.0）可用；`.NET` 的 E2E 測試專案
     （`test/OAuth.AuthServer.WebUI.E2E`）已經裝好 Playwright 瀏覽器與相關 selector 邏輯，但那是
     測試框架（Reqnroll + xUnit），拿來錄影需要多包一層。**傾向用 Node 腳本**直接呼叫 Playwright API
     開瀏覽器、操作、關閉並落地影片，最單純、啟動最快、也最容易複製成多份平行跑。
  2. 場景範圍：本次先錄「登入 + 同意授權」（MVC Client → AuthServer 登入 → 同意頁 → 導回個人資料頁），
     對應 `doc/guides/authserver-core-for-developers.md` 與 `doc/architecture/diagrams.md` 1.1 節。
  3. 帳號策略：每次錄影前用 AuthServer `/register` 建立一組全新測試帳號（隨機 email），確保一定會
     出現「同意頁面」（避免因為之前測過同一帳號、已有 Permanent Authorization 而跳過同意畫面）。
  4. 影片輸出位置：預設存在 scratchpad（`/tmp/.../scratchpad/recordings/`），**不 commit 進 git**
     （二進位影片檔會讓 repo 肥大；且內容含隨機測試帳密，不該進版控）。錄完後如果你想留存，我再
     依你指示搬到你要的位置或轉成別的格式。
- **風險**：
  - WSL 環境沒有實體螢幕，瀏覽器一定要用 headless 模式跑（Playwright headless 模式本身仍支援
    影片錄製，不受影響）。
  - 目前服務狀態：AuthServer（7001）、PostgreSQL（5432, docker）、MVC Client（5101）都已在跑，
    錄影前只需二次確認存活即可，不需要重啟。
  - 若之後想同時錄多個場景，各自用不同隨機帳號、各自獨立瀏覽器 context，彼此不會互相干擾，
    真的可以同時背景執行；風險只在於同時對 AuthServer 大量發請求時 Rate Limiting
    （`login` 端點有 `EnableRateLimiting`）可能誤觸發，平行數建議先抓 2-3 支同時跑。

## 執行步驟

| # | 步驟 | 說明 | 狀態 |
|---|------|------|------|
| 1 | 確認服務存活 | 二次確認 AuthServer/DB/MVC Client 仍在跑，避免錄到一半才發現服務掛了 | ✅ 完成 |
| 2 | 建立錄影腳本骨架 | Node + Playwright 腳本，參數化「場景名稱、帳號密碼、輸出路徑」，之後複製一份改參數就能錄新場景、可平行執行（放在 tools/recording/，進版控） | ✅ 完成（antigravity，commit aed10e4） |
| 3 | 錄製「登入 + 同意授權」 | 執行腳本，跑完整流程：註冊→登入→同意→回到 MVC Client 個人資料頁，落地 `.webm` | ✅ 完成（329KB .webm，5 步驟皆確認發生） |
| 4 | 驗證影片可播放、內容正確 | 檢查檔案存在、大小合理，回報影片路徑與時長 | ✅ 完成 |
| 5 | 合回 main | worktree yaochangyu/yaochangyu-record-login-consent-demo 完成後合併到 main | ✅ 完成（merge 11ae10f，已 push origin main，worktree 已清理） |

**狀態說明**:
- ⬜ 待做 (Not started)
- 🟦 進行中 (In progress)
- ✅ 完成 (Completed)
- ⚠️ 阻塞 (Blocked - 需要使用者決定)

## 步驟詳情

### Step 1: 確認服務存活

**預期產出**：三個 port（7001 / 5432 / 5101）連線正常的確認結果。

**完成條件**：
- [ ] AuthServer `https://localhost:7001/.well-known/openid-configuration` 回 200
- [ ] PostgreSQL 5432 可連線
- [ ] MVC Client `https://localhost:5101` 可連線

**進度**：
```
⬜ 未開始
```

---

### Step 2: 建立錄影腳本骨架

**預期產出**：
- 一支 Node 腳本（暫定路徑：`scratchpad` 底下，不進 repo，因為這是輔助工具不是產品程式碼；
  如果你想長期保留在 repo 裡方便之後重複用，跟我說，我改放 `tools/` 之類的位置並 commit）。
- 腳本接受參數：帳號/密碼（或自動產生隨機帳號）、輸出檔名、場景步驟（先寫死登入+同意這條，
  之後要加場景就複製一份改步驟）。
- 使用已核對過的實際 selector（來自 `test/OAuth.AuthServer.WebUI.E2E` 既有 E2E 測試，不是憑印象猜）：
  - 註冊頁 `input[name='email']` / `input[name='password']` / `button[type='submit']`
  - 登入頁 `input[name='userName']` / `input[type='password']` / `button[type='submit']`
  - 同意頁 `button[value='accept']`
  - MVC Client 個人資料頁驗證 `h1:has-text('個人資料')`

**完成條件**：
- [ ] 腳本可執行，headless 模式跑完不報錯
- [ ] Playwright context 有開啟 `recordVideo`

**進度**：
```
⬜ 未開始
```

---

### Step 3: 錄製「登入 + 同意授權」

**預期產出**：一支 `.webm` 影片，完整涵蓋：開啟 MVC Client 個人資料頁（觸發未登入導向）→
AuthServer 登入頁填帳密送出 → 同意頁勾選同意 → 導回 MVC Client 顯示已登入的個人資料頁。

**完成條件**：
- [ ] 影片檔案產生且非 0 bytes
- [ ] 過程中沒有出現未預期的錯誤畫面（例如 500、驗證失敗）

**進度**：
```
⬜ 未開始
```

---

### Step 4: 驗證影片可播放、內容正確

**預期產出**：回報影片路徑、檔案大小、大致時長，以及是否有任何步驟卡住的跡象（可用 ffprobe 或
直接看檔案結構判斷，不需要真的用眼睛看過整支影片）。

**完成條件**：
- [ ] 確認檔案格式正確（webm container 沒有損毀）
- [ ] 回報結果給你，問你要不要追加錄其他場景（平行執行）

**進度**：
```
⬜ 未開始
```

---

## 遭遇的問題

（尚無，執行時如遇到阻塞會記錄在這裡並同步寫 `.issues/`）

---

## 完成檢查表

計畫完成時執行：

- [ ] 所有步驟狀態都是 ✅ 完成
- [ ] 影片檔案位置與內容已回報給使用者
- [ ] 若腳本決定要留在 repo 裡，已 commit（預設不 commit，除非你指示要留）
- [ ] 計畫書已移到 `.archive/` 資料夾

---

**狀態**：規劃中，等你確認後開始 Step 1。
