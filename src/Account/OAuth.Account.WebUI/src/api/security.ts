import { getAccessToken } from '../auth/oidc'

export interface TwoFactorStatus {
  isTwoFactorEnabled: boolean
  hasAuthenticator: boolean
}

export interface Generate2FaKeyResult {
  sharedKey: string
  authenticatorUri: string
}

export interface ChangePasswordPayload {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

const API_BASE = '/api/v1/account/security'

async function authFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAccessToken()
  const headers = new Headers(options.headers || {})
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  return fetch(url, { ...options, headers })
}

export const securityApi = {
  async getTwoFactorStatus(): Promise<TwoFactorStatus> {
    const res = await authFetch(`${API_BASE}/2fa-status`)
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '取得 2FA 狀態失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },

  async changePassword(payload: ChangePasswordPayload): Promise<{ message: string }> {
    const res = await authFetch(`${API_BASE}/change-password`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '修改密碼失敗' }))
      const details = err.errors ? `: ${err.errors.join(', ')}` : ''
      throw new Error((err.message || `HTTP ${res.status}`) + details)
    }
    return res.json()
  },

  async generateTwoFactorKey(): Promise<Generate2FaKeyResult> {
    const res = await authFetch(`${API_BASE}/2fa/generate-key`, {
      method: 'POST',
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '產生 2FA 金鑰失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },

  async verifyAndEnableTwoFactor(code: string): Promise<{ message: string }> {
    const res = await authFetch(`${API_BASE}/2fa/verify-and-enable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ code }),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '驗證 2FA 失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },

  async disableTwoFactor(): Promise<{ message: string }> {
    const res = await authFetch(`${API_BASE}/2fa/disable`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({}),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '停用 2FA 失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },
}
