<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { developerApi, type AppResponse } from '../api/developer'

const route = useRoute()
const router = useRouter()
const appId = route.params.id as string

const app = ref<AppResponse | null>(null)
const loading = ref(true)
const saving = ref(false)
const submittingReview = ref(false)
const message = ref('')
const error = ref('')

const displayName = ref('')
const description = ref('')
const logoUrl = ref('')
const redirectUrisInput = ref('')
const postLogoutRedirectUrisInput = ref('')
const requestedScopesInput = ref('')
const reviewNotes = ref('')

const fetchApp = async () => {
  loading.value = true
  error.value = ''
  try {
    const res = await developerApi.getApp(appId)
    app.value = res
    displayName.value = res.displayName
    description.value = res.description || ''
    logoUrl.value = res.logoUrl || ''
    redirectUrisInput.value = res.redirectUris.join('\n')
    postLogoutRedirectUrisInput.value = res.postLogoutRedirectUris.join('\n')
    requestedScopesInput.value = res.requestedScopes.join(', ')
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const handleUpdate = async () => {
  saving.value = true
  error.value = ''
  message.value = ''

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
    const res = await developerApi.updateApp(appId, {
      displayName: displayName.value,
      description: description.value,
      logoUrl: logoUrl.value,
      redirectUris,
      postLogoutRedirectUris,
      requestedScopes,
    })
    app.value = res
    message.value = '應用程式設定已更新成功！'
  } catch (err: any) {
    error.value = err.message
  } finally {
    saving.value = false
  }
}

const handleSubmitReview = async () => {
  if (!confirm('確定要將此應用程式提交上線審查嗎？審查期間仍可於沙盒環境測試。'))
    return

  submittingReview.value = true
  error.value = ''
  message.value = ''
  try {
    const res = await developerApi.submitReview(appId, reviewNotes.value)
    if (app.value) {
      app.value.status = res.status as any
      app.value.reviewSubmittedAt = res.submittedAt
    }
    message.value = '已成功提交上線審查！審核小組將於 1-2 個工作日內完成審批。'
  } catch (err: any) {
    error.value = err.message
  } finally {
    submittingReview.value = false
  }
}

const handleDelete = async () => {
  if (!confirm('警告：確定要刪除此應用程式嗎？此動作無法復原！'))
    return

  try {
    await developerApi.deleteApp(appId)
    router.push('/apps')
  } catch (err: any) {
    error.value = err.message
  }
}

onMounted(fetchApp)
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <router-link to="/apps" style="color: var(--primary); text-decoration: none; font-size: 0.875rem;">
          ← 返回應用程式清單
        </router-link>
        <h1 class="page-title" style="margin-top: 0.5rem;">{{ app?.displayName || '載入中...' }}</h1>
      </div>
      <div v-if="app" style="display: flex; gap: 0.75rem;">
        <router-link v-if="app.clientType === 'confidential'" :to="`/apps/${app.id}/keys`" class="btn btn-primary">
          🔑 金鑰輪替管理
        </router-link>
        <button @click="handleDelete" class="btn btn-danger">
          刪除 App
        </button>
      </div>
    </div>

    <div v-if="error" class="alert alert-warning">
      {{ error }}
    </div>

    <div v-if="message" class="alert alert-info">
      {{ message }}
    </div>

    <div v-if="loading" style="text-align: center; padding: 3rem;">
      載入中...
    </div>

    <div v-else-if="app">
      <!-- Status Card -->
      <div class="card" style="display: flex; justify-content: space-between; align-items: center; background-color: #f8fafc;">
        <div>
          <div style="font-size: 0.875rem; color: var(--text-muted);">目前生命週期狀態</div>
          <div style="margin-top: 0.25rem; display: flex; align-items: center; gap: 0.75rem;">
            <span class="badge" :class="`badge-${app.status.toLowerCase()}`" style="font-size: 0.9rem; padding: 0.35rem 0.75rem;">
              {{ app.status }}
            </span>
            <span class="badge badge-pkce">全類型強制 PKCE 防護</span>
          </div>
        </div>

        <div v-if="app.status === 'Sandbox'" style="display: flex; align-items: center; gap: 0.5rem;">
          <input
            v-model="reviewNotes"
            type="text"
            class="form-input"
            placeholder="填寫審查備註或上線說明..."
            style="width: 260px;"
          />
          <button @click="handleSubmitReview" class="btn btn-primary" :disabled="submittingReview">
            {{ submittingReview ? '提交中...' : '提交上線審查' }}
          </button>
        </div>
        <div v-else-if="app.status === 'InReview'" style="color: #92400e; font-size: 0.875rem;">
          ⏳ 審核中（提交時間：{{ app.reviewSubmittedAt ? new Date(app.reviewSubmittedAt).toLocaleString() : '最近' }}）
        </div>
      </div>

      <!-- App Metadata Info -->
      <div class="card">
        <h2 style="font-size: 1.15rem; font-weight: 600; margin-bottom: 1rem;">基本識別資訊</h2>
        <div class="form-group">
          <label class="form-label">Client ID (客戶端識別碼)</label>
          <div class="code-box">
            {{ app.clientId }}
          </div>
        </div>
        <div class="form-group">
          <label class="form-label">客戶端類型 (Client Type)</label>
          <div style="font-size: 0.875rem; color: var(--text-main);">
            {{ app.appType }} — <strong>{{ app.clientType }}</strong>
          </div>
        </div>
      </div>

      <!-- App Form Settings -->
      <div class="card">
        <h2 style="font-size: 1.15rem; font-weight: 600; margin-bottom: 1rem;">應用程式設定修改</h2>
        <form @submit.prevent="handleUpdate">
          <div class="form-group">
            <label class="form-label">應用程式名稱</label>
            <input v-model="displayName" type="text" class="form-input" required />
          </div>

          <div class="form-group">
            <label class="form-label">說明</label>
            <textarea v-model="description" class="form-textarea" rows="3"></textarea>
          </div>

          <div class="form-group">
            <label class="form-label">Logo 圖片網址 (可選)</label>
            <input v-model="logoUrl" type="url" class="form-input" placeholder="https://..." />
          </div>

          <div class="form-group">
            <label class="form-label">授權回呼網址 (Redirect URIs)</label>
            <textarea v-model="redirectUrisInput" class="form-textarea" rows="3" required></textarea>
            <p class="form-hint">每行輸入一個有效 URI。</p>
          </div>

          <div class="form-group">
            <label class="form-label">登出回呼網址 (Post-Logout Redirect URIs)</label>
            <textarea v-model="postLogoutRedirectUrisInput" class="form-textarea" rows="2"></textarea>
          </div>

          <div class="form-group">
            <label class="form-label">請求權限範圍 (Scopes)</label>
            <input v-model="requestedScopesInput" type="text" class="form-input" />
            <p class="form-hint">以逗號分隔，例如：openid, profile, email</p>
          </div>

          <div style="margin-top: 1.5rem;">
            <button type="submit" class="btn btn-primary" :disabled="saving">
              {{ saving ? '儲存中...' : '儲存變更' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
