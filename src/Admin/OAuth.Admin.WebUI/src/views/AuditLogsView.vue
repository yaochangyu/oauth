<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type AuditLog } from '../api/admin';

const logs = ref<AuditLog[]>([]);
const loading = ref(false);
const filterEventType = ref('');
const searchActor = ref('');
const searchTarget = ref('');
const errorMessage = ref('');

async function loadLogs() {
  loading.value = true;
  errorMessage.value = '';
  try {
    logs.value = await adminApi.getAuditLogs(
      filterEventType.value || undefined,
      searchActor.value || undefined,
      searchTarget.value || undefined
    );
  } catch (err: any) {
    errorMessage.value = err.message || '載入審計日誌失敗';
  } finally {
    loading.value = false;
  }
}

function formatDate(iso: string) {
  if (!iso) return '-';
  try {
    const d = new Date(iso);
    return d.toLocaleString('zh-TW', { hour12: false });
  } catch {
    return iso;
  }
}

function getEventBadgeClass(eventType: string) {
  if (eventType.includes('Suspended') || eventType.includes('Locked') || eventType.includes('Reject')) return 'badge-rejected';
  if (eventType.includes('Approved') || eventType.includes('Unlocked') || eventType.includes('Restored')) return 'badge-approved';
  if (eventType.includes('Revoke')) return 'badge-inreview';
  return 'badge-standard';
}

onMounted(() => {
  loadLogs();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">授權與安全審計日誌 (Audit Logs)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            即時記錄第三方應用審核、違規停用、Token 吊銷、用戶帳號凍結與安全異動事件
          </p>
        </div>
        <button class="btn btn-outline" @click="loadLogs">重新整理</button>
      </div>

      <div class="filter-bar">
        <select v-model="filterEventType" class="select" @change="loadLogs">
          <option value="">全部事件類型</option>
          <option value="AppApproved">AppApproved (應用核准)</option>
          <option value="AppRejected">AppRejected (應用駁回)</option>
          <option value="AppSuspended">AppSuspended (強制停用/吊銷)</option>
          <option value="AppRestored">AppRestored (應用恢復)</option>
          <option value="UserLockedOut">UserLockedOut (用戶凍結)</option>
          <option value="UserUnlocked">UserUnlocked (解除凍結)</option>
          <option value="UserSessionsRevoked">UserSessionsRevoked (強制登出)</option>
          <option value="ScopeCreated">ScopeCreated (建立Scope)</option>
          <option value="ScopeUpdated">ScopeUpdated (更新Scope)</option>
          <option value="ScopeDeleted">ScopeDeleted (刪除Scope)</option>
        </select>
        <input
          v-model="searchActor"
          type="text"
          class="input-text"
          placeholder="搜尋操作人員 (Actor)..."
          @keyup.enter="loadLogs"
        />
        <input
          v-model="searchTarget"
          type="text"
          class="input-text"
          placeholder="搜尋目標對象 (Target)..."
          @keyup.enter="loadLogs"
        />
        <button class="btn btn-primary" @click="loadLogs">搜尋</button>
      </div>

      <div v-if="errorMessage" style="padding: 12px; background: #fee2e2; color: #b91c1c; border-radius: 6px; margin-bottom: 16px;">
        {{ errorMessage }}
      </div>

      <div v-if="loading" style="text-align: center; padding: 40px; color: var(--text-muted);">
        載入中...
      </div>

      <div v-else-if="logs.length === 0" style="text-align: center; padding: 40px; color: var(--text-muted);">
        查無符合條件的審計日誌。
      </div>

      <div v-else class="table-container">
        <table class="admin-table">
          <thead>
            <tr>
              <th>時間戳記</th>
              <th>事件類型</th>
              <th>操作人員 (Actor)</th>
              <th>目標對象 (Target)</th>
              <th>事件詳細說明</th>
              <th>來源 IP</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="l in logs" :key="l.id">
              <td style="font-size: 0.8rem; font-family: monospace; white-space: nowrap;">
                {{ formatDate(l.timestamp) }}
              </td>
              <td>
                <span class="badge" :class="getEventBadgeClass(l.eventType)">
                  {{ l.eventType }}
                </span>
              </td>
              <td style="font-weight: 600;">{{ l.actor }}</td>
              <td style="font-family: monospace;">{{ l.target }}</td>
              <td style="font-size: 0.85rem; max-width: 320px;">{{ l.details }}</td>
              <td style="font-size: 0.8rem; color: var(--text-muted);">{{ l.ipAddress }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
