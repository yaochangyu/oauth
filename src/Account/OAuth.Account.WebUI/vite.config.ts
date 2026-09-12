import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import path from 'path'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5500,
    proxy: {
      '/api': {
        target: 'https://localhost:7004',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: '../OAuth.Account.WebAPI/wwwroot',
    emptyOutDir: true,
  },
})
