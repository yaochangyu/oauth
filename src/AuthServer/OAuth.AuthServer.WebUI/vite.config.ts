import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// AuthServer Headless 認證站（/login、/consent、/register、/error）。
// 開發模式：Vite dev server 反向代理 /api、/connect 到 AuthServer（https://localhost:7001），
// 讓 Cookie 落在同一個瀏覽器工作階段內，避免跨來源 Cookie 問題。
// 正式建置：輸出到 ../OAuth.AuthServer.WebAPI/wwwroot，由 AuthServer 本身以靜態檔案託管（同源）。
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5300,
    proxy: {
      '/api': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false,
      },
      '/connect': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: '../OAuth.AuthServer.WebAPI/wwwroot',
    emptyOutDir: true,
  },
})
