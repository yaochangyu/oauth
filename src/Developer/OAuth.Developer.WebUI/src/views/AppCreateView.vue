<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { developerApi, type AppResponse } from '../api/developer'

const router = useRouter()

const displayName = ref('')
const appType = ref<'Web' | 'SPA' | 'Mobile'>('Web')
const description = ref('')
const logoUrl = ref('')
const redirectUrisInput = ref('https://localhost:5001/callback')
const postLogoutRedirectUrisInput = ref('https://localhost:5001/logout-callback')
const requestedScopesInput = ref('openid, profile, email')

const loading = ref(false)
const error = ref('')

// Secret created modal
const showSecretModal = ref(false)
const createdApp = ref<AppResponse | null>(null)
const secretCopied = ref(false)

const handleCreate = async () => {
  loading.value = true
  error.value = ''

  const redirectUris = redirectUrisInput.value
    .split('\n')
    .map(s => s.trim())
    .filter(Boolean)

  const postLogoutRedirectUris = postLogoutRedirectUrisInput.value
    .split('\n')
    .map(s => s.trim())
    .filter(Boolean)

  const requestedScopes = requestedScopesInput.value
    .split(',')
    .map(s => s.trim())
    .filter(Boolean)

  try {
    const res = await developerApi.createApp({
      displayName: displayName.value,
      appType: appType.value,
      description: description.value,
      logoUrl: logoUrl.value,
      redirectUris,
      postLogoutRedirectUris,
      requestedScopes,
    })

    createdApp.value = res
    if (res.clientSecret) {
      showSecretModal.value = true
    } else {
      router.push(`/apps/${res.id}`)
    }
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const copySecret = () => {
  if (createdApp.value?.clientSecret) {
    navigator.clipboard.writeText(createdApp.value.clientSecret)
    secretCopied.value = true
    setTimeout(() => {
      secretCopied.value = false
    }, 2000)
  }
}

const closeSecretModal = () => {
  showSecretModal.value = false
  if (createdApp.value) {
    router.push(`/apps/${createdApp.value.id}`)
  }
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">建立新應用程式</h1>
    </div>

    <div v-if="error" class="alert alert-warning">
      {{ error }}
    </div>

    <div class="card">
      <form @submit.prevent="handleCreate">
        <div class="form-group">
          <label class="form-label">應用程式名稱 *</label>
          <input
            v-model="displayName"
            type="text"
            class="form-input"
            placeholder="例如：My SaaS Integration"
            required
          />
        </div>

        <div class="form-group">
          <label class="form-label">應用程式架構類型 (App Type) *</label>
          <select v-model="appType" class="form-select">
            <option value="Web">機密型 Web 伺服器應用程式 (Confidential Client - 具備 Client Secret)</option>
            <option value="SPA">單頁式前端應用程式 (SPA - Public Client)</option>
            <option value="Mobile">行動原生應用程式 (Mobile / Native - Public Client)</option>
          </select>
          <p class="form-hint" style="color: #6b21a8; font-weight: 500;">
            🛡️ 依據 RFC 9700 OAuth 2.0 Security BCP，本平台所有客戶端（包含機密型 Web）均強制要求 PKCE 防護。
          </p>
        </div>

        <div class="form-group">
          <label class="form-label">應用程式說明</label>
          <textarea
            v-model="description"
            class="form-textarea"
            rows="3"
            placeholder="簡要說明此應用程式的用途與串接功能"
          ></textarea>
        </div>

        <div class="form-group">
          <label class="form-label">授權回呼網址 (Redirect URIs) *</label>
          <textarea
            v-model="redirectUrisInput"
            class="form-textarea"
            rows="3"
            placeholder="每行輸入一個絕對網址，例如：https://app.example.com/oauth/callback"
            required
          ></textarea>
          <p class="form-hint">授權碼發放後將重新導向回這些經註冊的白名單網址。</p>
        </div>

        <div class="form-group">
          <label class="form-label">登出回呼網址 (Post-Logout Redirect URIs)</label>
          <textarea
            v-model="postLogoutRedirectUrisInput"
            class="form-textarea"
            rows="2"
            placeholder="每行輸入一個網址，例如：https://app.example.com/logout-callback"
          ></textarea>
        </div>

        <div class="form-group">
          <label class="form-label">請求權限範圍 (Scopes)</label>
          <input
            v-model="requestedScopesInput"
            type="text"
            class="form-input"
            placeholder="openid, profile, email"
          />
          <p class="form-hint">以逗號分隔，如：openid, profile, email, api</p>
        </div>

        <div style="display: flex; gap: 1rem; margin-top: 1.5rem;">
          <button type="submit" class="btn btn-primary" :disabled="loading">
            {{ loading ? '建立中...' : '確認建立應用程式' }}
          </button>
          <router-link to="/apps" class="btn btn-secondary">
            取消
          </router-link>
        </div>
      </form>
    </div>

    <!-- Secret Display Modal -->
    <div v-if="showSecretModal" class="modal-overlay">
      <div class="modal-content">
        <h2 style="font-size: 1.25rem; font-weight: 700; margin-bottom: 0.75rem; color: #1e293b;">
          🔑 應用程式金鑰已生成
        </h2>
        <div class="alert alert-warning" style="margin-bottom: 1rem; font-size: 0.85rem;">
          ⚠️ <strong>請立即複製並妥善保存！</strong> 基於安全考量，Client Secret 明文僅於建立時顯示一次，未來將無法再次檢視。
        </div>

        <div style="margin-bottom: 1rem;">
          <label class="form-label">Client ID</label>
          <div class="code-box">
            {{ createdApp?.clientId }}
          </div>
        </div>

        <div style="margin-bottom: 1.5rem;">
          <label class="form-label">Client Secret (僅顯示一次)</label>
          <div class="code-box" style="color: #4ade80;">
            <span>{{ createdApp?.clientSecret }}</span>
            <button @click="copySecret" class="btn btn-secondary btn-sm" style="margin-left: 0.5rem;">
              {{ secretCopied ? '已複製！' : '複製' }}
            </button>
          </div>
        </div>

        <div style="text-align: right;">
          <button @click="closeSecretModal" class="btn btn-primary">
            我已妥善保存，進入 App 詳細設定
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
