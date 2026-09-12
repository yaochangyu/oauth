<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { developerApi, type AppResponse } from '../api/developer'

const apps = ref<AppResponse[]>([])
const selectedAppId = ref('')
const clientId = ref('')
const redirectUri = ref('')
const scope = ref('openid profile email')
const codeVerifier = ref('')
const codeChallenge = ref('')
const state = ref('')
const generatedUrl = ref('')
const urlCopied = ref(false)

// Validate credentials tool
const testClientId = ref('')
const testClientSecret = ref('')
const testResult = ref<{ isValid: boolean; message: string; error?: string } | null>(null)
const validating = ref(false)

const fetchApps = async () => {
  try {
    apps.value = await developerApi.getApps()
    if (apps.value.length > 0) {
      selectApp(apps.value[0])
    }
  } catch {
    // ignore
  }
}

const selectApp = (app: AppResponse) => {
  selectedAppId.value = app.id
  clientId.value = app.clientId
  redirectUri.value = app.redirectUris[0] || 'https://localhost:5001/callback'
  scope.value = app.requestedScopes.join(' ') || 'openid profile email'
  testClientId.value = app.clientId
}

const onAppSelectChange = () => {
  const found = apps.value.find(a => a.id === selectedAppId.value)
  if (found) selectApp(found)
}

// Generate PKCE helper
const generatePKCE = async () => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~'
  let verifier = ''
  const bytes = new Uint8Array(64)
  crypto.getRandomValues(bytes)
  for (let i = 0; i < bytes.length; i++) {
    verifier += chars[bytes[i] % chars.length]
  }
  codeVerifier.value = verifier

  // Calculate SHA-256 for code_challenge
  const encoder = new TextEncoder()
  const data = encoder.encode(verifier)
  const hash = await crypto.subtle.digest('SHA-256', data)
  const base64 = btoa(String.fromCharCode(...new Uint8Array(hash)))
  codeChallenge.value = base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')

  state.value = Math.random().toString(36).substring(2, 10)
}

const handleGenerateUrl = async () => {
  if (!codeChallenge.value) {
    await generatePKCE()
  }

  try {
    const res = await developerApi.generateAuthorizeUrl({
      clientId: clientId.value,
      redirectUri: redirectUri.value,
      scope: scope.value,
      codeChallenge: codeChallenge.value,
      codeChallengeMethod: 'S256',
      state: state.value,
    })
    generatedUrl.value = res.authorizeUrl
  } catch (err: any) {
    alert(err.message)
  }
}

const copyUrl = () => {
  if (generatedUrl.value) {
    navigator.clipboard.writeText(generatedUrl.value)
    urlCopied.value = true
    setTimeout(() => { urlCopied.value = false }, 2000)
  }
}

const handleValidateSecret = async () => {
  validating.value = true
  testResult.value = null
  try {
    await developerApi.validateCredentials({
      clientId: testClientId.value,
      clientSecret: testClientSecret.value,
    })
    testResult.value = {
      isValid: true,
      message: '✓ 金鑰驗證成功！此 Secret 為有效金鑰（Active 或相容期內的 Retiring 金鑰）。',
    }
  } catch (err: any) {
    testResult.value = {
      isValid: false,
      message: `✗ 驗證失敗：${err.message}`,
      error: 'invalid_client',
    }
  } finally {
    validating.value = false
  }
}

