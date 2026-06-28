<template>
  <div class="page">
    <h1>OAuth2 SPA Demo</h1>
    <p class="subtitle">Vue 3 + oidc-client-ts + OpenIddict</p>

    <div v-if="loggedIn" class="card">
      <p>✅ 已登入為 <strong>{{ user?.profile.name || user?.profile.email }}</strong></p>
      <div class="actions">
        <router-link to="/profile" class="btn btn-primary">查看個人資料</router-link>
        <button class="btn btn-outline" @click="doLogout">登出</button>
      </div>
    </div>

    <div v-else class="card">
      <p>尚未登入，點擊下方按鈕開始 OAuth2 Authorization Code + PKCE 流程。</p>
      <button class="btn btn-primary" @click="doLogin">登入</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { login, logout, getUser } from '../auth/oidc'
import type { User } from 'oidc-client-ts'

const loggedIn = ref(false)
const user = ref<User | null>(null)

onMounted(async () => {
  user.value = await getUser()
  loggedIn.value = !!user.value && !user.value.expired
})

async function doLogin() {
  await login()
}

async function doLogout() {
  await logout()
}
</script>
