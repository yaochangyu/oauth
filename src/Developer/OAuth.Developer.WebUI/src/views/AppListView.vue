<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { developerApi, type AppResponse } from '../api/developer'

const apps = ref<AppResponse[]>([])
const activeTab = ref<'all' | 'Sandbox' | 'InReview' | 'Approved'>('all')
const loading = ref(true)
const error = ref('')

const fetchApps = async () => {
  loading.value = true
  error.value = ''
  try {
    apps.value = await developerApi.getApps()
  } catch (err: any) {
    error.value = err.message
  } finally {
    loading.value = false
  }
}

const filteredApps = computed(() => {
  if (activeTab.value === 'all') return apps.value
  return apps.value.filter(app => app.status === activeTab.value)
})

const getStatusBadgeClass = (status: string) => {
  switch (status) {
    case 'Sandbox': return 'badge-sandbox'
    case 'InReview': return 'badge-inreview'
    case 'Approved': return 'badge-approved'
    case 'Rejected': return 'badge-rejected'
    default: return 'badge-sandbox'
  }
}

onMounted(fetchApps)
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">我的應用程式</h1>
        <p style="color: var(--text-muted); font-size: 0.875rem; margin-top: 0.25rem;">
          管理您註冊的所有 Web、SPA 與 Mobile OAuth 應用程式。
        </p>
      </div>
      <router-link to="/apps/create" class="btn btn-primary">
        + 建立應用程式
      </router-link>
    </div>

    <div style="display: flex; gap: 0.5rem; margin-bottom: 1.5rem; border-bottom: 1px solid var(--border); padding-bottom: 0.5rem;">
      <button
        class="btn btn-sm"
        :class="activeTab === 'all' ? 'btn-primary' : 'btn-secondary'"
        @click="activeTab = 'all'"
      >
        全部 ({{ apps.length }})
      </button>
      <button
        class="btn btn-sm"
        :class="activeTab === 'Sandbox' ? 'btn-primary' : 'btn-secondary'"
        @click="activeTab = 'Sandbox'"
      >
        沙盒測試 ({{ apps.filter(a => a.status === 'Sandbox').length }})
      </button>
      <button
        class="btn btn-sm"
        :class="activeTab === 'InReview' ? 'btn-primary' : 'btn-secondary'"
        @click="activeTab = 'InReview'"
      >
        審核中 ({{ apps.filter(a => a.status === 'InReview').length }})
      </button>
      <button
        class="btn btn-sm"
        :class="activeTab === 'Approved' ? 'btn-primary' : 'btn-secondary'"
        @click="activeTab = 'Approved'"
      >
        已核准上線 ({{ apps.filter(a => a.status === 'Approved').length }})
      </button>
    </div>

    <div v-if="error" class="alert alert-warning">
      {{ error }}
    </div>

    <div v-if="loading" style="text-align: center; padding: 3rem; color: var(--text-muted);">
      載入中...
    </div>

    <div v-else-if="filteredApps.length === 0" class="card" style="text-align: center; padding: 3rem;">
      <p style="color: var(--text-muted); margin-bottom: 1rem;">尚無符合條件的應用程式</p>
      <router-link to="/apps/create" class="btn btn-primary">
        立即建立第一個應用程式
      </router-link>
    </div>

    <div v-else style="display: grid; grid-template-columns: repeat(auto-fill, minmax(320px, 1fr)); gap: 1.25rem;">
      <div v-for="app in filteredApps" :key="app.id" class="card" style="display: flex; flex-direction: column; justify-content: space-between;">
        <div>
          <div style="display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 0.75rem;">
            <h3 style="font-size: 1.15rem; font-weight: 600;">{{ app.displayName }}</h3>
            <span class="badge" :class="getStatusBadgeClass(app.status)">
              {{ app.status }}
            </span>
          </div>

          <div style="display: flex; gap: 0.5rem; margin-bottom: 0.75rem;">
            <span class="badge badge-pkce">強制 PKCE</span>
            <span class="badge" style="background-color: #f1f5f9; color: #475569;">
              {{ app.appType }} ({{ app.clientType }})
            </span>
          </div>

          <p style="color: var(--text-muted); font-size: 0.875rem; margin-bottom: 1rem; min-height: 2.5rem;">
            {{ app.description || '無詳細描述' }}
          </p>

          <div style="font-size: 0.75rem; color: var(--text-muted); margin-bottom: 1rem;">
            <strong>Client ID:</strong>
            <div class="code-box" style="padding: 0.35rem 0.5rem; font-size: 0.75rem;">
              {{ app.clientId }}
            </div>
          </div>
        </div>

        <div style="display: flex; gap: 0.5rem; border-top: 1px solid var(--border); padding-top: 1rem;">
          <router-link :to="`/apps/${app.id}`" class="btn btn-secondary btn-sm" style="flex: 1;">
            設定與詳情
          </router-link>
          <router-link v-if="app.clientType === 'confidential'" :to="`/apps/${app.id}/keys`" class="btn btn-primary btn-sm" style="flex: 1;">
            金鑰輪替管理
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>
