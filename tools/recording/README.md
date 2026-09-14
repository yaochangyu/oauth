# OAuth Scenario Recording Tool

這是一個基於 Node.js + Playwright 的瀏覽器自動化操作與錄影工具，用於錄製 OAuth / OIDC 相關場景並輸出為 `.webm` 影片。

## 安裝

在 `tools/recording` 目錄下：

```bash
npm install
npx playwright install chromium
```

## 使用方式

### 基本用法

錄製預設場景（`login-consent`），產生的影片會儲存至 `output/login-consent-<timestamp>.webm`：

```bash
node record-scenario.js
# 或使用 npm script
npm run record
```

### 指定自訂參數

```bash
# 指定場景與輸出檔案路徑
node record-scenario.js --scenario login-consent --output ./output/my-demo.webm

# 或透過環境變數傳入
SCENARIO=login-consent OUTPUT_PATH=./output/my-demo.webm node record-scenario.js
```

### 參數說明

| 參數 | 縮寫 | 說明 | 預設值 |
|---|---|---|---|
| `--scenario` | `-s` | 欲錄製的場景名稱 | `login-consent` |
| `--output` | `-o` | 輸出影片路徑（.webm） | `./output/<scenario>-<timestamp>.webm` |
| `--help` | `-h` | 顯示參數說明 | - |

| 環境變數 | 說明 | 預設值 |
|---|---|---|
| `SCENARIO` | 欲錄製的場景名稱 | `login-consent` |
| `OUTPUT_PATH` | 輸出影片路徑 | - |
| `AUTH_SERVER_URL` | AuthServer 基礎網址 | `https://localhost:7001` |
| `MVC_CLIENT_URL` | MVC Client 基礎網址 | `https://localhost:5101` |

## 如何擴充新場景

1. 在 `scenarios/` 目錄下新增場景實作檔案（例如 `scenarios/refresh-token.js`），並匯出非同步函式：
   ```javascript
   export async function runRefreshToken(page, options = {}) {
     // 步驟實作
   }
   ```
2. 在 `scenarios/index.js` 中註冊該場景名稱：
   ```javascript
   import { runRefreshToken } from './refresh-token.js';

   export const scenarios = {
     'login-consent': runLoginConsent,
     'refresh-token': runRefreshToken,
   };
   ```
3. 執行新場景：
   ```bash
   node record-scenario.js --scenario refresh-token
   ```
