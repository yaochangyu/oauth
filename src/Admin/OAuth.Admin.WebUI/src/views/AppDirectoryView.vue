<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type AppDetail, type CreateAppPayload, type UpdateAppPayload } from '../api/admin';

const apps = ref<AppDetail[]>([]);
const loading = ref(false);
const filterStatus = ref('');
const searchKeyword = ref('');
const errorMessage = ref('');
const successMessage = ref('');

// App Modal (Create & Edit)
const showModal = ref(false);
const isEditing = ref(false);
const saving = ref(false);

const form = ref<{
  id: string;
  clientId: string;
  displayName: string;
  clientType: string;
  consentType: string;
  clientSecret: string;
  developer: string;
  redirectUris: string[];
  postLogoutRedirectUris: string[];
  permissions: string[];
  requirements: string[];
}>({
  id: '',
  clientId: '',
  displayName: '',
  clientType: 'confidential',
  consentType: 'explicit',
  clientSecret: '',
  developer: '',
  redirectUris: [],
  postLogoutRedirectUris: [],
  permissions: [],
  requirements: []
});

const grantTypes = [
  { value: 'gt:authorization_code', label: '授權碼 (Authorization Code)' },
  { value: 'gt:client_credentials', label: '用戶端憑證 (Client Credentials)' },
  { value: 'gt:refresh_token', label: '重新整理權杖 (Refresh Token)' },
  { value: 'gt:urn:ietf:params:oauth:grant-type:device_code', label: '裝置授權碼 (Device Code)' }
];

const endpointOptions = [
  { value: 'ept:authorization', label: '授權端點 (/connect/authorize)' },
  { value: 'ept:token', label: '權杖端點 (/connect/token)' },
  { value: 'ept:logout', label: '登出端點 (/connect/logout)' },
  { value: 'ept:userinfo', label: '用戶資訊端點 (/connect/userinfo)' },
  { value: 'ept:revocation', label: '吊銷端點 (/connect/revocation)' },
  { value: 'ept:introspection', label: '內省端點 (/connect/introspect)' }
];

const scopeOptions = [
  { value: 'scp:openid', label: 'openid' },
  { value: 'scp:profile', label: 'profile' },
  { value: 'scp:email', label: 'email' },
  { value: 'scp:roles', label: 'roles' },
  { value: 'scp:api', label: 'api' }
];

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

function openCreateModal() {
  isEditing.value = false;
  form.value = {
    id: '',
    clientId: '',
    displayName: '',
    clientType: 'confidential',
    consentType: 'explicit',
    clientSecret: '',
    developer: '',
    redirectUris: ['https://'],
    postLogoutRedirectUris: ['https://'],
    permissions: ['gt:authorization_code', 'gt:refresh_token', 'rst:code', 'ept:authorization', 'ept:token', 'scp:openid', 'scp:profile', 'scp:email'],
    requirements: ['ft:pkce']
  };
  showModal.value = true;
}

function openEditModal(app: AppDetail) {
  isEditing.value = true;
  form.value = {
    id: app.id,
    clientId: app.clientId,
    displayName: app.displayName,
    clientType: app.clientType || 'confidential',
    consentType: app.consentType || 'explicit',
    clientSecret: '',
    developer: app.developer || '',
    redirectUris: app.redirectUris && app.redirectUris.length > 0 ? [...app.redirectUris] : ['https://'],
    postLogoutRedirectUris: app.postLogoutRedirectUris && app.postLogoutRedirectUris.length > 0 ? [...app.postLogoutRedirectUris] : [],
    permissions: [...(app.permissions || [])],
    requirements: [...(app.requirements || [])]
  };
  showModal.value = true;
}

function addRedirectUri() {
  form.value.redirectUris.push('https://');
}

function removeRedirectUri(index: number) {
  form.value.redirectUris.splice(index, 1);
}

function addPostLogoutUri() {
  form.value.postLogoutRedirectUris.push('https://');
}

function removePostLogoutUri(index: number) {
  form.value.postLogoutRedirectUris.splice(index, 1);
}

function togglePermission(perm: string) {
  const idx = form.value.permissions.indexOf(perm);
  if (idx > -1) {
    form.value.permissions.splice(idx, 1);
  } else {
    form.value.permissions.push(perm);
  }
}

function toggleRequirement(req: string) {
  const idx = form.value.requirements.indexOf(req);
  if (idx > -1) {
    form.value.requirements.splice(idx, 1);
  } else {
    form.value.requirements.push(req);
  }
}

