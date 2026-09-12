<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  securityApi,
  type TwoFactorStatus,
  type Generate2FaKeyResult,
  type ChangePasswordPayload,
} from '../api/security'

// 2FA 狀態
const twoFactorStatus = ref<TwoFactorStatus | null>(null)
const keySetup = ref<Generate2FaKeyResult | null>(null)
const verificationCode = ref('')
const loading2Fa = ref(true)
const action2FaLoading = ref(false)
const error2Fa = ref('')
const success2Fa = ref('')

// 修改密碼
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const passwordLoading = ref(false)
const errorPassword = ref('')
const successPassword = ref('')

const fetch2FaStatus = async () => {
  loading2Fa.value = true
  error2Fa.value = ''
  try {
    twoFactorStatus.value = await securityApi.getTwoFactorStatus()
  } catch (err: any) {
    error2Fa.value = err.message
  } finally {
    loading2Fa.value = false
  }
}

const handleStart2FaSetup = async () => {
  action2FaLoading.value = true
  error2Fa.value = ''
  success2Fa.value = ''
  try {
    keySetup.value = await securityApi.generateTwoFactorKey()
  } catch (err: any) {
    error2Fa.value = err.message
  } finally {
    action2FaLoading.value = false
  }
}

const handleVerify2Fa = async () => {
  if (!verificationCode.value) {
    error2Fa.value = '請輸入 6 位數驗證碼'
    return
  }
  action2FaLoading.value = true
  error2Fa.value = ''
  success2Fa.value = ''
  try {
    const res = await securityApi.verifyAndEnableTwoFactor(verificationCode.value)
    success2Fa.value = res.message || '2FA 雙層驗證已成功啟用！'
    keySetup.value = null
    verificationCode.value = ''
    await fetch2FaStatus()
  } catch (err: any) {
    error2Fa.value = err.message
  } finally {
    action2FaLoading.value = false
  }
}

const handleDisable2Fa = async () => {
  if (!confirm('確定要停用 2FA 雙層驗證嗎？停用後您的帳號安全性將降低。')) {
    return
  }
  action2FaLoading.value = true
  error2Fa.value = ''
  success2Fa.value = ''
  try {
    const res = await securityApi.disableTwoFactor()
    success2Fa.value = res.message || '2FA 雙層驗證已停用。'
    await fetch2FaStatus()
  } catch (err: any) {
    error2Fa.value = err.message
  } finally {
    action2FaLoading.value = false
  }
}

const handleChangePassword = async () => {
  if (newPassword.value !== confirmPassword.value) {
    errorPassword.value = '新密碼與確認密碼不一致'
    return
  }
  passwordLoading.value = true
  errorPassword.value = ''
  successPassword.value = ''
  try {
    const payload: ChangePasswordPayload = {
      currentPassword: currentPassword.value,
      newPassword: newPassword.value,
      confirmPassword: confirmPassword.value,
    }
    const res = await securityApi.changePassword(payload)
    successPassword.value = res.message || '密碼已成功更新！'
    currentPassword.value = ''
    newPassword.value = ''
    confirmPassword.value = ''
  } catch (err: any) {
    errorPassword.value = err.message
  } finally {
    passwordLoading.value = false
  }
}

