<script setup lang="ts">
import { ref } from 'vue'
import { useRoute } from 'vue-router'
import { login, type ApiError } from '../api/client'

const route = useRoute()
const returnUrl = typeof route.query.returnUrl === 'string' ? route.query.returnUrl : undefined

const userName = ref('')
const password = ref('')
const errorMessage = ref<string | null>(null)
const submitting = ref(false)

async function onSubmit() {
  errorMessage.value = null
  submitting.value = true
  try {
    const result = await login({
      userName: userName.value,
      password: password.value,
      returnUrl,
    })
    window.location.href = result.returnUrl
  } catch (e) {
    const err = e as ApiError
    errorMessage.value = err.message ?? '帳號或密碼錯誤'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="card">
    <h1>登入</h1>

    <p v-if="errorMessage" class="error-message" data-testid="error-message">{{ errorMessage }}</p>

    <form @submit.prevent="onSubmit">
      <div class="form-group">
        <label for="userName">帳號或 Email</label>
        <input id="userName" name="userName" type="text" v-model="userName" required autocomplete="username" />
      </div>
      <div class="form-group">
        <label for="password">密碼</label>
        <input id="password" name="password" type="password" v-model="password" required autocomplete="current-password" />
      </div>
      <button type="submit" :disabled="submitting">登入</button>
    </form>

    <p class="links">
      還沒有帳號？<RouterLink to="/register">註冊新帳號</RouterLink>
    </p>
  </div>
</template>
