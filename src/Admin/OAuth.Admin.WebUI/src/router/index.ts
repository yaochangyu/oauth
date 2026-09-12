import { createRouter, createWebHistory } from 'vue-router';
import ReviewDashboardView from '../views/ReviewDashboardView.vue';
import AppDirectoryView from '../views/AppDirectoryView.vue';
import UserDirectoryView from '../views/UserDirectoryView.vue';
import ScopeDirectoryView from '../views/ScopeDirectoryView.vue';
import AuditLogsView from '../views/AuditLogsView.vue';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      redirect: '/reviews'
    },
    {
      path: '/reviews',
      name: 'reviews',
      component: ReviewDashboardView,
      meta: { title: '第三方 App 審批工作台' }
    },
    {
      path: '/apps',
      name: 'apps',
      component: AppDirectoryView,
      meta: { title: '全域應用程式目錄' }
    },
    {
      path: '/users',
      name: 'users',
      component: UserDirectoryView,
      meta: { title: '全站使用者管理' }
    },
    {
      path: '/scopes',
      name: 'scopes',
      component: ScopeDirectoryView,
      meta: { title: 'Scope 矩陣設定' }
    },
    {
      path: '/audit-logs',
      name: 'audit-logs',
      component: AuditLogsView,
      meta: { title: '授權與安全審計日誌' }
    }
  ]
});

router.afterEach((to) => {
  if (to.meta?.title) {
    document.title = `${to.meta.title} - OAuth 管理員後台`;
  }
});

export default router;
