<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getUser, login, logout } from './auth/oidc'

const user = ref<any>(null)

onMounted(async () => {
  user.value = await getUser()
})

const handleLogin = async () => {
  await login()
}

const handleLogout = async () => {
  await logout()
}
</script>

<template>
  <div class="app-container">
    <header class="navbar">
      <router-link to="/apps" class="nav-brand">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <path d="M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5"/>
        </svg>
        OAuth 開發者平台
      </router-link>
      <nav class="nav-links">
        <router-link to="/apps" class="nav-link">我的應用程式</router-link>
        <router-link to="/sandbox" class="nav-link">沙盒測試工具</router-link>
        <router-link to="/register" class="nav-link">開發者帳號</router-link>
        <div style="margin-left: 1rem;">
          <button v-if="user" @click="handleLogout" class="btn btn-secondary btn-sm">
            登出 ({{ user.profile?.name || user.profile?.email || 'Developer' }})
          </button>
          <button v-else @click="handleLogin" class="btn btn-primary btn-sm">
            登入
          </button>
        </div>
      </nav>
    </header>

    <main class="main-content">
      <router-view />
    </main>
  </div>
</template>
