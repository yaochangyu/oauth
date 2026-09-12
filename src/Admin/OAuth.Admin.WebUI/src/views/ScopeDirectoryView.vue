<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type ScopeDetail } from '../api/admin';

const scopes = ref<ScopeDetail[]>([]);
const loading = ref(false);
const searchKeyword = ref('');
const errorMessage = ref('');
const successMessage = ref('');

// Create/Edit Modal State
const showModal = ref(false);
const isEditing = ref(false);
const currentId = ref('');
const formName = ref('');
const formDisplayName = ref('');
const formDescription = ref('');
const formResources = ref('');
const formIsSensitive = ref(false);

async function loadScopes() {
  loading.value = true;
  errorMessage.value = '';
  try {
    scopes.value = await adminApi.getScopes(searchKeyword.value || undefined);
  } catch (err: any) {
    errorMessage.value = err.message || '載入 Scope 矩陣失敗';
  } finally {
    loading.value = false;
  }
}

function openCreateModal() {
  isEditing.value = false;
  currentId.value = '';
  formName.value = '';
  formDisplayName.value = '';
  formDescription.value = '';
  formResources.value = '';
  formIsSensitive.value = false;
  showModal.value = true;
}

function openEditModal(s: ScopeDetail) {
  isEditing.value = true;
  currentId.value = s.id;
  formName.value = s.name;
  formDisplayName.value = s.displayName;
  formDescription.value = s.description;
  formResources.value = s.resources.join(', ');
  formIsSensitive.value = s.isSensitive;
  showModal.value = true;
}

async function saveScope() {
  if (!formName.value.trim()) {
    alert('Scope 名稱為必填');
    return;
  }
  const resources = formResources.value.split(',').map(r => r.trim()).filter(Boolean);

  try {
    if (isEditing.value) {
      await adminApi.updateScope(currentId.value, {
        displayName: formDisplayName.value,
        description: formDescription.value,
        resources,
        isSensitive: formIsSensitive.value
      });
      successMessage.value = `已更新 Scope「${formName.value}」！`;
    } else {
      await adminApi.createScope({
        name: formName.value.trim(),
        displayName: formDisplayName.value,
        description: formDescription.value,
        resources,
        isSensitive: formIsSensitive.value
      });
      successMessage.value = `已成功建立 Scope「${formName.value}」！`;
    }
    showModal.value = false;
    await loadScopes();
  } catch (err: any) {
    alert(err.message || '儲存 Scope 失敗');
  }
}

async function deleteScope(s: ScopeDetail) {
  if (!confirm(`確定要刪除 Scope「${s.name}」嗎？`)) return;
  try {
    await adminApi.deleteScope(s.id);
    successMessage.value = `已刪除 Scope「${s.name}」！`;
    await loadScopes();
  } catch (err: any) {
    errorMessage.value = err.message || '刪除失敗';
  }
}

onMounted(() => {
  loadScopes();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">全域 Scope 矩陣設定 (Scope Directory)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            定義 OAuth 2.0 授權權限範疇，區分「一般權限」與「高敏感權限（需資安審批）」
          </p>
        </div>
        <button class="btn btn-primary" @click="openCreateModal">+ 新增 Scope</button>
      </div>

      <div class="filter-bar">
        <input
          v-model="searchKeyword"
          type="text"
          class="input-text"
          placeholder="搜尋 Scope 名稱、顯示名稱..."
          @keyup.enter="loadScopes"
        />
        <button class="btn btn-primary" @click="loadScopes">搜尋</button>
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

      <div v-else class="table-container">
        <table class="admin-table">
          <thead>
            <tr>
              <th>Scope 名稱</th>
              <th>顯示名稱</th>
              <th>描述</th>
              <th>對應資源 (Resources)</th>
              <th>權限層級</th>
              <th style="text-align: right;">維護操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="s in scopes" :key="s.id">
              <td>
                <span style="font-weight: 600; font-family: monospace;">{{ s.name }}</span>
              </td>
              <td>{{ s.displayName || '-' }}</td>
              <td style="color: var(--text-muted); font-size: 0.85rem; max-width: 250px;">
                {{ s.description || '-' }}
              </td>
              <td>
                <span v-for="r in s.resources" :key="r" class="badge badge-sandbox" style="margin-right: 4px;">
                  {{ r }}
                </span>
                <span v-if="s.resources.length === 0" style="color: var(--text-muted); font-size: 0.8rem;">-</span>
              </td>
              <td>
                <span v-if="s.isSensitive" class="badge badge-sensitive">🔥 敏感權限 (Sensitive)</span>
                <span v-else class="badge badge-standard">一般權限 (Standard)</span>
              </td>
              <td style="text-align: right;">
                <div style="display: flex; gap: 6px; justify-content: flex-end;">
                  <button class="btn btn-outline" @click="openEditModal(s)">編輯</button>
                  <button class="btn btn-danger" @click="deleteScope(s)">刪除</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="modal-backdrop" @click.self="showModal = false">
      <div class="modal">
        <div class="modal-header">{{ isEditing ? '編輯 Scope' : '新增全域 Scope' }}</div>
        <div class="modal-body">
          <div>
            <label style="font-size: 0.85rem; font-weight: 600;">Scope 名稱 (唯一識別碼)：</label>
            <input
              v-model="formName"
              type="text"
              class="input-text"
              style="width: 100%; margin-top: 4px;"
              placeholder="例如：financial_records"
              :disabled="isEditing"
            />
          </div>

          <div>
            <label style="font-size: 0.85rem; font-weight: 600;">顯示名稱：</label>
            <input
              v-model="formDisplayName"
              type="text"
              class="input-text"
              style="width: 100%; margin-top: 4px;"
              placeholder="例如：財務紀錄讀取權限"
            />
          </div>

          <div>
            <label style="font-size: 0.85rem; font-weight: 600;">描述：</label>
            <textarea
              v-model="formDescription"
              rows="3"
              class="input-text"
              style="width: 100%; margin-top: 4px;"
              placeholder="權限詳細用途與說明"
            ></textarea>
          </div>

          <div>
            <label style="font-size: 0.85rem; font-weight: 600;">關聯資源 (逗號分隔)：</label>
            <input
              v-model="formResources"
              type="text"
              class="input-text"
              style="width: 100%; margin-top: 4px;"
              placeholder="例如：financial_api, audit_api"
            />
          </div>

          <div style="display: flex; align-items: center; gap: 8px; margin-top: 6px;">
            <input id="sensitive-check" v-model="formIsSensitive" type="checkbox" style="width: 18px; height: 18px;" />
            <label for="sensitive-check" style="font-size: 0.9rem; font-weight: 600; color: #be185d; cursor: pointer;">
              標記為高敏感權限 (Sensitive Scope，需管理員額外審查)
            </label>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showModal = false">取消</button>
          <button class="btn btn-primary" @click="saveScope">儲存</button>
        </div>
      </div>
    </div>
  </div>
</template>
