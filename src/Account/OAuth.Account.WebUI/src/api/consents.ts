import { getAccessToken } from '../auth/oidc'

export interface AuthorizedApp {
  authorizationId: string
  clientId: string
  clientDisplayName: string
  scopes: string[]
  authorizedAt: string
}

const API_BASE = '/api/v1/account/consents'

async function authFetch(url: string, options: RequestInit = {}): Promise<Response> {
  const token = await getAccessToken()
  const headers = new Headers(options.headers || {})
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }
  return fetch(url, { ...options, headers })
}

export const consentsApi = {
  async getAuthorizedApps(): Promise<AuthorizedApp[]> {
    const res = await authFetch(API_BASE)
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '取得授權應用程式清單失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
    return res.json()
  },

  async revokeAppConsent(authorizationId: string): Promise<void> {
    const res = await authFetch(`${API_BASE}/${authorizationId}`, {
      method: 'DELETE',
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ message: '撤銷授權失敗' }))
      throw new Error(err.message || `HTTP ${res.status}`)
    }
  },
}
