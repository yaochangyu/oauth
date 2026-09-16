/**
 * 在無頭瀏覽器畫面頂端注入一條模擬「網址列」的 overlay，讓截圖與錄影檔案本身
 * 就看得到目前頁面的 URL（headless Chromium 沒有真實瀏覽器外框可以錄）。
 */

const BAR_HEIGHT = 32;

async function injectBar(page) {
  await page.evaluate((barHeight) => {
    let bar = document.getElementById('__demo_url_bar__');
    if (!bar) {
      bar = document.createElement('div');
      bar.id = '__demo_url_bar__';
      Object.assign(bar.style, {
        position: 'fixed',
        top: '0',
        left: '0',
        right: '0',
        height: `${barHeight}px`,
        lineHeight: `${barHeight}px`,
        zIndex: '2147483647',
        background: '#202124',
        color: '#e8eaed',
        fontFamily: "Menlo, Consolas, 'Courier New', monospace",
        fontSize: '12px',
        padding: '0 10px',
        whiteSpace: 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
        borderBottom: '1px solid #444',
        boxShadow: '0 1px 3px rgba(0,0,0,0.4)',
      });
      document.documentElement.insertBefore(bar, document.body || null);
      if (document.body) {
        document.body.style.marginTop = `${barHeight}px`;
      }
    }
    bar.textContent = window.location.href;
  }, BAR_HEIGHT);
}

/**
 * 掛上網址列 overlay：初次注入 + 每次頁面完整載入後自動重新注入
 * （因為每次 navigation 都會重置 DOM）。
 * @param {import('playwright').Page} page
 */
export async function attachUrlBar(page) {
  page.on('load', () => {
    injectBar(page).catch(() => {});
  });
  await injectBar(page).catch(() => {});
}

/**
 * 在拍照/錄影前手動再注入一次，確保覆蓋到 SPA 內部路由跳轉等不會觸發
 * 'load' 事件的情況。
 * @param {import('playwright').Page} page
 */
export async function refreshUrlBar(page) {
  await injectBar(page).catch(() => {});
}
