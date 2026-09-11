import { createRouter, createWebHistory } from 'vue-router'
import LoginView from '../views/LoginView.vue'
import ConsentView from '../views/ConsentView.vue'
import RegisterView from '../views/RegisterView.vue'
import ErrorView from '../views/ErrorView.vue'

// 路由別名：同時相容新的 Headless 路徑與舊 Razor Pages 路徑，
// 讓既有透過 /Account/Login、/Connect/Consent 導頁的呼叫端（含既有 Playwright 測試）不受影響。
const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    { path: '/Account/Login', name: 'login-legacy', component: LoginView },
    { path: '/consent', name: 'consent', component: ConsentView },
    { path: '/Connect/Consent', name: 'consent-legacy', component: ConsentView },
    { path: '/register', name: 'register', component: RegisterView },
    { path: '/error', name: 'error', component: ErrorView },
    { path: '/', redirect: '/login' },
    { path: '/:pathMatch(.*)*', redirect: '/error' },
  ],
})

export default router
