<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { developerApi, type CredentialsResponse, type AppResponse } from '../api/developer'

const route = useRoute()
const appId = route.params.id as string

const app = ref<AppResponse | null>(null)
const credentials = ref<CredentialsResponse | null>(null)
const loading = ref(true)
const rotating = ref(false)
const revoking = ref(false)
const error = ref('')
const message = ref('')

// Rotation modal state
const showNewSecretModal = ref(false)
const newSecretValue = ref('')
const clientIdCopied = ref(false)
const newSecretCopied = ref(false)

// Countdown timer
const countdownDisplay = ref('')
let timerInterval: any = null

const fetchCredentials = async () => {
  loading.value = true
  error.value = ''
  try {
    const [appRes, credRes] = await Promise.all([
      developerApi.getApp(appId),
      developerApi.getCredentials(appId),
    ])
    app.value = appRes
    credentials.value = credRes
    updateCountdown()
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const updateCountdown = () => {
  if (!credentials.value?.hasRetiringSecret || !credentials.value?.retiringSecretExpiresAt) {
    countdownDisplay.value = ''
    return
  }

  const expireTime = new Date(credentials.value.retiringSecretExpiresAt).getTime()
  const now = Date.now()
  const diff = expireTime - now

  if (diff <= 0) {
    countdownDisplay.value = '已過期'
    return
  }

  const days = Math.floor(diff / (1000 * 60 * 60 * 24))
  const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
  const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
  const seconds = Math.floor((diff % (1000 * 60)) / 1000)

  countdownDisplay.value = `${days} 天 ${hours} 小時 ${minutes} 分 ${seconds} 秒`
}

const handleRotate = async () => {
  if (!confirm('警告：輪替金鑰將產生新的 Client Secret。原金鑰將保留 7 天過渡相容期（零停機），是否確認進行輪替？'))
    return

  rotating.value = true
  error.value = ''
  message.value = ''

  try {
    const res = await developerApi.rotateSecret(appId)
    newSecretValue.value = res.newSecret
    showNewSecretModal.value = true
    await fetchCredentials()
    message.value = res.message
  } catch (err: any) {
    error.value = err.message
  } finally {
    rotating.value = false
  }
}

const handleRevoke = async () => {
  if (!confirm('嚴正警告：點擊立即作廢後，使用舊 Secret 的伺服器請求將瞬間收到 invalid_client 失敗！請確保所有第三方叢集已全部更新為新金鑰。確認作廢？'))
    return

  revoking.value = true
  error.value = ''
  message.value = ''

  try {
    const res = await developerApi.revokeRetiringSecret(appId)
    await fetchCredentials()
    message.value = res.message
  } catch (err: any) {
    error.value = err.message
  } finally {
    revoking.value = false
  }
}

const copyClientId = () => {
  if (credentials.value?.clientId) {
    navigator.clipboard.writeText(credentials.value.clientId)
    clientIdCopied.value = true
    setTimeout(() => { clientIdCopied.value = false }, 2000)
  }
}

const copyNewSecret = () => {
  if (newSecretValue.value) {
    navigator.clipboard.writeText(newSecretValue.value)
    newSecretCopied.value = true
    setTimeout(() => { newSecretCopied.value = false }, 2000)
  }
}

onMounted(() => {
  fetchCredentials()
  timerInterval = setInterval(updateCountdown, 1000)
})

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval)
})
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <router-link :to="`/apps/${appId}`" style="color: var(--primary); text-decoration: none; font-size: 0.875rem;">
          ← 返回應用程式詳情
        </router-link>
        <h1 class="page-title" style="margin-top: 0.5rem;">零停機金鑰管理 (Dual-Secret Rotation)</h1>
      </div>
    </div>

    <div v-if="error" class="alert alert-warning">
      {{ error }}
    </div>

    <div v-if="message" class="alert alert-info">
      {{ message }}
    </div>

    <div v-if="loading" style="text-align: center; padding: 3rem;">
      載入金鑰狀態中...
    </div>

    <div v-else-if="credentials">
      <!-- Retiring Secret Grace Period Warning Card -->
      <div v-if="credentials.hasRetiringSecret" class="card alert-warning" style="border: 2px solid var(--warning-border);">
        <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; width: 100%;">
          <div>
            <h3 style="font-size: 1.15rem; font-weight: 700; color: #b45309; display: flex; align-items: center; gap: 0.5rem;">
              ⚠️ 舊金鑰目前處於過渡相容緩衝期（雙金鑰並行）
            </h3>
            <p style="margin-top: 0.5rem; font-size: 0.875rem; color: #92400e;">
              為確保生產環境伺服器滾動更新時零停機，舊金鑰仍可正常通過 Token 交換。
              <br/>
              <strong>剩餘過渡緩衝時間：</strong>
              <span style="font-family: monospace; font-size: 1rem; font-weight: 700; color: #b45309; margin-left: 0.25rem;">
                {{ countdownDisplay }}
              </span>
            </p>
          </div>
          <button @click="handleRevoke" class="btn btn-danger" :disabled="revoking">
            {{ revoking ? '作廢中...' : '已完成伺服器更新，立即廢止舊金鑰' }}
          </button>
        </div>
      </div>

      <!-- Active Client ID & Secret Card -->
      <div class="card">
        <h2 style="font-size: 1.25rem; font-weight: 700; margin-bottom: 1.25rem;">
          當前有效金鑰 (Active Credentials)
        </h2>

        <div class="form-group">
          <label class="form-label">Client ID (公開客戶端識別碼)</label>
          <div class="code-box">
            <span>{{ credentials.clientId }}</span>
            <button @click="copyClientId" class="btn btn-secondary btn-sm">
              {{ clientIdCopied ? '已複製！' : '複製' }}
            </button>
          </div>
        </div>

        <div class="form-group">
          <label class="form-label">當前主要 Client Secret (Active)</label>
          <div class="code-box" style="color: #94a3b8;">
            <span>{{ credentials.activeSecretMasked || '無有效 Secret' }}</span>
            <span style="font-size: 0.75rem; color: #64748b;">(基於安全原則，僅建立或輪替時顯示一次)</span>
          </div>
        </div>

        <div style="margin-top: 1.75rem; padding-top: 1.25rem; border-top: 1px solid var(--border); display: flex; justify-content: space-between; align-items: center;">
          <div>
            <h4 style="font-size: 0.95rem; font-weight: 600;">輪替 Client Secret</h4>
            <p style="font-size: 0.8rem; color: var(--text-muted); margin-top: 0.25rem;">
              當金鑰疑似外洩或依據安全合規週期需要更換時，立即生成新金鑰並保留舊金鑰 7 天相容期。
            </p>
          </div>
          <button @click="handleRotate" class="btn btn-warning" :disabled="rotating">
            {{ rotating ? '輪替中...' : '輪替金鑰 (Rotate Secret)' }}
          </button>
        </div>
      </div>

      <!-- Architecture Explanatory Card -->
      <div class="card" style="background-color: #f8fafc;">
        <h3 style="font-size: 1rem; font-weight: 600; margin-bottom: 0.5rem; color: #334155;">
          💡 零停機雙金鑰輪替 (Zero-Downtime Secret Rotation) 原理
        </h3>
        <ol style="margin-left: 1.25rem; font-size: 0.85rem; color: #64748b; line-height: 1.6;">
          <li>點擊「輪替金鑰」後，平台即刻產生全新的 <strong>Active Secret</strong>。</li>
          <li>原 Secret 降級為 <strong>Retiring Secret</strong>，系統將同時接受新舊兩組金鑰驗證長達 7 天。</li>
          <li>您的後端維運團隊可逐步重啟/發布線上多台應用伺服器，期間完全無中斷。</li>
          <li>發布完成後，可手動點擊「立即廢止舊金鑰」徹底清除舊金鑰。</li>
        </ol>
      </div>
    </div>

    <!-- Rotate Secret Modal -->
    <div v-if="showNewSecretModal" class="modal-overlay">
      <div class="modal-content">
        <h2 style="font-size: 1.25rem; font-weight: 700; margin-bottom: 0.75rem; color: #1e293b;">
          🎉 新金鑰已生成
        </h2>
        <div class="alert alert-warning" style="margin-bottom: 1rem; font-size: 0.85rem;">
          ⚠️ <strong>請立即複製並更新至生產環境配置！</strong> 本金鑰明文僅於本次操作顯示一次。
        </div>

        <div style="margin-bottom: 1.5rem;">
          <label class="form-label">全新 Client Secret</label>
          <div class="code-box" style="color: #4ade80;">
            <span>{{ newSecretValue }}</span>
            <button @click="copyNewSecret" class="btn btn-secondary btn-sm" style="margin-left: 0.5rem;">
              {{ newSecretCopied ? '已複製！' : '複製' }}
            </button>
          </div>
        </div>

        <div style="text-align: right;">
          <button @click="showNewSecretModal = false" class="btn btn-primary">
            我已複製妥善，關閉視窗
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
