<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { adminApi, type UserSummary, type UserDetail } from '../api/admin';

const users = ref<UserSummary[]>([]);
const loading = ref(false);
const searchKeyword = ref('');
const errorMessage = ref('');
const successMessage = ref('');

// Detail / Role Modal
const showDetailModal = ref(false);
const selectedUser = ref<UserDetail | null>(null);
const newRoleName = ref('');

async function loadUsers() {
  loading.value = true;
  errorMessage.value = '';
  try {
    users.value = await adminApi.getUsers(searchKeyword.value || undefined);
  } catch (err: any) {
    errorMessage.value = err.message || '載入使用者失敗';
  } finally {
    loading.value = false;
  }
}

async function lockout(user: UserSummary) {
  if (!confirm(`確定要凍結使用者「${user.userName}」的帳號嗎？\n凍結後該帳號將無法登入任何授權服務。`)) return;
  try {
    await adminApi.lockoutUser(user.id);
    successMessage.value = `已成功凍結「${user.userName}」！`;
    await loadUsers();
  } catch (err: any) {
    errorMessage.value = err.message || '凍結帳號失敗';
  }
}

async function unlock(user: UserSummary) {
  if (!confirm(`確定要解除「${user.userName}」的帳號凍結嗎？`)) return;
  try {
    await adminApi.unlockUser(user.id);
    successMessage.value = `已解除「${user.userName}」凍結！`;
    await loadUsers();
  } catch (err: any) {
    errorMessage.value = err.message || '解除凍結失敗';
  }
}

async function revokeSessions(user: UserSummary) {
  if (!confirm(`確定要強制登出「${user.userName}」的所有已連線工作階段嗎？`)) return;
  try {
    await adminApi.revokeSessions(user.id);
    successMessage.value = `已強制更新「${user.userName}」安全戳記並清除工作階段！`;
  } catch (err: any) {
    errorMessage.value = err.message || '強制登出失敗';
  }
}

async function openDetails(user: UserSummary) {
  try {
    selectedUser.value = await adminApi.getUser(user.id);
    showDetailModal.value = true;
  } catch (err: any) {
    errorMessage.value = err.message || '載入使用者詳細資訊失敗';
  }
}

async function addRole() {
  if (!selectedUser.value || !newRoleName.value.trim()) return;
  try {
    await adminApi.addUserRole(selectedUser.value.id, newRoleName.value.trim());
    selectedUser.value = await adminApi.getUser(selectedUser.value.id);
    newRoleName.value = '';
    await loadUsers();
  } catch (err: any) {
    alert(err.message || '新增角色失敗');
  }
}

async function removeRole(roleName: string) {
  if (!selectedUser.value) return;
  try {
    await adminApi.removeUserRole(selectedUser.value.id, roleName);
    selectedUser.value = await adminApi.getUser(selectedUser.value.id);
    await loadUsers();
  } catch (err: any) {
    alert(err.message || '移除角色失敗');
  }
}

onMounted(() => {
  loadUsers();
});
</script>

<template>
  <div>
    <div class="card">
      <div class="card-header">
        <div>
          <h2 class="card-title">全站使用者管理 (User Directory)</h2>
          <p style="color: var(--text-muted); font-size: 0.85rem; margin-top: 4px;">
            管理員凍結違規帳號、解除鎖定、強制登出已連線工作階段與角色權限指派
          </p>
        </div>
      </div>

      <div class="filter-bar">
        <input
          v-model="searchKeyword"
          type="text"
          class="input-text"
          placeholder="搜尋使用者帳號、Email..."
          @keyup.enter="loadUsers"
        />
        <button class="btn btn-primary" @click="loadUsers">搜尋</button>
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
              <th>使用者帳號</th>
              <th>Email</th>
              <th>角色</th>
              <th>狀態</th>
              <th style="text-align: right;">帳號控管操作</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in users" :key="u.id">
              <td>
                <div style="font-weight: 600; cursor: pointer; color: var(--primary);" @click="openDetails(u)">
                  {{ u.userName }}
                </div>
              </td>
              <td>{{ u.email }}</td>
              <td>
                <span v-for="r in u.roles" :key="r" class="badge badge-standard" style="margin-right: 4px;">
                  {{ r }}
                </span>
                <span v-if="u.roles.length === 0" style="color: var(--text-muted); font-size: 0.8rem;">一般用戶</span>
              </td>
              <td>
                <span v-if="u.isLocked" class="badge badge-locked">已凍結 (Locked)</span>
                <span v-else class="badge badge-active">正常 (Active)</span>
              </td>
              <td style="text-align: right;">
                <div style="display: flex; gap: 6px; justify-content: flex-end;">
                  <button class="btn btn-outline" @click="openDetails(u)">詳情/角色</button>
                  <button v-if="!u.isLocked" class="btn btn-danger" @click="lockout(u)">凍結帳號</button>
                  <button v-else class="btn btn-success" @click="unlock(u)">解除凍結</button>
                  <button class="btn btn-warning" @click="revokeSessions(u)">強制登出</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- User Detail / Role Modal -->
    <div v-if="showDetailModal && selectedUser" class="modal-backdrop" @click.self="showDetailModal = false">
      <div class="modal" style="max-width: 600px;">
        <div class="modal-header">使用者詳細資料與權限設定</div>
        <div class="modal-body">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 8px; font-size: 0.9rem;">
            <div><strong>帳號：</strong> {{ selectedUser.userName }}</div>
            <div><strong>Email：</strong> {{ selectedUser.email }}</div>
            <div><strong>信箱已驗證：</strong> {{ selectedUser.emailConfirmed ? '是' : '否' }}</div>
            <div><strong>帳號狀態：</strong> {{ selectedUser.isLocked ? '已凍結' : '正常' }}</div>
          </div>

          <hr style="border: 0; border-top: 1px solid var(--border); margin: 12px 0;" />

          <div>
            <div style="font-weight: 600; font-size: 0.9rem; margin-bottom: 8px;">已指派角色：</div>
            <div style="display: flex; gap: 6px; flex-wrap: wrap; margin-bottom: 12px;">
              <span
                v-for="r in selectedUser.roles"
                :key="r"
                class="badge badge-standard"
                style="display: inline-flex; align-items: center; gap: 6px; padding: 6px 10px;"
              >
                {{ r }}
                <span style="cursor: pointer; font-weight: bold;" @click="removeRole(r)">×</span>
              </span>
              <span v-if="selectedUser.roles.length === 0" style="color: var(--text-muted); font-size: 0.85rem;">尚未指派任何角色</span>
            </div>

            <div style="display: flex; gap: 8px;">
              <input
                v-model="newRoleName"
                type="text"
                class="input-text"
                placeholder="輸入新角色名稱 (例: Administrator)"
                style="flex: 1;"
              />
              <button class="btn btn-primary" @click="addRole">加入角色</button>
            </div>
          </div>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showDetailModal = false">關閉</button>
        </div>
      </div>
    </div>
  </div>
</template>
