import { getAccessToken } from '../auth/oidc'

export interface AppResponse {
  id: string
  clientId: string
  displayName: string
  appType: 'Web' | 'SPA' | 'Mobile'
  clientType: 'confidential' | 'public'
  description?: string
  logoUrl?: string
  status: 'Sandbox' | 'InReview' | 'Approved' | 'Rejected'
  requiresPkce: boolean
  clientSecret?: string
  redirectUris: string[]
  postLogoutRedirectUris: string[]
  requestedScopes: string[]
  createdAt?: string
  reviewSubmittedAt?: string
}

export interface CreateAppRequest {
  displayName: string
  appType: 'Web' | 'SPA' | 'Mobile'
  description?: string
  logoUrl?: string
  redirectUris: string[]
  postLogoutRedirectUris?: string[]
  requestedScopes?: string[]
}

export interface UpdateAppRequest {
  displayName?: string
  description?: string
  logoUrl?: string
  redirectUris?: string[]
  postLogoutRedirectUris?: string[]
  requestedScopes?: string[]
}

export interface CredentialsResponse {
  clientId: string
  clientType: string
  hasActiveSecret: boolean
  activeSecretMasked?: string
  hasRetiringSecret: boolean
  retiringSecretExpiresAt?: string
  retiringSecretExpiresInSeconds?: number
}

export interface RotateSecretResponse {
  clientId: string
  newSecret: string
  retiringSecretExpiresAt: string
  message: string
}

export interface RevokeRetiringSecretResponse {
  success: boolean
  message: string
}

export interface ReviewStatusResponse {
  appId: string
  status: string
  submittedAt?: string
  reviewNotes?: string
}

export interface DeveloperStatusResponse {
  isDeveloperEnabled: boolean
  organizationName?: string
  contactEmail?: string
  registeredAt?: string
}

export interface GenerateAuthorizeUrlRequest {
  clientId: string
  redirectUri: string
  scope?: string
  codeChallenge?: string
  codeChallengeMethod?: string
  state?: string
  prompt?: string
}

export interface GenerateAuthorizeUrlResponse {
  authorizeUrl: string
}

export interface ValidateCredentialsRequest {
  clientId: string
  clientSecret: string
}

export interface ValidateCredentialsResponse {
  isValid: boolean
  error?: string
  errorDescription?: string
}

const API_BASE = '/api/v1/developer'

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = await getAccessToken()
  const headers = new Headers(options.headers || {})
  headers.set('Content-Type', 'application/json')
  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers,
  })

  if (!response.ok) {
    let errorDetail = '請求失敗'
    try {
      const errJson = await response.json()
      errorDetail = errJson.error || errJson.errorDescription || errJson.message || JSON.stringify(errJson)
    } catch {
      errorDetail = await response.text()
    }
    throw new Error(errorDetail || `HTTP ${response.status}`)
  }

  if (response.status === 204) {
    return {} as T
  }

  return response.json()
}

export const developerApi = {
  // Developer Account
  getDeveloperStatus: () => request<DeveloperStatusResponse>('/account/status'),
  enableDeveloper: (data: { organizationName?: string; contactEmail?: string; acceptAgreement: boolean }) =>
    request<DeveloperStatusResponse>('/account/enable', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  // Applications
  getApps: () => request<AppResponse[]>('/apps'),
  getApp: (id: string) => request<AppResponse>(`/apps/${id}`),
  createApp: (data: CreateAppRequest) =>
    request<AppResponse>('/apps', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateApp: (id: string, data: UpdateAppRequest) =>
    request<AppResponse>(`/apps/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteApp: (id: string) =>
    request<void>(`/apps/${id}`, {
      method: 'DELETE',
    }),

  // Credentials & Rotation
  getCredentials: (id: string) => request<CredentialsResponse>(`/apps/${id}/credentials`),
  rotateSecret: (id: string) =>
    request<RotateSecretResponse>(`/apps/${id}/rotate-secret`, {
      method: 'POST',
    }),
  revokeRetiringSecret: (id: string) =>
    request<RevokeRetiringSecretResponse>(`/apps/${id}/revoke-retiring-secret`, {
      method: 'POST',
    }),

  // Review
  submitReview: (id: string, notes?: string) =>
    request<ReviewStatusResponse>(`/apps/${id}/submit-review`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  getReviewStatus: (id: string) => request<ReviewStatusResponse>(`/apps/${id}/review-status`),

  // Sandbox tools
  generateAuthorizeUrl: (data: GenerateAuthorizeUrlRequest) =>
    request<GenerateAuthorizeUrlResponse>('/sandbox/generate-authorize-url', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  validateCredentials: (data: ValidateCredentialsRequest) =>
    request<ValidateCredentialsResponse>('/sandbox/validate-credentials', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
}
