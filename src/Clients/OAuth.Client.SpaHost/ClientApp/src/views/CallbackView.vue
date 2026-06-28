<template>
  <div class="page">
    <p v-if="loading">正在處理登入回呼...</p>
    <p v-if="error" class="error">{{ error }}</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { handleCallback } from '../auth/oidc'

const router = useRouter()
const loading = ref(true)
const error = ref<string | null>(null)

onMounted(async () => {
  try {
    await handleCallback()
    router.push('/profile')
  } catch (e) {
    error.value = String(e)
    loading.value = false
  }
})
</script>
