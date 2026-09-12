<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { handleCallback } from '../auth/oidc'

const router = useRouter()
const error = ref('')

onMounted(async () => {
  try {
    await handleCallback()
    router.replace('/')
  } catch (err: any) {
    console.error('OIDC callback error:', err)
    error.value = err.message || '登入回傳驗證失敗，請重試。'
  }
})
</script>

<template>
  <div style="display: flex; justify-content: center; align-items: center; min-height: 50vh;">
    <div class="card" style="max-width: 480px; width: 100%; text-align: center; padding: 2.5rem;">
      <div v-if="error">
        <div style="font-size: 2.5rem; margin-bottom: 1rem;">⚠️</div>
        <h2 style="font-size: 1.25rem; font-weight: 600; color: var(--danger); margin-bottom: 0.5rem;">
          登入驗證失敗
        </h2>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-bottom: 1.5rem;">
          {{ error }}
        </p>
        <router-link to="/" class="btn btn-primary">返回首頁</router-link>
      </div>
      <div v-else>
        <div style="font-size: 2.5rem; margin-bottom: 1rem;">🔄</div>
        <h2 style="font-size: 1.25rem; font-weight: 600; margin-bottom: 0.5rem;">正在處理登入驗證...</h2>
        <p style="color: var(--text-muted); font-size: 0.875rem;">請稍候，即將為您跳轉至會員中心。</p>
      </div>
    </div>
  </div>
</template>