onMounted(() => {
  fetchApps()
  generatePKCE()
})
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">沙盒除錯與 PKCE 測試工具</h1>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-top: 0.25rem;">
          模擬第三方應用程式發起 OAuth 2.0 / OIDC 授權碼與 PKCE 流程，檢驗配置正確性。
        </p>
      </div>
    </div>

    <div class="card">
      <h2 style="font-size: 1.15rem; font-weight: 600; margin-bottom: 1rem;">
        1. 快速載入我的應用程式
      </h2>
      <div class="form-group" v-if="apps.length > 0">
        <label class="form-label">選擇既有 App</label>
        <select v-model="selectedAppId" @change="onAppSelectChange" class="form-select">
          <option v-for="a in apps" :key="a.id" :value="a.id">
            {{ a.displayName }} ({{ a.clientId }}) [{{ a.appType }}]
          </option>
        </select>
      </div>
    </div>

    <!-- PKCE Authorize URL Generator -->
    <div class="card">
      <h2 style="font-size: 1.15rem; font-weight: 600; margin-bottom: 1rem;">
        2. PKCE 授權網址產生器 (RFC 9700 S256)
      </h2>

      <div class="form-group">
        <label class="form-label">Client ID</label>
        <input v-model="clientId" type="text" class="form-input" required />
      </div>

      <div class="form-group">
        <label class="form-label">Redirect URI</label>
        <input v-model="redirectUri" type="text" class="form-input" required />
      </div>

      <div class="form-group">
        <label class="form-label">Scopes</label>
        <input v-model="scope" type="text" class="form-input" />
      </div>

      <div style="background-color: #f8fafc; border: 1px solid var(--border); border-radius: var(--radius); padding: 1rem; margin-bottom: 1.25rem;">
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;">
          <h4 style="font-size: 0.875rem; font-weight: 600;">PKCE 參數 (Proof Key for Code Exchange)</h4>
          <button @click="generatePKCE" class="btn btn-secondary btn-sm">
            重新生成隨機 PKCE Code Verifier
          </button>
        </div>

        <div class="form-group" style="margin-bottom: 0.75rem;">
          <label class="form-label" style="font-size: 0.8rem;">code_verifier (客戶端留存用於換 Token)</label>
          <div class="code-box" style="font-size: 0.75rem;">{{ codeVerifier }}</div>
        </div>

        <div class="form-group" style="margin-bottom: 0;">
          <label class="form-label" style="font-size: 0.8rem;">code_challenge (S256 雜湊，隨 authorize 發送)</label>
          <div class="code-box" style="font-size: 0.75rem; color: #a78bfa;">{{ codeChallenge }}</div>
        </div>
      </div>

      <button @click="handleGenerateUrl" class="btn btn-primary">
        生成授權網址 (Generate Authorize URL)
      </button>

      <div v-if="generatedUrl" style="margin-top: 1.5rem; padding-top: 1.25rem; border-top: 1px solid var(--border);">
        <label class="form-label">生成的 OAuth 2.0 授權測試網址：</label>
        <div class="code-box" style="font-size: 0.8rem; word-break: break-all;">
          <span>{{ generatedUrl }}</span>
          <button @click="copyUrl" class="btn btn-secondary btn-sm" style="margin-left: 0.5rem;">
            {{ urlCopied ? '已複製！' : '複製網址' }}
          </button>
        </div>
        <div style="margin-top: 0.75rem;">
          <a :href="generatedUrl" target="_blank" class="btn btn-secondary btn-sm">
            在新視窗開啟並測試授權流程 ↗
          </a>
        </div>
      </div>
    </div>

    <!-- Secret Validation Test Tool -->
    <div class="card">
      <h2 style="font-size: 1.15rem; font-weight: 600; margin-bottom: 0.5rem;">
        3. 金鑰有效性檢測工具 (支援雙金鑰與過渡期驗證)
      </h2>
      <p style="font-size: 0.85rem; color: var(--text-muted); margin-bottom: 1rem;">
        輸入 Client ID 與 Client Secret，即時測試金鑰是否通過伺服器端雜湊驗證。
      </p>

      <div class="form-group">
        <label class="form-label">Client ID</label>
        <input v-model="testClientId" type="text" class="form-input" />
      </div>

      <div class="form-group">
        <label class="form-label">Client Secret (新金鑰或輪替中的舊金鑰)</label>
        <input v-model="testClientSecret" type="text" class="form-input" placeholder="sk_live_..." />
      </div>

      <button @click="handleValidateSecret" class="btn btn-secondary" :disabled="validating">
        {{ validating ? '驗證中...' : '測試金鑰有效性' }}
      </button>

      <div v-if="testResult" style="margin-top: 1rem;">
        <div :class="testResult.isValid ? 'alert alert-info' : 'alert alert-warning'">
          {{ testResult.message }}
        </div>
      </div>
    </div>
  </div>
</template>
