<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()

const errorCode = computed(() => {
  const value = route.query.code ?? route.query.error
  return typeof value === 'string' && value.length > 0 ? value : 'unknown_error'
})

const errorDescription = computed(() => {
  const value = route.query.error_description
  return typeof value === 'string' ? value : null
})
</script>

<template>
  <div class="card">
    <h1>發生錯誤</h1>
    <p class="error-message" data-testid="error-code">{{ errorCode }}</p>
    <p v-if="errorDescription">{{ errorDescription }}</p>
    <p class="links">
      <RouterLink to="/login">回到登入頁</RouterLink>
    </p>
  </div>
</template>
