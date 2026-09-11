<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { getConsentInfo, acceptConsent, denyConsent, type ApiError, type ConsentInfo } from '../api/client'

const route = useRoute()
const returnUrl = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : ''

const info = ref<ConsentInfo | null>(null)
const errorMessage = ref<string | null>(null)
const submitting = ref(false)

onMounted(async () => {
  if (!returnUrl) {
    errorMessage.value = '缺少 returnUrl 參數'
    return
  }
  try {
    info.value = await getConsentInfo(returnUrl)
  } catch (e) {
    const err = e as ApiError
    errorMessage.value = err.message ?? '無法取得授權請求資訊'
  }
})

async function onAccept() {
  if (!info.value) return
  submitting.value = true
  try {
    const result = await acceptConsent({ returnUrl: info.value.returnUrl, clientId: info.value.clientId })
    window.location.href = result.redirectUrl
  } catch (e) {
    const err = e as ApiError
    errorMessage.value = err.message ?? '同意授權失敗'
  } finally {
    submitting.value = false
  }
}

async function onDeny() {
  if (!info.value) return
  submitting.value = true
  try {
    const result = await denyConsent({ returnUrl: info.value.returnUrl, clientId: info.value.clientId })
    window.location.href = result.redirectUrl
  } catch (e) {
    const err = e as ApiError
    errorMessage.value = err.message ?? '拒絕授權失敗'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="card">
    <h1>授權請求</h1>

    <p v-if="errorMessage" class="error-message" data-testid="error-message">{{ errorMessage }}</p>

    <template v-if="info">
      <p><strong>{{ info.clientDisplayName }}</strong> 請求存取以下資源：</p>

      <ul class="scope-list" data-testid="scope-list">
        <li v-for="scope in info.scopes" :key="scope">{{ scope }}</li>
      </ul>

      <button type="button" value="accept" :disabled="submitting" @click="onAccept">同意</button>
      <button type="button" value="deny" :disabled="submitting" @click="onDeny">拒絕</button>
    </template>
  </div>
</template>
