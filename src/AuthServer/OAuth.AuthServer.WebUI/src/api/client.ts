// Headless AuthServer API 呼叫共用封裝。一律帶上 Cookie（credentials: 'include'），
// 對應後端 Identity Cookie 驗證（/api/v1/account/login 建立的工作階段）。

export interface ApiError {
  status: number
  message: string
}

async function request<T>(input: string, init?: RequestInit): Promise<T> {
  const response = await fetch(input, {
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
    ...init,
  })

  const text = await response.text()
  const body = text ? JSON.parse(text) : undefined

  if (!response.ok) {
    const message =
      (body && typeof body === 'object' && 'message' in body && String((body as { message: unknown }).message)) ||
      (response.status === 429 ? '嘗試次數過多，請稍後再試' : `請求失敗（HTTP ${response.status}）`)
    const error: ApiError = { status: response.status, message }
    throw error
  }

  return body as T
}

export interface LoginRequest {
  userName: string
  password: string
  returnUrl?: string | null
}

export interface LoginResponse {
  success: boolean
  returnUrl: string
}

export function login(payload: LoginRequest): Promise<LoginResponse> {
  return request<LoginResponse>('/api/v1/account/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function logout(): Promise<void> {
  return request<void>('/api/v1/account/logout', { method: 'POST' })
}

export interface RegisterRequest {
  email: string
  password: string
  displayName?: string | null
}

export interface RegisterResponse {
  userId: string
  email: string
}

export function register(payload: RegisterRequest): Promise<RegisterResponse> {
  return request<RegisterResponse>('/api/v1/account/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export interface ConsentInfo {
  clientId: string
  clientDisplayName: string
  scopes: string[]
  returnUrl: string
}

export function getConsentInfo(returnUrl: string): Promise<ConsentInfo> {
  return request<ConsentInfo>(`/api/v1/connect/consent-info?returnUrl=${encodeURIComponent(returnUrl)}`)
}

export interface ConsentDecisionRequest {
  returnUrl: string
  clientId: string
}

export interface ConsentDecisionResponse {
  redirectUrl: string
}

export function acceptConsent(payload: ConsentDecisionRequest): Promise<ConsentDecisionResponse> {
  return request<ConsentDecisionResponse>('/api/v1/connect/consent-accept', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function denyConsent(payload: ConsentDecisionRequest): Promise<ConsentDecisionResponse> {
  return request<ConsentDecisionResponse>('/api/v1/connect/consent-deny', {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