async function saveApp() {
  if (!form.value.clientId.trim() && !isEditing.value) {
    alert('請填寫 Client ID');
    return;
  }

  // 確保授權碼模式包含 rst:code
  if (form.value.permissions.includes('gt:authorization_code') && !form.value.permissions.includes('rst:code')) {
    form.value.permissions.push('rst:code');
  }

  const validRedirects = form.value.redirectUris.map(u => u.trim()).filter(u => u && u !== 'https://');
  const validPostLogouts = form.value.postLogoutRedirectUris.map(u => u.trim()).filter(u => u && u !== 'https://');

  saving.value = true;
  try {
    if (isEditing.value) {
      const payload: UpdateAppPayload = {
        displayName: form.value.displayName,
        clientType: form.value.clientType,
        consentType: form.value.consentType,
        developer: form.value.developer,
        redirectUris: validRedirects,
        postLogoutRedirectUris: validPostLogouts,
        permissions: form.value.permissions,
        requirements: form.value.requirements
      };
      if (form.value.clientSecret.trim()) {
        payload.clientSecret = form.value.clientSecret.trim();
      }
      await adminApi.updateApp(form.value.clientId, payload);
      successMessage.value = `已成功更新應用程式「${form.value.displayName || form.value.clientId}」！`;
    } else {
      const payload: CreateAppPayload = {
        clientId: form.value.clientId.trim(),
        displayName: form.value.displayName.trim(),
        clientType: form.value.clientType,
        consentType: form.value.consentType,
        clientSecret: form.value.clientSecret.trim() || undefined,
        developer: form.value.developer.trim() || undefined,
        redirectUris: validRedirects,
        postLogoutRedirectUris: validPostLogouts,
        permissions: form.value.permissions,
        requirements: form.value.requirements
      };
      await adminApi.createApp(payload);
      successMessage.value = `已成功建立應用程式「${form.value.displayName || form.value.clientId}」！`;
    }

    showModal.value = false;
    await loadApps();
  } catch (err: any) {
    alert(err.message || '儲存失敗');
  } finally {
    saving.value = false;
  }
}

