<script setup lang="ts">
import { useRoute } from 'vue-router';

const route = useRoute();
</script>

<template>
  <div class="app-container">
    <aside class="sidebar">
      <div class="sidebar-brand">
        <span>🔐 OAuth Admin</span>
        <span class="badge-admin">PROD</span>
      </div>

      <ul class="nav-menu">
        <li class="nav-item">
          <router-link to="/reviews">
            <span>📋</span>
            <span>★ 應用審批工作台</span>
          </router-link>
        </li>
        <li class="nav-item">
          <router-link to="/apps">
            <span>📱</span>
            <span>全域應用程式目錄</span>
          </router-link>
        </li>
        <li class="nav-item">
          <router-link to="/users">
            <span>👥</span>
            <span>使用者帳號管理</span>
          </router-link>
        </li>
        <li class="nav-item">
          <router-link to="/roles">
            <span>🏷️</span>
            <span>角色權限管理</span>
          </router-link>
        </li>
        <li class="nav-item">
          <router-link to="/scopes">
            <span>🎯</span>
            <span>Scope 矩陣設定</span>
          </router-link>
        </li>
        <li class="nav-item">
          <router-link to="/audit-logs">
            <span>📜</span>
            <span>授權審計日誌</span>
          </router-link>
        </li>
      </ul>
    </aside>

    <main class="main-content">
      <header class="topbar">
        <div class="topbar-title">{{ route.meta.title || '管理員後台' }}</div>
        <div class="topbar-user">
          <span>管理員：<strong>admin</strong></span>
          <span class="badge badge-approved">Administrator</span>
        </div>
      </header>

      <div class="page-body">
        <router-view />
      </div>
    </main>
  </div>
</template>
