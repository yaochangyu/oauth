<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type RoleSummary } from '../api/admin';

const roles = ref<RoleSummary[]>([]);
const loading = ref(false);
const searchKeyword = ref('');
const errorMessage = ref('');
const successMessage = ref('');

// Add Role Modal
const showCreateModal = ref(false);
const newRoleName = ref('');
const creating = ref(false);

async function loadRoles() {
  loading.value = true;
  errorMessage.value = '';
  try {
    roles.value = await adminApi.getRoles(searchKeyword.value || undefined);
  } catch (err: any) {
    errorMessage.value = err.message || '載入角色清單失敗';
  } finally {
    loading.value = false;
  }
}

async function createRole() {
  if (!newRoleName.value.trim()) return;
  creating.value = true;
  try {
    await adminApi.createRole(newRoleName.value.trim());
    successMessage.value = `已成功建立角色「${newRoleName.value.trim()}」！`;
    newRoleName.value = '';
    showCreateModal.value = false;
    await loadRoles();
  } catch (err: any) {
    alert(err.message || '建立角色失敗');
  } finally {
    creating.value = false;
  }
}

async function deleteRole(role: RoleSummary) {
  if (!confirm(`確定要刪除角色「${role.name}」嗎？\n刪除後擁有此角色的使用者將失去對應角色權限。`)) return;
  try {
    await adminApi.deleteRole(role.name);
    successMessage.value = `已成功刪除角色「${role.name}」！`;
    await loadRoles();
  } catch (err: any) {
    errorMessage.value = err.message || '刪除角色失敗';
  }
}

onMounted(() => {
  loadRoles();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">全域角色管理 (Role Directory)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            維護全站身分授權系統中的 Identity 角色清單，支援新增、刪除與全域查詢
          </p>
        </div>
        <div>
          <button class="btn btn-primary" @click="showCreateModal = true">
            ＋ 新增角色
          </button>
        </div>
      </div>

      <div class="filter-bar">
        <input
          v-model="searchKeyword"
          type="text"
          class="input-text"
          placeholder="搜尋角色名稱..."
          @keyup.enter="loadRoles"
        />
        <button class="btn btn-primary" @click="loadRoles">搜尋</button>
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

      <div v-else-if="roles.length === 0" style="text-align: center; padding: 40px; color: var(--text-muted);">
        查無符合條件的角色。
      </div>

      <div v-else class="table-container">
        <table class="admin-table">
          <thead>
            <tr>
              <th>角色名稱 (Role Name)</th>
              <th>角色識別碼 (Role ID)</th>
              <th style="text-align: right;">維運操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="r in roles" :key="r.id">
              <td>
                <div style="font-weight: 600; display: flex; align-items: center; gap: 8px;">
                  <span class="badge badge-standard">{{ r.name }}</span>
                </div>
              </td>
              <td style="font-size: 0.85rem; color: var(--text-muted);">{{ r.id }}</td>
              <td style="text-align: right;">
                <button class="btn btn-danger" @click="deleteRole(r)">刪除角色</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Create Role Modal -->
    <div v-if="showCreateModal" class="modal-backdrop" @click.self="showCreateModal = false">
      <div class="modal" style="max-width: 480px;">
        <div class="modal-header">新增角色</div>
        <div class="modal-body">
          <div style="margin-bottom: 12px;">
            <label style="display: block; font-weight: 600; font-size: 0.85rem; margin-bottom: 4px;">角色名稱：</label>
            <input
              v-model="newRoleName"
              type="text"
              class="input-text"
              placeholder="例: Administrator, Auditor, Developer..."
              style="width: 100%;"
              @keyup.enter="createRole"
            />
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showCreateModal = false">取消</button>
          <button class="btn btn-primary" :disabled="creating || !newRoleName.trim()" @click="createRole">
            {{ creating ? '建立中...' : '確認新增' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
