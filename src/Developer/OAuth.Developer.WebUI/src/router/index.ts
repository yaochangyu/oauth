import { createRouter, createWebHistory } from 'vue-router'
import AppListView from '../views/AppListView.vue'
import AppCreateView from '../views/AppCreateView.vue'
import AppDetailView from '../views/AppDetailView.vue'
import KeyManagementView from '../views/KeyManagementView.vue'
import SandboxTestView from '../views/SandboxTestView.vue'
import DeveloperRegisterView from '../views/DeveloperRegisterView.vue'

const routes = [
  {
    path: '/',
    redirect: '/apps',
  },
  {
    path: '/register',
    name: 'DeveloperRegister',
    component: DeveloperRegisterView,
  },
  {
    path: '/apps',
    name: 'AppList',
    component: AppListView,
  },
  {
    path: '/apps/create',
    name: 'AppCreate',
    component: AppCreateView,
  },
  {
    path: '/apps/:id',
    name: 'AppDetail',
    component: AppDetailView,
  },
  {
    path: '/apps/:id/keys',
    name: 'KeyManagement',
    component: KeyManagementView,
  },
  {
    path: '/sandbox',
    name: 'SandboxTest',
    component: SandboxTestView,
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})
