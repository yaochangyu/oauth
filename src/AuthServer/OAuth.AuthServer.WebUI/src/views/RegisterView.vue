<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { register, type ApiError } from '../api/client'

const router = useRouter()

const email = ref('')
const password = ref('')
const displayName = ref('')
const errorMessage = ref<string | null>(null)
const successMessage = ref<string | null>(null)
const submitting = ref(false)

async function onSubmit() {
  errorMessage.value = null
  successMessage.value = null
  submitting.value = true
  try {
    await register({
      email: email.value,
      password: password.value,
      displayName: displayName.value || null,
    })
    successMessage.value = '註冊成功，請登入'
    setTimeout(() => router.push({ path: '/login' }), 1000)
  } catch (e) {
    const err = e as ApiError
    errorMessage.value = err.message ?? '註冊失敗'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="card">
    <h1>註冊新帳號</h1>

    <p v-if="errorMessage" class="error-message" data-testid="error-message">{{ errorMessage }}</p>
    <p v-if="successMessage" data-testid="success-message">{{ successMessage }}</p>

    <form @submit.prevent="onSubmit">
      <div class="form-group">
        <label for="email">Email</label>
        <input id="email" name="email" type="email" v-model="email" required autocomplete="email" />
      </div>
      <div class="form-group">
        <label for="password">密碼</label>
        <input id="password" name="password" type="password" v-model="password" required autocomplete="new-password" />
      </div>
      <div class="form-group">
        <label for="displayName">顯示名稱（選填）</label>
        <input id="displayName" name="displayName" type="text" v-model="displayName" autocomplete="nickname" />
      </div>
      <button type="submit" :disabled="submitting">註冊</button>
    </form>

    <p class="links">
      已經有帳號？<RouterLink to="/login">前往登入</RouterLink>
    </p>
  </div>
</template>