onMounted(() => {
  fetch2FaStatus()
})
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">帳號安全</h1>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-top: 0.25rem;">
          管理您的登入密碼及兩步驟驗證 (2FA) 安全設定。
        </p>
      </div>
    </div>

    <!-- 2FA 雙層驗證區塊 -->
    <div class="card" style="margin-bottom: 2rem;">
      <h2 style="font-size: 1.25rem; font-weight: 600; margin-bottom: 0.5rem;">兩步驟驗證 (2FA / TOTP)</h2>
      <p style="color: var(--text-muted); font-size: 0.875rem; margin-bottom: 1.5rem;">
        透過 Google Authenticator、Microsoft Authenticator 等驗證器 App 提供額外安全防護。
      </p>

      <div v-if="error2Fa" class="alert alert-danger">{{ error2Fa }}</div>
      <div v-if="success2Fa" class="alert alert-success">{{ success2Fa }}</div>

      <div v-if="loading2Fa" style="color: var(--text-muted); padding: 1rem 0;">載入 2FA 狀態中...</div>
      
      <div v-else>
        <!-- 已啟用 2FA -->
        <div v-if="twoFactorStatus?.isTwoFactorEnabled" style="display: flex; align-items: center; justify-content: space-between; padding: 1rem; background-color: #ecfdf5; border-radius: 8px; border: 1px solid #a7f3d0;">
          <div>
            <div style="display: flex; align-items: center; gap: 0.5rem;">
              <span class="badge badge-success">已啟用</span>
              <strong style="color: #065f46;">兩步驟驗證目前處於啟用狀態</strong>
            </div>
            <p style="font-size: 0.875rem; color: #047857; margin-top: 0.25rem;">每次登入均需輸入驗證器產生之 6 位數驗證碼。</p>
          </div>
          <button
            @click="handleDisable2Fa"
            :disabled="action2FaLoading"
            class="btn btn-outline"
            style="color: var(--danger); border-color: var(--danger);"
          >
            {{ action2FaLoading ? '處理中...' : '停用 2FA' }}
          </button>
        </div>

        <!-- 未啟用 2FA -->
        <div v-else>
          <div v-if="!keySetup" style="display: flex; align-items: center; justify-content: space-between; padding: 1rem; background-color: var(--bg-card); border-radius: 8px; border: 1px solid var(--border);">
            <div>
              <div style="display: flex; align-items: center; gap: 0.5rem;">
                <span class="badge badge-muted">未啟用</span>
                <strong>兩步驟驗證目前未啟用</strong>
              </div>
              <p style="font-size: 0.875rem; color: var(--text-muted); margin-top: 0.25rem;">
                啟用後可提升帳號安全性，防止密碼洩漏造成的未授權存取。
              </p>
            </div>
            <button
              @click="handleStart2FaSetup"
              :disabled="action2FaLoading"
              class="btn btn-primary"
            >
              {{ action2FaLoading ? '載入中...' : '設定並啟用 2FA' }}
            </button>
          </div>

          <!-- 設定 2FA 流程 -->
          <div v-else style="padding: 1.5rem; background-color: var(--bg); border-radius: 8px; border: 1px solid var(--border); margin-top: 1rem;">
            <h3 style="font-size: 1rem; font-weight: 600; margin-bottom: 0.75rem;">1. 設定驗證器 App</h3>
            <p style="font-size: 0.875rem; color: var(--text-muted); margin-bottom: 1rem;">
              請開啟您的身份驗證器 App，手動輸入以下金鑰或開啟連結：
            </p>

            <div class="form-group">
              <label class="form-label">金鑰 (Secret Key)</label>
              <input
                type="text"
                :value="keySetup.sharedKey"
                readonly
                class="form-input"
                style="font-family: monospace; letter-spacing: 0.1em; background: #fff;"
              />
            </div>

            <div class="form-group">
              <label class="form-label">Authenticator URI</label>
              <input
                type="text"
                :value="keySetup.authenticatorUri"
                readonly
                class="form-input"
                style="font-family: monospace; font-size: 0.8125rem; background: #fff;"
              />
            </div>

            <h3 style="font-size: 1rem; font-weight: 600; margin-top: 1.5rem; margin-bottom: 0.75rem;">2. 輸入 6 位數驗證碼</h3>
            <div style="display: flex; gap: 0.75rem; max-width: 400px;">
              <input
                v-model="verificationCode"
                type="text"
                placeholder="例如: 123456"
                maxlength="6"
                class="form-input"
                style="text-align: center; font-size: 1.25rem; letter-spacing: 0.25em;"
              />
              <button
                @click="handleVerify2Fa"
                :disabled="action2FaLoading || !verificationCode"
                class="btn btn-primary"
                style="white-space: nowrap;"
              >
                {{ action2FaLoading ? '驗證中...' : '驗證並啟用' }}
              </button>
              <button
                @click="keySetup = null"
                class="btn btn-outline"
                style="white-space: nowrap;"
              >
                取消
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 修改密碼區塊 -->
    <div class="card">
      <h2 style="font-size: 1.25rem; font-weight: 600; margin-bottom: 0.5rem;">修改登入密碼</h2>
      <p style="color: var(--text-muted); font-size: 0.875rem; margin-bottom: 1.5rem;">
        密碼長度需至少 8 個字元，並包含大寫英文、小寫英文、數字與特殊符號。
      </p>

      <div v-if="errorPassword" class="alert alert-danger">{{ errorPassword }}</div>
      <div v-if="successPassword" class="alert alert-success">{{ successPassword }}</div>

      <form @submit.prevent="handleChangePassword" style="max-width: 480px;">
        <div class="form-group">
          <label class="form-label">目前密碼</label>
          <input
            v-model="currentPassword"
            type="password"
            required
            class="form-input"
            placeholder="請輸入目前密碼"
          />
        </div>

        <div class="form-group">
          <label class="form-label">新密碼</label>
          <input
            v-model="newPassword"
            type="password"
            required
            class="form-input"
            placeholder="請輸入新密碼"
          />
        </div>

        <div class="form-group">
          <label class="form-label">確認新密碼</label>
          <input
            v-model="confirmPassword"
            type="password"
            required
            class="form-input"
            placeholder="請再次輸入新密碼"
          />
        </div>

        <div style="margin-top: 1.5rem;">
          <button
            type="submit"
            :disabled="passwordLoading || !currentPassword || !newPassword || !confirmPassword"
            class="btn btn-primary"
          >
            {{ passwordLoading ? '更新中...' : '儲存新密碼' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
