#!/usr/bin/env node
/**
 * ============================================================================
 * Scenario Recording Script (Node.js + Playwright)
 * ============================================================================
 * 
 * 用法 (Usage):
 *   node record-scenario.js [選項]
 * 
 * 選項 (Options):
 *   --scenario, -s <name>    指定欲錄製的場景名稱 (預設: login-consent)
 *   --output, -o <path>      指定輸出影片路徑 (預設: ./output/<scenario>-<timestamp>.webm)
 *   --help, -h               顯示說明訊息
 * 
 * 環境變數 (Environment Variables):
 *   SCENARIO                 欲錄製的場景名稱
 *   OUTPUT_PATH              輸出影片路徑
 *   AUTH_SERVER_URL          AuthServer 網址 (預設: https://localhost:7001)
 *   MVC_CLIENT_URL           MVC Client 網址 (預設: https://localhost:5101)
 * 
 * 範例 (Examples):
 *   node record-scenario.js
 *   node record-scenario.js --scenario login-consent
 *   node record-scenario.js --scenario login-consent --output ./output/login_demo.webm
 * ============================================================================
 */

import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';
import { getScenario, listScenarios } from './scenarios/index.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// 解析命令列參數
function parseArgs() {
  const args = process.argv.slice(2);
  const options = {
    scenario: process.env.SCENARIO || 'login-consent',
    output: process.env.OUTPUT_PATH || null,
  };

  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (arg === '--scenario' || arg === '-s') {
      options.scenario = args[++i];
    } else if (arg === '--output' || arg === '-o') {
      options.output = args[++i];
    } else if (arg === '--help' || arg === '-h') {
      console.log(`
Usage: node record-scenario.js [options]

Options:
  --scenario, -s <name>   Scenario name to record (available: ${listScenarios().join(', ')}) (default: login-consent)
  --output, -o <path>     Target output .webm file path (default: ./output/<scenario>-<timestamp>.webm)
  --help, -h              Show this help message
      `);
      process.exit(0);
    }
  }

  return options;
}

async function main() {
  const options = parseArgs();
  const scenarioFn = getScenario(options.scenario);

  if (!scenarioFn) {
    console.error(`[ERROR] 未知的場景: "${options.scenario}"`);
    console.error(`可用場景: ${listScenarios().join(', ')}`);
    process.exit(1);
  }

  const defaultOutputDir = path.join(__dirname, 'output');
  if (!fs.existsSync(defaultOutputDir)) {
    fs.mkdirSync(defaultOutputDir, { recursive: true });
  }

  // 暫存錄影目錄給 Playwright
  const tempVideoDir = path.join(defaultOutputDir, '.temp_video');
  if (!fs.existsSync(tempVideoDir)) {
    fs.mkdirSync(tempVideoDir, { recursive: true });
  }

  const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
  const finalOutputPath = options.output 
    ? path.resolve(process.cwd(), options.output)
    : path.join(defaultOutputDir, `${options.scenario}-${timestamp}.webm`);

  // 確保目標輸出檔案的目錄存在
  const targetDir = path.dirname(finalOutputPath);
  if (!fs.existsSync(targetDir)) {
    fs.mkdirSync(targetDir, { recursive: true });
  }

  console.log(`[INIT] 啟動錄影腳本...`);
  console.log(`[INIT] 目標場景: ${options.scenario}`);
  console.log(`[INIT] 目標輸出檔案: ${finalOutputPath}`);

  const browser = await chromium.launch({
    headless: true,
  });

  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 1280, height: 720 },
    recordVideo: {
      dir: tempVideoDir,
      size: { width: 1280, height: 720 },
    },
  });

  const page = await context.newPage();

  let scenarioError = null;
  try {
    await scenarioFn(page, options);
  } catch (err) {
    scenarioError = err;
    console.error(`\n[FATAL] 場景執行中發生錯誤:`, err.message);
  } finally {
    // 取得 video 物件參照
    const video = page.video();

    // 關閉頁面與 context，使 Playwright 寫入完整的影片檔
    await page.close().catch(() => {});
    await context.close().catch(() => {});
    await browser.close().catch(() => {});

    if (scenarioError) {
      // 若執行失敗，嘗試清理暫存
      if (video) {
        const videoPath = await video.path().catch(() => null);
        if (videoPath && fs.existsSync(videoPath)) {
          fs.unlinkSync(videoPath);
        }
      }
      console.error(`[ABORT] 錄影腳本終止（非 0 結束碼）。`);
      process.exit(1);
    }

    // 取得產生的影片檔案並搬移到最終輸出路徑
    if (video) {
      try {
        const generatedVideoPath = await video.path();
        if (generatedVideoPath && fs.existsSync(generatedVideoPath)) {
          // 若目標檔案已存在則刪除
          if (fs.existsSync(finalOutputPath)) {
            fs.unlinkSync(finalOutputPath);
          }
          fs.renameSync(generatedVideoPath, finalOutputPath);

          const stats = fs.statSync(finalOutputPath);
          console.log(`\n========================================`);
          console.log(`[SUCCESS] 錄影成功產出！`);
          console.log(`影片路徑: ${finalOutputPath}`);
          console.log(`檔案大小: ${stats.size} bytes (${(stats.size / 1024).toFixed(2)} KB)`);
          console.log(`========================================\n`);
        } else {
          console.error(`[ERROR] 找不到 Playwright 產生的影片檔案。`);
          process.exit(1);
        }
      } catch (videoErr) {
        console.error(`[ERROR] 處理影片檔案時發生錯誤:`, videoErr.message);
        process.exit(1);
      } finally {
        // 清理 tempVideoDir 若為空
        try {
          if (fs.existsSync(tempVideoDir) && fs.readdirSync(tempVideoDir).length === 0) {
            fs.rmdirSync(tempVideoDir);
          }
        } catch (_) {}
      }
    } else {
      console.error(`[ERROR] 未能取得錄影物件。`);
      process.exit(1);
    }
  }
}

main().catch((err) => {
  console.error(`[FATAL] 未攔截的異常:`, err);
  process.exit(1);
});
