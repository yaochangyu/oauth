import { getAccessToken } from '../auth/oidc'

export interface UserProfile {
  userId: string
  email: string
  displayName?: string
  avatarUrl?: string
  emailConfirmed: boolean
  isTwoFactorEnabled: boolean
}

export interface UpdateProfilePayload {
  displayName?: string
  avatarUrl?: string
}

const API_BASE = '/api/v1/account/profile'

async function authFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAccessToken()
  const headers = new Headers(options.headers || {})
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  return fetch(url, { ...options, headers })
}

export const profileApi = {
  async getProfile(): Promise<UserProfile> {
    const res = await authFetch(API_BASE)
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '載入個人資料失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },

  async updateProfile(payload: UpdateProfilePayload): Promise<UserProfile> {
    const res = await authFetch(API_BASE, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '更新個人資料失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },
}
