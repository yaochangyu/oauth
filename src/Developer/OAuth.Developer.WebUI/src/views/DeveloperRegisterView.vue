<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { developerApi, type DeveloperStatusResponse } from '../api/developer'

const status = ref<DeveloperStatusResponse | null>(null)
const organizationName = ref('')
const contactEmail = ref('')
const acceptAgreement = ref(false)
const loading = ref(false)
const message = ref('')
const error = ref('')

const fetchStatus = async () => {
  try {
    status.value = await developerApi.getDeveloperStatus()
    if (status.value) {
      organizationName.value = status.value.organizationName || ''
      contactEmail.value = status.value.contactEmail || ''
    }
  } catch (err: any) {
    error.value = err.message
  }
}

const handleEnable = async () => {
  if (!acceptAgreement.value) {
    error.value = '請先勾選同意開發者服務協議'
    return
  }

  loading.value = true
  error.value = ''
  message.value = ''

  try {
    status.value = await developerApi.enableDeveloper({
      organizationName: organizationName.value,
      contactEmail: contactEmail.value,
      acceptAgreement: acceptAgreement.value,
    })
    message.value = '開發者身分已成功啟用／更新！'
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

onMounted(fetchStatus)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">開發者帳號與身分啟用</h1>
    </div>

    <div v-if="error" class="alert alert-warning">
      {{ error }}
    </div>

    <div v-if="message" class="alert alert-info">
      {{ message }}
    </div>

    <div class="card">
      <h2 style="font-size: 1.25rem; margin-bottom: 1rem;">開發者資料登記</h2>
      
      <div v-if="status?.isDeveloperEnabled" style="margin-bottom: 1.5rem;">
        <span class="badge badge-approved" style="font-size: 0.875rem; padding: 0.35rem 0.75rem;">
          ✓ 已具備第三方開發者權限
        </span>
      </div>

      <form @submit.prevent="handleEnable">
        <div class="form-group">
          <label class="form-label">組織／公司／個人名稱</label>
          <input
            v-model="organizationName"
            type="text"
            class="form-input"
            placeholder="例如：Acme Innovations Ltd."
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">技術聯絡電子郵件</label>
          <input
            v-model="contactEmail"
            type="email"
            class="form-input"
            placeholder="developer-support@acme.com"
            required
          />
          <p class="form-hint">此信箱將用於接收 API 重大變更通知與上線審核結果。</p>
        </div>

        <div class="form-group" style="margin-top: 1.5rem;">
          <label style="display: flex; align-items: center; gap: 0.5rem; cursor: pointer; font-size: 0.875rem;">
            <input type="checkbox" v-model="acceptAgreement" />
            我同意遵守 OAuth 開發者服務條款與隱私安全規範 (全類型客戶端強制要求 PKCE)
          </label>
        </div>

        <div style="margin-top: 1.5rem;">
          <button type="submit" class="btn btn-primary" :disabled="loading">
            {{ status?.isDeveloperEnabled ? '更新開發者資料' : '立即啟用開發者身分' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
