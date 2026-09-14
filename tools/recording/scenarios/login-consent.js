/**
 * Scenario: login-consent
 * 
 * 流程：
 * 1. 註冊全新隨機測試帳號 (AuthServer)
 * 2. 存取 MVC Client 受保護頁面 (/Profile)，觸發 OIDC 授權跳轉
 * 3. 於 AuthServer 登入頁輸入帳密登入
 * 4. 於 AuthServer 授權同意頁點擊「同意」
 * 5. 導回 MVC Client 個人資料頁，確認登入授權成功
 */

import { fileURLToPath } from 'url';

/**
 * 執行 login-consent 場景錄影
 * @param {import('playwright').Page} page
 * @param {object} options
 */
export async function runLoginConsent(page, options = {}) {
  const authServerUrl = process.env.AUTH_SERVER_URL || 'https://localhost:7001';
  const mvcClientUrl = process.env.MVC_CLIENT_URL || 'https://localhost:5101';
  
  const randomSuffix = `${Date.now()}_${Math.random().toString(36).substring(2, 7)}`;
  const email = `demo_${randomSuffix}@example.com`;
  const password = 'DemoPassword123';

  console.log(`\n=== 開始執行場景: login-consent ===`);
  console.log(`AuthServer: ${authServerUrl}`);
  console.log(`MVC Client: ${mvcClientUrl}`);
  console.log(`測試帳號: ${email}\n`);

  // [1/5] 註冊帳號
  console.log(`[1/5] 註冊帳號 ${email}...`);
  try {
    await page.goto(`${authServerUrl}/register`, { waitUntil: 'networkidle' });
    await page.waitForSelector("input[name='email']", { timeout: 15000 });
    await page.fill("input[name='email']", email);
    await page.waitForTimeout(300);
    await page.fill("input[name='password']", password);
    await page.waitForTimeout(300);
    await page.click("button[type='submit']");
    
    // 等待註冊完成或跳轉
    await page.waitForTimeout(1000);
    console.log(`[1/5] 註冊帳號完成。`);
  } catch (err) {
    throw new Error(`[1/5] 註冊帳號失敗: ${err.message}`);
  }

  // [2/5] 前往 MVC Client 受保護頁面
  console.log(`[2/5] 前往 MVC Client 受保護頁面 ${mvcClientUrl}/Profile...`);
  try {
    await page.goto(`${mvcClientUrl}/Profile`, { waitUntil: 'domcontentloaded' });
    // 會觸發 Challenge 並自動重定向至 AuthServer /login 或 /connect/authorize
    await page.waitForTimeout(1000);
    console.log(`[2/5] 已發起存取受保護頁面，目前 URL: ${page.url()}`);
  } catch (err) {
    throw new Error(`[2/5] 前往 MVC Client 失敗: ${err.message}`);
  }

  // [3/5] 於 AuthServer 登入頁登入
  console.log(`[3/5] 等待 AuthServer 登入頁並填寫帳密...`);
  try {
    await page.waitForSelector("input[name='userName']", { timeout: 15000 });
    await page.fill("input[name='userName']", email);
    await page.waitForTimeout(300);
    await page.fill("input[type='password']", password);
    await page.waitForTimeout(300);
    await page.click("button[type='submit']");
    console.log(`[3/5] 登入表單已送出。`);
  } catch (err) {
    throw new Error(`[3/5] 登入失敗: ${err.message}`);
  }

  // [4/5] 授權同意頁面處理
  console.log(`[4/5] 等待授權同意頁面出現...`);
  try {
    // 等待 consent 頁面或直接回到 profile
    const consentButtonSelector = "button[value='accept']";
    const profileHeaderSelector = "h1:has-text('個人資料')";

    // 使用 Promise.race 偵測出現的是 consent 還是 profile
    const outcome = await Promise.race([
      page.waitForSelector(consentButtonSelector, { timeout: 15000 }).then(() => 'consent'),
      page.waitForSelector(profileHeaderSelector, { timeout: 15000 }).then(() => 'skipped_consent')
    ]);

    if (outcome === 'skipped_consent') {
      console.warn(`[WARNING] [4/5] 警告：未出現授權同意頁面直接跳回了個人資料頁！請確認使用者是否曾經授權過或 Consent 未觸發。`);
      throw new Error(`[4/5] 異常：未偵測到同意頁面 (Consent page skipped)`);
    }

    console.log(`[4/5] 同意頁面已載入 (URL: ${page.url()})，點擊「同意」...`);
    await page.waitForTimeout(500);
    await page.click(consentButtonSelector);
    console.log(`[4/5] 已點擊同意授權。`);
  } catch (err) {
    throw new Error(`[4/5] 同意頁面處理失敗: ${err.message}`);
  }

  // [5/5] 確認導回 MVC Client 個人資料頁
  console.log(`[5/5] 等待導回 MVC Client 並確認個人資料頁面...`);
  try {
    await page.waitForSelector("h1:has-text('個人資料')", { timeout: 15000 });
    console.log(`[5/5] 成功到達 MVC Client 個人資料頁面 (URL: ${page.url()})！`);
    
    // 額外停留 2 秒讓錄製畫面穩定呈現最終狀態
    await page.waitForTimeout(2000);
  } catch (err) {
    throw new Error(`[5/5] 確認個人資料頁失敗: ${err.message}`);
  }

  console.log(`\n=== 場景 login-consent 執行完成 ===\n`);
}
