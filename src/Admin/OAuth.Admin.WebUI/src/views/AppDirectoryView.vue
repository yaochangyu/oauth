<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type AppDetail } from '../api/admin';

const apps = ref<AppDetail[]>([]);
const loading = ref(false);
const filterStatus = ref('');
const searchKeyword = ref('');
const errorMessage = ref('');
const successMessage = ref('');

async function loadApps() {
  loading.value = true;
  errorMessage.value = '';
  try {
    apps.value = await adminApi.getApps(filterStatus.value || undefined, searchKeyword.value || undefined);
  } catch (err: any) {
    errorMessage.value = err.message || '載入應用程式清單失敗';
  } finally {
    loading.value = false;
  }
}

async function suspend(app: AppDetail) {
  if (!confirm(`【資安警告】確定要強制停用違規應用「${app.displayName || app.clientId}」嗎？\n停用後系統將立即同步吊銷該 Client 底下的所有流通 Access/Refresh Token！`)) return;
  try {
    await adminApi.suspendApp(app.clientId);
    successMessage.value = `已強制停用「${app.displayName || app.clientId}」並吊銷所有流通 Token！`;
    await loadApps();
  } catch (err: any) {
    errorMessage.value = err.message || '停用失敗';
  }
}

async function restore(app: AppDetail) {
  if (!confirm(`確定要恢復應用程式「${app.displayName || app.clientId}」為核准狀態嗎？`)) return;
  try {
    await adminApi.restoreApp(app.clientId);
    successMessage.value = `已成功恢復「${app.displayName || app.clientId}」！`;
    await loadApps();
  } catch (err: any) {
    errorMessage.value = err.message || '恢復失敗';
  }
}

function getBadgeClass(status: string) {
  switch (status.toLowerCase()) {
    case 'sandbox': return 'badge-sandbox';
    case 'inreview': return 'badge-inreview';
    case 'approved': return 'badge-approved';
    case 'rejected': return 'badge-rejected';
    case 'suspended': return 'badge-suspended';
    default: return 'badge-sandbox';
  }
}

onMounted(() => {
  loadApps();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">全域應用程式目錄 (App Directory)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            管理全站 OAuth 2.0 / OIDC 用戶端應用程式狀態與強制停用 / 吊銷安全機制
          </p>
        </div>
      </div>

      <div class="filter-bar">
        <input
          v-model="searchKeyword"
          type="text"
          class="input-text"
          placeholder="搜尋 Client ID、名稱、開發者..."
          @keyup.enter="loadApps"
        />
        <select v-model="filterStatus" class="select" @change="loadApps">
          <option value="">全部狀態</option>
          <option value="Sandbox">Sandbox (沙盒開發)</option>
          <option value="InReview">InReview (審核中)</option>
          <option value="Approved">Approved (已核准上線)</option>
          <option value="Rejected">Rejected (已駁回)</option>
          <option value="Suspended">Suspended (強制停用/已吊銷)</option>
        </select>
        <button class="btn btn-primary" @click="loadApps">查詢</button>
      </div>

      <div v-if="successMessage" style="padding: 12px; background: #dcfce7; color: #15803d; border-radius: 6px; margin-bottom: 16px;">
        {{ successMessage }}
      </div>
      <div v-if="errorMessage" style="padding: 12px; background: #fee2e2; color: #b91c1c; border-radius: 6px; margin-bottom: 16px;">
        {{ errorMessage }}
      </div>

      <div v-if="loading" style="text-align: center; padding: 40px; color: var(--text-muted);">
        載入中...
      </div>

      <div v-else-if="apps.length === 0" style="text-align: center; padding: 40px; color: var(--text-muted);">
        查無符合條件的應用程式。
      </div>

      <div v-else class="table-container">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Client ID / 顯示名稱</th>
              <th>開發者</th>
              <th>用戶端類型</th>
              <th>狀態</th>
              <th>備註 / 駁回理由</th>
              <th style="text-align: right;">維運操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="app in apps" :key="app.id">
              <td>
                <div style="font-weight: 600;">{{ app.displayName || app.clientId }}</div>
                <div style="font-size: 0.8rem; color: var(--text-muted);">{{ app.clientId }}</div>
              </td>
              <td>{{ app.developer || '平台內建 / 系統' }}</td>
              <td>{{ app.clientType }}</td>
              <td>
                <span class="badge" :class="getBadgeClass(app.status)">
                  {{ app.status }}
                </span>
              </td>
              <td style="font-size: 0.85rem; color: var(--danger); max-width: 250px;">
                {{ app.rejectReason || '-' }}
              </td>
              <td style="text-align: right;">
                <div style="display: flex; gap: 8px; justify-content: flex-end;">
                  <button
                    v-if="app.status === 'Approved'"
                    class="btn btn-danger"
                    @click="suspend(app)"
                  >
                    強制停用 (吊銷Token)
                  </button>
                  <button
                    v-if="app.status === 'Suspended'"
                    class="btn btn-success"
                    @click="restore(app)"
                  >
                    恢復核准
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
