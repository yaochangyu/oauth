export interface AppDetail {
  id: string;
  clientId: string;
  displayName: string;
  clientType: string;
  consentType: string;
  developer: string;
  status: 'Sandbox' | 'InReview' | 'Approved' | 'Rejected' | 'Suspended';
  rejectReason?: string;
  requestReason?: string;
  submittedAt?: string;
  redirectUris: string[];
  postLogoutRedirectUris: string[];
  permissions: string[];
  requirements: string[];
}

export interface UserSummary {
  id: string;
  userName: string;
  email: string;
  isLocked: boolean;
  lockoutEnd?: string;
  roles: string[];
}

export interface UserDetail {
  id: string;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  lockoutEnabled: boolean;
  lockoutEnd?: string;
  isLocked: boolean;
  roles: string[];
  claims: Array<{ type: string; value: string }>;
}

export interface ScopeDetail {
  id: string;
  name: string;
  displayName: string;
  description: string;
  resources: string[];
  isSensitive: boolean;
}

export interface AuditLog {
  id: string;
  eventType: string;
  actor: string;
  target: string;
  details: string;
  ipAddress: string;
  timestamp: string;
}

const API_BASE = '/api/v1/admin';

async function fetchJson<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(options?.headers || {})
    }
  });

  if (!res.ok) {
    let errorMsg = `HTTP ${res.status} ${res.statusText}`;
    try {
      const body = await res.json();
      if (body.message) errorMsg = body.message;
    } catch {
      // ignore
    }
    throw new Error(errorMsg);
  }

  return res.json();
}

export const adminApi = {
  // App Reviews
  getPendingApps: () => fetchJson<AppDetail[]>(`${API_BASE}/apps/pending`),
  getApps: (status?: string, filter?: string) => {
    const params = new URLSearchParams();
    if (status) params.append('status', status);
    if (filter) params.append('filter', filter);
    const qs = params.toString();
    return fetchJson<AppDetail[]>(`${API_BASE}/apps${qs ? `?${qs}` : ''}`);
  },
  getApp: (id: string) => fetchJson<AppDetail>(`${API_BASE}/apps/${encodeURIComponent(id)}`),
  approveApp: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/apps/${encodeURIComponent(id)}/approve`, { method: 'POST' }),
  rejectApp: (id: string, reason: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/apps/${encodeURIComponent(id)}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    }),
  suspendApp: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/apps/${encodeURIComponent(id)}/suspend`, { method: 'POST' }),
  restoreApp: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/apps/${encodeURIComponent(id)}/restore`, { method: 'POST' }),
  deleteApp: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/apps/${encodeURIComponent(id)}`, { method: 'DELETE' }),

  // User Management
  getUsers: (filter?: string, page: number = 1, pageSize: number = 50) => {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() });
    if (filter) params.append('filter', filter);
    return fetchJson<UserSummary[]>(`${API_BASE}/users?${params.toString()}`);
  },
  getUser: (id: string) => fetchJson<UserDetail>(`${API_BASE}/users/${encodeURIComponent(id)}`),
  lockoutUser: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/users/${encodeURIComponent(id)}/lockout`, { method: 'PUT' }),
  unlockUser: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/users/${encodeURIComponent(id)}/unlock`, { method: 'PUT' }),
  revokeSessions: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/users/${encodeURIComponent(id)}/revoke-sessions`, { method: 'POST' }),
  addUserRole: (id: string, roleName: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/users/${encodeURIComponent(id)}/roles`, {
      method: 'POST',
      body: JSON.stringify({ roleName })
    }),
  removeUserRole: (id: string, roleName: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/users/${encodeURIComponent(id)}/roles/${encodeURIComponent(roleName)}`, {
      method: 'DELETE'
    }),

  // Scope Management
  getScopes: (filter?: string) => {
    const params = new URLSearchParams();
    if (filter) params.append('filter', filter);
    const qs = params.toString();
    return fetchJson<ScopeDetail[]>(`${API_BASE}/scopes${qs ? `?${qs}` : ''}`);
  },
  getScope: (id: string) => fetchJson<ScopeDetail>(`${API_BASE}/scopes/${encodeURIComponent(id)}`),
  createScope: (payload: { name: string; displayName?: string; description?: string; resources?: string[]; isSensitive?: boolean }) =>
    fetchJson<{ message: string }>(`${API_BASE}/scopes`, {
      method: 'POST',
      body: JSON.stringify(payload)
    }),
  updateScope: (id: string, payload: { displayName?: string; description?: string; resources?: string[]; isSensitive?: boolean }) =>
    fetchJson<{ message: string }>(`${API_BASE}/scopes/${encodeURIComponent(id)}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    }),
  deleteScope: (id: string) =>
    fetchJson<{ message: string }>(`${API_BASE}/scopes/${encodeURIComponent(id)}`, { method: 'DELETE' }),

  // Audit Logs
  getAuditLogs: (eventType?: string, actor?: string, target?: string) => {
    const params = new URLSearchParams();
    if (eventType) params.append('eventType', eventType);
    if (actor) params.append('actor', actor);
    if (target) params.append('target', target);
    const qs = params.toString();
    return fetchJson<AuditLog[]>(`${API_BASE}/audit-logs${qs ? `?${qs}` : ''}`);
  }
};
