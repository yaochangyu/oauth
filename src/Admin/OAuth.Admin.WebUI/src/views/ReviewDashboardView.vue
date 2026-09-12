<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type AppDetail } from '../api/admin';

const pendingApps = ref<AppDetail[]>([]);
const loading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

// Reject Modal State
const showRejectModal = ref(false);
const selectedApp = ref<AppDetail | null>(null);
const rejectReason = ref('');

async function loadPendingApps() {
  loading.value = true;
  errorMessage.value = '';
  try {
    pendingApps.value = await adminApi.getPendingApps();
  } catch (err: any) {
    errorMessage.value = err.message || '載入待審核清單失敗';
  } finally {
    loading.value = false;
  }
}

async function approve(app: AppDetail) {
  if (!confirm(`確定要核准應用程式「${app.displayName || app.clientId}」上線嗎？`)) return;
  try {
    await adminApi.approveApp(app.clientId);
    successMessage.value = `已成功核准「${app.displayName || app.clientId}」！`;
    await loadPendingApps();
  } catch (err: any) {
    errorMessage.value = err.message || '核准失敗';
  }
}

function openRejectModal(app: AppDetail) {
  selectedApp.value = app;
  rejectReason.value = '';
  showRejectModal.value = true;
}

async function submitReject() {
  if (!selectedApp.value) return;
  if (!rejectReason.value.trim()) {
    alert('請填寫駁回原因');
    return;
  }
  try {
    await adminApi.rejectApp(selectedApp.value.clientId, rejectReason.value.trim());
    successMessage.value = `已駁回「${selectedApp.value.displayName || selectedApp.value.clientId}」！`;
    showRejectModal.value = false;
    await loadPendingApps();
  } catch (err: any) {
    errorMessage.value = err.message || '駁回失敗';
  }
}

onMounted(() => {
  loadPendingApps();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">★ 第三方應用審批工作台 (Review Workbench)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            審核開發者於 DevPortal 提交上線的 OAuth 應用程式與請求 Scope
          </p>
        </div>
        <button class="btn btn-outline" @click="loadPendingApps">重新整理</button>
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

      <div v-else-if="pendingApps.length === 0" style="text-align: center; padding: 40px; color: var(--text-muted);">
        目前沒有待審核的應用程式。所有申請皆已完成審查。
      </div>

      <div v-else class="table-container">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Client ID / 名稱</th>
              <th>開發者</th>
              <th>要求 Scopes / 權限</th>
              <th>Redirect URIs</th>
              <th>申請理由</th>
              <th>狀態</th>
              <th style="text-align: right;">審批操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="app in pendingApps" :key="app.id">
              <td>
                <div style="font-weight: 600;">{{ app.displayName || app.clientId }}</div>
                <div style="font-size: 0.8rem; color: var(--text-muted);">{{ app.clientId }}</div>
              </td>
              <td>{{ app.developer || '未知' }}</td>
              <td>
                <div style="display: flex; gap: 4px; flex-wrap: wrap;">
                  <span
                    v-for="p in app.permissions"
                    :key="p"
                    class="badge"
                    :class="p.includes('sensitive') ? 'badge-sensitive' : 'badge-standard'"
                  >
                    {{ p.replace('scp:', '').replace('ept:', '') }}
                  </span>
                </div>
              </td>
              <td>
                <div v-for="uri in app.redirectUris" :key="uri" style="font-size: 0.8rem; word-break: break-all;">
                  {{ uri }}
                </div>
              </td>
              <td style="max-width: 220px; font-size: 0.85rem;">
                {{ app.requestReason || '（未附理由）' }}
              </td>
              <td>
                <span class="badge badge-inreview">InReview 待審核</span>
              </td>
              <td style="text-align: right;">
                <div style="display: flex; gap: 8px; justify-content: flex-end;">
                  <button class="btn btn-success" @click="approve(app)">核准通過</button>
                  <button class="btn btn-danger" @click="openRejectModal(app)">駁回修改</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Reject Modal -->
    <div v-if="showRejectModal" class="modal-backdrop" @click.self="showRejectModal = false">
      <div class="modal">
        <div class="modal-header">駁回應用程式審查申請</div>
        <div class="modal-body">
          <p style="font-size: 0.9rem; color: var(--text-muted);">
            應用程式：<strong>{{ selectedApp?.displayName || selectedApp?.clientId }}</strong>
          </p>
          <label style="font-size: 0.85rem; font-weight: 600;">請輸入駁回原因（開發者將於 DevPortal 看到此備註）：</label>
          <textarea
            v-model="rejectReason"
            rows="4"
            class="input-text"
            style="width: 100%;"
            placeholder="例如：Redirect URI 不符合 HTTPS 規範，請修正後重新送審"
          ></textarea>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showRejectModal = false">取消</button>
          <button class="btn btn-danger" @click="submitReject">確認駁回</button>
        </div>
      </div>
    </div>
  </div>
</template>
