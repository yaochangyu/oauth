import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'
import ProfileView from '../views/ProfileView.vue'
import SecurityView from '../views/SecurityView.vue'
import AuthorizedAppsView from '../views/AuthorizedAppsView.vue'
import CallbackView from '../views/CallbackView.vue'

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/profile',
  },
  {
    path: '/profile',
    name: 'profile',
    component: ProfileView,
  },
  {
    path: '/security',
    name: 'security',
    component: SecurityView,
  },
  {
    path: '/apps',
    name: 'apps',
    component: AuthorizedAppsView,
  },
  {
    path: '/callback',
    name: 'callback',
    component: CallbackView,
  },
]

export const router = createRouter({
  history: createWebHistory(),
  routes,
})
