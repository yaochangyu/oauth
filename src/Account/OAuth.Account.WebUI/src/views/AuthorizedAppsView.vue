<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { consentsApi, type AuthorizedApp } from '../api/consents'

const apps = ref<AuthorizedApp[]>([])
const loading = ref(true)
const error = ref('')
const revokingId = ref<string | null>(null)
const successMsg = ref('')

const scopeLabels: Record<string, string> = {
  openid: '基本身份 (openid)',
  profile: '個人資料 (profile)',
  email: '電子信箱 (email)',
  offline_access: '離線存取 / Refresh Token (offline_access)',
  roles: '使用者角色 (roles)',
}

const formatScope = (scope: string) => {
  return scopeLabels[scope] || scope
}

const formatDate = (isoString: string) => {
  try {
    const d = new Date(isoString)
    return d.toLocaleString('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    })
  } catch {
    return isoString
  }
}

const fetchAuthorizedApps = async () => {
  loading.value = true
  error.value = ''
  try {
    apps.value = await consentsApi.getAuthorizedApps()
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const handleRevoke = async (app: AuthorizedApp) => {
  const confirmed = confirm(
    `確定要解除「${app.clientDisplayName || app.clientId}」的存取授權嗎？\n\n解除後，該應用程式將無法再存取您的帳號，且現有的連線將立即中斷。`
  )
  if (!confirmed) return

  revokingId.value = app.authorizationId
  error.value = ''
  successMsg.value = ''
  try {
    await consentsApi.revokeAppConsent(app.authorizationId)
    apps.value = apps.value.filter((a) => a.authorizationId !== app.authorizationId)
    successMsg.value = `已成功解除「${app.clientDisplayName || app.clientId}」的授權。`
  } catch (err: any) {
    error.value = err.message
  } finally {
    revokingId.value = null
  }
}

onMounted(() => {
  fetchAuthorizedApps()
})
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">已授權應用程式</h1>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-top: 0.25rem;">
          管理擁有您帳號存取權限的第三方應用程式，隨時可撤銷授權與作廢連線金鑰。
        </p>
      </div>
      <button @click="fetchAuthorizedApps" class="btn btn-outline" style="font-size: 0.875rem;">
        重新整理
      </button>
    </div>

    <div v-if="error" class="alert alert-danger">{{ error }}</div>
    <div v-if="successMsg" class="alert alert-success">{{ successMsg }}</div>

    <div v-if="loading" class="card" style="text-align: center; padding: 3rem; color: var(--text-muted);">
      載入已授權應用程式清單中...
    </div>

    <div v-else-if="apps.length === 0" class="card" style="text-align: center; padding: 3rem; color: var(--text-muted);">
      <div style="font-size: 2.5rem; margin-bottom: 0.5rem;">🔒</div>
      <h3 style="font-size: 1.125rem; font-weight: 600; color: var(--text-main); margin-bottom: 0.25rem;">目前沒有任何已授權的應用程式</h3>
      <p style="font-size: 0.875rem;">當您使用此帳號登入第三方應用程式時，授權紀錄將會顯示在此處。</p>
    </div>

    <div v-else style="display: grid; gap: 1rem;">
      <div
        v-for="app in apps"
        :key="app.authorizationId"
        class="card"
        style="display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem;"
      >
        <div style="flex: 1;">
          <div style="display: flex; align-items: center; gap: 0.75rem;">
            <h3 style="font-size: 1.125rem; font-weight: 600;">
              {{ app.clientDisplayName || app.clientId }}
            </h3>
            <span class="badge badge-muted" style="font-family: monospace;">{{ app.clientId }}</span>
          </div>

          <p style="font-size: 0.8125rem; color: var(--text-muted); margin-top: 0.375rem;">
            授權時間：{{ formatDate(app.authorizedAt) }}
          </p>

          <div style="margin-top: 0.875rem;">
            <span style="font-size: 0.8125rem; color: var(--text-muted); display: block; margin-bottom: 0.375rem;">
              授予之存取權限 (Scopes)：
            </span>
            <div style="display: flex; flex-wrap: wrap; gap: 0.375rem;">
              <span
                v-for="scope in app.scopes"
                :key="scope"
                class="badge badge-primary"
                style="font-size: 0.75rem;"
              >
                {{ formatScope(scope) }}
              </span>
            </div>
          </div>
        </div>

        <button
          @click="handleRevoke(app)"
          :disabled="revokingId === app.authorizationId"
          class="btn btn-outline"
          style="color: var(--danger); border-color: var(--danger); white-space: nowrap;"
        >
          {{ revokingId === app.authorizationId ? '解除中...' : '解除授權' }}
        </button>
      </div>
    </div>
  </div>
</template>
