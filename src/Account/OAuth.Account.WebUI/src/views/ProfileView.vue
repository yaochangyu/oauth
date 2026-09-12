<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { profileApi, type UserProfile } from '../api/profile'

const profile = ref<UserProfile | null>(null)
const displayName = ref('')
const avatarUrl = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const success = ref('')

const fetchProfile = async () => {
  loading.value = true
  error.value = ''
  try {
    profile.value = await profileApi.getProfile()
    displayName.value = profile.value.displayName || ''
    avatarUrl.value = profile.value.avatarUrl || ''
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const handleSave = async () => {
  saving.value = true
  error.value = ''
  success.value = ''
  try {
    profile.value = await profileApi.updateProfile({
      displayName: displayName.value,
      avatarUrl: avatarUrl.value,
    })
    success.value = '個人資料已成功更新！'
  } catch (err: any) {
    error.value = err.message
  } finally {
    saving.value = false
  }
}

onMounted(fetchProfile)
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">個人資料</h1>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-top: 0.25rem;">
          檢視與維護您的個人基本資料、暱稱與頭像。
        </p>
      </div>
    </div>

    <div v-if="error" class="alert alert-danger">
      {{ error }}
    </div>

    <div v-if="success" class="alert alert-success">
      {{ success }}
    </div>

    <div v-if="loading" class="card" style="text-align: center; padding: 3rem; color: var(--text-muted);">
      載入中...
    </div>

    <div v-else-if="profile" class="card">
      <div style="display: flex; align-items: center; gap: 1.5rem; margin-bottom: 2rem; padding-bottom: 1.5rem; border-bottom: 1px solid var(--border);">
        <img
          v-if="profile.avatarUrl"
          :src="profile.avatarUrl"
          alt="Avatar"
          class="avatar-circle"
        />
        <div v-else class="avatar-circle">
          {{ (profile.displayName || profile.email || 'U').charAt(0).toUpperCase() }}
        </div>
        <div>
          <h2 style="font-size: 1.25rem; font-weight: 600;">{{ profile.displayName || '未設定暱稱' }}</h2>
          <p style="color: var(--text-muted); font-size: 0.875rem;">{{ profile.email }}</p>
          <div style="display: flex; gap: 0.5rem; margin-top: 0.5rem;">
            <span v-if="profile.emailConfirmed" class="badge badge-success">Email 已驗證</span>
            <span v-if="profile.isTwoFactorEnabled" class="badge badge-success">2FA 雙層驗證已開啟</span>
            <span v-else class="badge badge-muted">2FA 雙層驗證未開啟</span>
          </div>
        </div>
      </div>

      <form @submit.prevent="handleSave">
        <div class="form-group">
          <label class="form-label">使用者 ID (Subject)</label>
          <input
            type="text"
            :value="profile.userId"
            disabled
            class="form-input"
            style="background-color: #f1f5f9; cursor: not-allowed;"
          />
        </div>

        <div class="form-group">
          <label class="form-label">電子信箱</label>
          <input
            type="email"
            :value="profile.email"
            disabled
            class="form-input"
            style="background-color: #f1f5f9; cursor: not-allowed;"
          />
        </div>

        <div class="form-group">
          <label class="form-label">顯示暱稱</label>
          <input
            v-model="displayName"
            type="text"
            placeholder="請輸入您的暱稱"
            class="form-input"
          />
          <p class="form-hint">授權第三方應用程式時將顯示此暱稱。</p>
        </div>

        <div class="form-group">
          <label class="form-label">大頭貼圖片 URL</label>
          <input
            v-model="avatarUrl"
            type="url"
            placeholder="https://example.com/avatar.png"
            class="form-input"
          />
          <p class="form-hint">支援公開可存取的 HTTPS 圖片連結。</p>
        </div>

        <div style="display: flex; justify-content: flex-end; margin-top: 1.5rem;">
          <button type="submit" :disabled="saving" class="btn btn-primary">
            {{ saving ? '儲存中...' : '儲存變更' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