async function deleteApp(app: AppDetail) {
  if (!confirm(`確定要刪除應用程式「${app.displayName || app.clientId}」嗎？\n此動作將永久刪除該 Client 配置，無法復原！`)) return;
  try {
    await adminApi.deleteApp(app.clientId);
    successMessage.value = `已成功刪除「${app.displayName || app.clientId}」！`;
    await loadApps();
  } catch (err: any) {
    errorMessage.value = err.message || '刪除失敗';
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
            管理全站 OAuth 2.0 / OIDC 用戶端應用程式（建立、編輯、刪除與安全停用/吊銷）
          </p>
        </div>
        <div>
          <button class="btn btn-primary" @click="openCreateModal">
            ＋ 建立應用程式
          </button>
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
                <div style="display: flex; gap: 6px; justify-content: flex-end; flex-wrap: wrap;">
                  <button class="btn btn-outline" @click="openEditModal(app)">編輯</button>
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
                  <button class="btn btn-danger" @click="deleteApp(app)">刪除</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create / Edit Application Modal -->
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal" style="max-width: 750px; max-height: 90vh; overflow-y: auto;">
        <div class="modal-header">
          {{ isEditing ? `編輯應用程式：${form.displayName || form.clientId}` : '建立新第三方應用程式' }}
        </div>
        <div class="modal-body">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 12px;">
            <div>
              <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">Client ID：</label>
              <input
                v-model="form.clientId"
                type="text"
                class="input-text"
                placeholder="例: custom-web-app"
                :disabled="isEditing"
                style="width: 100%;"
              />
            </div>
            <div>
              <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">顯示名稱 (Display Name)：</label>
              <input
                v-model="form.displayName"
                type="text"
                class="input-text"
                placeholder="例: 客戶端入口應用"
                style="width: 100%;"
              />
            </div>
          </div>

          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 12px; margin-bottom: 12px;">
            <div>
              <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">用戶端類型 (Client Type)：</label>
              <select v-model="form.clientType" class="select" style="width: 100%;">
                <option value="confidential">Confidential (機密用戶端 / 具備 Secret)</option>
                <option value="public">Public (公開用戶端 / SPA / 行動App)</option>
              </select>
            </div>
            <div>
              <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">同意授權類型 (Consent Type)：</label>
              <select v-model="form.consentType" class="select" style="width: 100%;">
                <option value="explicit">Explicit (需使用者同意)</option>
                <option value="implicit">Implicit (自動核准 / 內部應用)</option>
                <option value="external">External (外部授權)</option>
                <option value="systematic">Systematic (系統服務)</option>
              </select>
            </div>
          </div>

          <div v-if="form.clientType === 'confidential'" style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">Client Secret：</label>
            <input
              v-model="form.clientSecret"
              type="password"
              class="input-text"
              :placeholder="isEditing ? '留空表示維持既有密鑰' : '設定用戶端密鑰'"
              style="width: 100%;"
            />
          </div>

          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">開發者聯絡 Email：</label>
            <input
              v-model="form.developer"
              type="text"
              class="input-text"
              placeholder="例: dev@example.com"
              style="width: 100%;"
            />
          </div>

          <hr style="border: 0; border-top: 1px solid var(--border); margin: 16px 0;" />

          <!-- Redirect URIs -->
          <div style="margin-bottom: 12px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
              <label style="font-weight: 600; font-size: 0.85rem;">重新導向 URI 清單 (Redirect URIs)：</label>
              <button class="btn btn-outline" style="padding: 2px 8px; font-size: 0.8rem;" @click="addRedirectUri">＋ 新增 URI</button>
            </div>
            <div v-for="(_uri, idx) in form.redirectUris" :key="idx" style="display: flex; gap: 8px; margin-bottom: 6px;">
              <input v-model="form.redirectUris[idx]" type="text" class="input-text" style="flex: 1;" placeholder="https://app.example.com/callback" />
              <button class="btn btn-danger" style="padding: 4px 8px;" @click="removeRedirectUri(idx)">✕</button>
            </div>
          </div>

          <!-- Post Logout Redirect URIs -->
          <div style="margin-bottom: 12px;">
            <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
              <label style="font-weight: 600; font-size: 0.85rem;">登出重新導向 URI 清單 (Post-Logout Redirect URIs)：</label>
              <button class="btn btn-outline" style="padding: 2px 8px; font-size: 0.8rem;" @click="addPostLogoutUri">＋ 新增 URI</button>
            </div>
            <div v-for="(_uri, idx) in form.postLogoutRedirectUris" :key="idx" style="display: flex; gap: 8px; margin-bottom: 6px;">
              <input v-model="form.postLogoutRedirectUris[idx]" type="text" class="input-text" style="flex: 1;" placeholder="https://app.example.com/logout-callback" />
              <button class="btn btn-danger" style="padding: 4px 8px;" @click="removePostLogoutUri(idx)">✕</button>
            </div>
          </div>

          <hr style="border: 0; border-top: 1px solid var(--border); margin: 16px 0;" />

          <!-- Permissions -->
          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 8px;">授權流程類型 (Grant Types)：</label>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 6px;">
              <label v-for="g in grantTypes" :key="g.value" style="display: flex; align-items: center; gap: 6px; font-size: 0.85rem; cursor: pointer;">
                <input type="checkbox" :checked="form.permissions.includes(g.value)" @change="togglePermission(g.value)" />
                {{ g.label }}
              </label>
            </div>
          </div>

          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 8px;">開放端點 (Endpoints)：</label>
            <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 6px;">
              <label v-for="e in endpointOptions" :key="e.value" style="display: flex; align-items: center; gap: 6px; font-size: 0.85rem; cursor: pointer;">
                <input type="checkbox" :checked="form.permissions.includes(e.value)" @change="togglePermission(e.value)" />
                {{ e.label }}
              </label>
            </div>
          </div>

          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 8px;">允許 Scope：</label>
            <div style="display: flex; gap: 12px; flex-wrap: wrap;">
              <label v-for="s in scopeOptions" :key="s.value" style="display: flex; align-items: center; gap: 6px; font-size: 0.85rem; cursor: pointer;">
                <input type="checkbox" :checked="form.permissions.includes(s.value)" @change="togglePermission(s.value)" />
                {{ s.label }}
              </label>
            </div>
          </div>

          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 8px;">安全性要求 (Requirements)：</label>
            <div>
              <label style="display: flex; align-items: center; gap: 6px; font-size: 0.85rem; cursor: pointer;">
                <input type="checkbox" :checked="form.requirements.includes('ft:pkce')" @change="toggleRequirement('ft:pkce')" />
                強制要求 PKCE (Proof Key for Code Exchange)
              </label>
            </div>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showModal = false">取消</button>
          <button class="btn btn-primary" :disabled="saving" @click="saveApp">
            {{ saving ? '儲存中...' : (isEditing ? '儲存變更' : '建立應用程式') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
