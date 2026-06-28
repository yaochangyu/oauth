<template>
  <div class="page">
    <h1>個人資料</h1>

    <div v-if="user" class="card">
      <div class="field">
        <label>名稱</label>
        <span>{{ user.profile.name }}</span>
      </div>
      <div class="field">
        <label>Email</label>
        <span>{{ user.profile.email }}</span>
      </div>
      <div class="field">
        <label>Subject</label>
        <span class="mono">{{ user.profile.sub }}</span>
      </div>
      <div class="field">
        <label>Access Token 有效</label>
        <span :class="user.expired ? 'tag-red' : 'tag-green'">
          {{ user.expired ? '已過期' : '有效（' + expiresIn + '）' }}
        </span>
      </div>
      <div class="field">
        <label>Refresh Token</label>
        <span :class="user.refresh_token ? 'tag-green' : 'tag-red'">
          {{ user.refresh_token ? '有值' : '無' }}
        </span>
      </div>

      <h3>Claims</h3>
      <table>
        <thead><tr><th>Type</th><th>Value</th></tr></thead>
        <tbody>
          <tr v-for="(val, key) in user.profile" :key="key">
            <td class="mono">{{ key }}</td>
            <td>{{ val }}</td>
          </tr>
        </tbody>
      </table>

      <div class="actions" style="margin-top:24px">
        <router-link to="/" class="btn btn-outline">← 返回首頁</router-link>
        <button class="btn btn-outline" @click="doLogout">登出</button>
      </div>
    </div>

    <div v-else class="card">
      <p>尚未登入。<router-link to="/">返回首頁</router-link></p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getUser, logout } from '../auth/oidc'
import type { User } from 'oidc-client-ts'

const router = useRouter()
const user = ref<User | null>(null)

const expiresIn = computed(() => {
  if (!user.value || user.value.expired) return ''
  const secs = user.value.expires_in ?? 0
  if (secs > 60) return Math.floor(secs / 60) + ' 分鐘'
  return secs + ' 秒'
})

onMounted(async () => {
  user.value = await getUser()
  if (!user.value) router.push('/')
})

async function doLogout() {
  await logout()
}
</script>
