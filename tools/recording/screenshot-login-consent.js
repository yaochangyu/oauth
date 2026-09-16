#!/usr/bin/env node
/**
 * ============================================================================
 * Screenshot Capture Script — login-consent scenario
 * ============================================================================
 *
 * 與 record-scenario.js 錄影腳本走同一套操作步驟，但改成在四個關鍵畫面各拍一張
 * PNG 截圖，用來搭配說明文件（doc/guides/）附圖。
 *
 * 用法:
 *   node screenshot-login-consent.js
 *
 * 環境變數:
 *   AUTH_SERVER_URL   AuthServer 網址 (預設: https://localhost:7001)
 *   MVC_CLIENT_URL    MVC Client 網址 (預設: https://localhost:5101)
 *   OUTPUT_DIR        輸出目錄 (預設: ./output/screenshots)
 * ============================================================================
 */

import { chromium } from 'playwright';
import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';
import { attachUrlBar, refreshUrlBar } from './lib/url-bar.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

async function main() {
  const authServerUrl = process.env.AUTH_SERVER_URL || 'https://localhost:7001';
  const mvcClientUrl = process.env.MVC_CLIENT_URL || 'https://localhost:5101';
  const outputDir = process.env.OUTPUT_DIR || path.join(__dirname, 'output', 'screenshots');

  if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
  }

  const randomSuffix = `${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
  const email = `demo_${randomSuffix}@example.com`;
  const password = 'DemoPassword123';

  console.log(`測試帳號: ${email}`);

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    ignoreHTTPSErrors: true,
    viewport: { width: 1280, height: 800 },
  });
  const page = await context.newPage();
  await attachUrlBar(page);

  try {
    // 01: 註冊頁
    console.log('[1/4] 註冊頁...');
    await page.goto(`${authServerUrl}/register`, { waitUntil: 'networkidle' });
    await page.waitForSelector("input[name='email']", { timeout: 15000 });
    await refreshUrlBar(page);
    console.log(`      URL: ${page.url()}`);
    await page.screenshot({ path: path.join(outputDir, '01-register.png') });

    await page.fill("input[name='email']", email);
    await page.fill("input[name='password']", password);
    await page.click("button[type='submit']");
    await page.waitForTimeout(1000);

    // 02: 登入頁
    console.log('[2/4] 登入頁...');
    await page.goto(`${mvcClientUrl}/Profile`, { waitUntil: 'domcontentloaded' });
    await page.waitForSelector("input[name='userName']", { timeout: 15000 });
    await refreshUrlBar(page);
    console.log(`      URL: ${page.url()}`);
    await page.screenshot({ path: path.join(outputDir, '02-login.png') });

    await page.fill("input[name='userName']", email);
    await page.fill("input[type='password']", password);
    await page.click("button[type='submit']");

    // 03: 同意頁
    console.log('[3/4] 同意頁...');
    await page.waitForSelector("button[value='accept']", { timeout: 15000 });
    await refreshUrlBar(page);
    console.log(`      URL: ${page.url()}`);
    await page.screenshot({ path: path.join(outputDir, '03-consent.png') });
    await page.click("button[value='accept']");

    // 04: MVC Client 個人資料頁（已登入）
    console.log('[4/4] 個人資料頁...');
    await page.waitForSelector("h1:has-text('個人資料')", { timeout: 15000 });
    await page.waitForTimeout(500);
    await refreshUrlBar(page);
    console.log(`      URL: ${page.url()}`);
    await page.screenshot({ path: path.join(outputDir, '04-profile.png') });

    console.log(`\n[SUCCESS] 四張截圖已輸出至 ${outputDir}`);
  } finally {
    await context.close();
    await browser.close();
  }
}

main().catch((err) => {
  console.error('[FATAL]', err);
  process.exit(1);
});
