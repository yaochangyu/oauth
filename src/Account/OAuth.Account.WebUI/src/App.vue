<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getUser, login, logout } from './auth/oidc'
import type { User } from 'oidc-client-ts'

const route = useRoute()
const currentUser = ref<User | null>(null)
const authLoading = ref(true)

const checkAuth = async () => {
  authLoading.value = true
  try {
    currentUser.value = await getUser()
  } catch (err) {
    console.error('Failed to get current user:', err)
  } finally {
    authLoading.value = false
  }
}

const handleLogin = async () => {
  await login()
}

const handleLogout = async () => {
  await logout()
}

onMounted(() => {
  checkAuth()
})
</script>

<template>
  <div class="app-layout">
    <header class="navbar">
      <div class="navbar-container">
        <div class="navbar-brand">
          <span style="font-size: 1.5rem;">🔐</span>
          <span class="brand-title">會員自服務中心</span>
          <span class="badge badge-primary" style="font-size: 0.75rem;">Account Portal</span>
        </div>

        <nav class="nav-links" v-if="!authLoading">
          <router-link to="/profile" class="nav-link" :class="{ active: route.path === '/profile' }">
            個人資料
          </router-link>
          <router-link to="/security" class="nav-link" :class="{ active: route.path === '/security' }">
            帳號安全 & 2FA
          </router-link>
          <router-link to="/apps" class="nav-link" :class="{ active: route.path === '/apps' }">
            已授權應用程式
          </router-link>
        </nav>

        <div class="navbar-user">
          <div v-if="authLoading" style="font-size: 0.875rem; color: var(--text-muted);">
            載入中...
          </div>
          <div v-else-if="currentUser && !currentUser.expired" style="display: flex; align-items: center; gap: 1rem;">
            <span style="font-size: 0.875rem; font-weight: 500;">
              {{ currentUser.profile.name || currentUser.profile.email || currentUser.profile.sub }}
            </span>
            <button @click="handleLogout" class="btn btn-outline" style="font-size: 0.8125rem; padding: 0.375rem 0.75rem;">
              登出
            </button>
          </div>
          <div v-else>
            <button @click="handleLogin" class="btn btn-primary" style="font-size: 0.8125rem; padding: 0.375rem 0.875rem;">
              SSO 登入
            </button>
          </div>
        </div>
      </div>
    </header>

    <main class="main-content">
      <router-view />
    </main>

    <footer class="app-footer">
      <p>© 2026 OAuth Platform · 會員自服務中心 (Phase 2)</p>
    </footer>
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
}

.navbar {
  background-color: var(--bg-card);
  border-bottom: 1px solid var(--border);
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar-container {
  max-width: 1024px;
  margin: 0 auto;
  padding: 0.75rem 1.5rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.brand-title {
  font-weight: 700;
  font-size: 1.125rem;
  color: var(--text-main);
}

.nav-links {
  display: flex;
  gap: 0.5rem;
}

.nav-link {
  padding: 0.5rem 0.875rem;
  border-radius: 6px;
  text-decoration: none;
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--text-muted);
  transition: all 0.15s ease;
}

.nav-link:hover {
  color: var(--text-main);
  background-color: var(--bg);
}

.nav-link.active {
  color: var(--primary);
  background-color: #eff6ff;
  font-weight: 600;
}

.navbar-user {
  display: flex;
  align-items: center;
}

.main-content {
  flex: 1;
  max-width: 1024px;
  width: 100%;
  margin: 0 auto;
  padding: 2rem 1.5rem;
}

.app-footer {
  border-top: 1px solid var(--border);
  padding: 1.5rem;
  text-align: center;
  font-size: 0.8125rem;
  color: var(--text-muted);
  background-color: var(--bg-card);
}
</style>
