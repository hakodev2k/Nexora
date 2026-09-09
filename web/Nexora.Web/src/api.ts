export type CsrfEnvelope = {
  requestToken: string;
  tokenType: 'csrf';
  expiresInSeconds: number;
};

export type AcceptedResponse = {
  status: string;
  messageCode: string;
};

export type ModuleProjection = {
  code: string;
  enabled: boolean;
  unavailableReason: string | null;
};

export type ProfileResponse = {
  id: string;
  email: string;
  displayName: string;
  timeZoneId: string;
  locale: 'vi' | 'en';
  state: string;
  personalSpaceId: string | null;
  modules: ModuleProjection[];
};

export type LoginResponse = {
  profile: ProfileResponse;
  expiresAt: string;
};

export type DevAccountMessage = {
  id: string;
  purpose: string;
  email: string;
  token: string;
  createdAt: string;
  expiresAt: string;
};

export type SessionProjection = {
  id: string;
  deviceLabel: string;
  createdAt: string;
  lastSeenAt: string;
  expiresAt: string;
  isCurrent: boolean;
};

export type SessionPage = {
  items: SessionProjection[];
  nextCursor: string | null;
};

export type AdminModuleResponse = {
  id: string;
  code: string;
  name: string;
  state: string;
  systemEnabled: boolean;
  registrationEnabled: boolean;
  policyRevision: string;
  eTag: string;
  requiredDependencies: string[];
  requiredBy: string[];
  unavailableReason: string | null;
};

export type AdminModulePageResponse = {
  items: AdminModuleResponse[];
  nextCursor: string | null;
};

export type ModulePolicyChangeRequest = {
  systemEnabled?: boolean;
  registrationEnabled?: boolean;
};

export type ModulePolicyCommitRequest = ModulePolicyChangeRequest & {
  previewToken: string;
};

export type ModuleChangeDiff = {
  field: string;
  before: string;
  after: string;
};

export type ModulePolicyBlocker = {
  code: string;
  message: string;
  field: string | null;
};

export type ModulePolicyPreviewResponse = {
  previewToken: string;
  expiresAt: string;
  eTag: string;
  changes: ModuleChangeDiff[];
  blockers: ModulePolicyBlocker[];
};

let csrfToken: string | null = null;
let currentProfileETag: string | null = null;

export async function getCsrf(): Promise<CsrfEnvelope> {
  const response = await fetch('/api/v1/auth/csrf', {
    method: 'GET',
    credentials: 'same-origin',
    headers: { Accept: 'application/json' },
    cache: 'no-store'
  });

  if (!response.ok) {
    throw new Error('CSRF endpoint unavailable');
  }

  const body = (await response.json()) as CsrfEnvelope;
  csrfToken = body.requestToken;
  return body;
}

export function getCsrfTokenFromMemory(): string | null {
  return csrfToken;
}

export function getCurrentProfileETag(): string | null {
  return currentProfileETag;
}

async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const method = init.method ?? 'GET';
  const unsafe = !['GET', 'HEAD', 'OPTIONS'].includes(method.toUpperCase());
  if (unsafe && csrfToken === null) {
    await getCsrf();
  }

  const headers = new Headers(init.headers);
  headers.set('Accept', 'application/json');
  if (unsafe) {
    headers.set('Content-Type', 'application/json');
    if (csrfToken !== null) {
      headers.set('X-CSRF-Token', csrfToken);
    }
  }

  const response = await fetch(path, {
    ...init,
    method,
    credentials: 'same-origin',
    cache: 'no-store',
    headers
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `${response.status} ${response.statusText}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (path === '/api/v1/me') {
    currentProfileETag = response.headers.get('ETag');
  }

  return (await response.json()) as T;
}

function devAdminHeaders(additional?: Record<string, string>): HeadersInit {
  return {
    'X-Nexora-Dev-SuperAdmin': 'true',
    ...additional
  };
}

export function registerDemoUser(email: string, password: string, timeZoneId = 'Asia/Ho_Chi_Minh') {
  return apiFetch<AcceptedResponse>('/api/v1/auth/registrations', {
    method: 'POST',
    body: JSON.stringify({ email, password, timeZoneId })
  });
}

export function listDevAccountMessages() {
  return apiFetch<DevAccountMessage[]>('/api/v1/dev/account-messages');
}

export function verifyEmail(token: string) {
  return apiFetch<{ status: string; messageCode: string; profile: ProfileResponse }>('/api/v1/auth/verifications', {
    method: 'POST',
    body: JSON.stringify({ token })
  });
}

export function login(email: string, password: string) {
  return apiFetch<LoginResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password })
  });
}

export function logout() {
  return apiFetch<void>('/api/v1/auth/logout', { method: 'POST' });
}

export function requestPasswordReset(email: string) {
  return apiFetch<AcceptedResponse>('/api/v1/auth/password-resets', {
    method: 'POST',
    body: JSON.stringify({ email })
  });
}

export function confirmPasswordReset(token: string, newPassword: string) {
  return apiFetch<void>('/api/v1/auth/password-resets/confirm', {
    method: 'POST',
    body: JSON.stringify({ token, newPassword })
  });
}

export function getMe() {
  return apiFetch<ProfileResponse>('/api/v1/me');
}

export function updateMe(patch: Partial<Pick<ProfileResponse, 'displayName' | 'timeZoneId' | 'locale'>>) {
  if (currentProfileETag === null) {
    throw new Error('Profile ETag missing. Call getMe before updateMe.');
  }

  return apiFetch<ProfileResponse>('/api/v1/me', {
    method: 'PATCH',
    headers: { 'If-Match': currentProfileETag },
    body: JSON.stringify(patch)
  });
}

export function listSessions() {
  return apiFetch<SessionPage>('/api/v1/me/sessions');
}

export function revokeAllSessions() {
  return apiFetch<void>('/api/v1/me/sessions/revoke-all', { method: 'POST' });
}

export function listAdminModulesDev() {
  return apiFetch<AdminModulePageResponse>('/api/v1/admin/modules', {
    headers: devAdminHeaders()
  });
}

export function previewAdminModulePolicyDev(moduleId: string, change: ModulePolicyChangeRequest) {
  return apiFetch<ModulePolicyPreviewResponse>(`/api/v1/admin/modules/${moduleId}/preview`, {
    method: 'POST',
    headers: devAdminHeaders(),
    body: JSON.stringify(change)
  });
}

export function setAdminModulePolicyDev(moduleId: string, eTag: string, request: ModulePolicyCommitRequest) {
  return apiFetch<AdminModuleResponse>(`/api/v1/admin/modules/${moduleId}/policy`, {
    method: 'PUT',
    headers: devAdminHeaders({ 'If-Match': eTag }),
    body: JSON.stringify(request)
  });
}
