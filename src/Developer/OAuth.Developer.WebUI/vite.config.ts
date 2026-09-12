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
    port: 5400,
    proxy: {
      '/api': {
        target: 'https://localhost:7003',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: '../OAuth.Developer.WebAPI/wwwroot',
    emptyOutDir: true,
  },
})
