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
  role: string;
  modules: ModuleProjection[];
};

export type DashboardItem = {
  id: string;
  kind: string;
  title: string;
  status: string | null;
  at: string | null;
  detail: string | null;
};

export type DashboardWidget = {
  id: string;
  title: string;
  sourceModule: string;
  state: 'Ready' | 'Empty' | 'Unavailable' | 'Degraded' | string;
  message: string | null;
  refreshedAt: string;
  count: number;
  items: DashboardItem[];
};

export type DashboardSnapshot = {
  timeZoneId: string;
  generatedAt: string;
  widgets: DashboardWidget[];
};

export type LoginResponse = {
  profile: ProfileResponse;
  expiresAt: string;
};

export type VerificationResponse = {
  status: string;
  messageCode: string;
  profile?: ProfileResponse;
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

export type ProjectRecord = {
  id: string;
  name: string;
  description: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  etag: string;
  startAt: string;
  endAt: string;
  priority: string;
  tagsJson: string;
  notes: string | null;
};

export type ProjectPage = {
  items: ProjectRecord[];
  nextCursor: string | null;
};

export type TaskRecord = {
  id: string;
  projectId: string;
  title: string;
  description: string | null;
  status: string;
  dueAt: string | null;
  createdAt: string;
  updatedAt: string;
  etag: string;
  startAt: string;
  endAt: string;
  priority: string;
  tagsJson: string;
  acceptanceCriteriaJson: string;
  rank: number;
  reminderAt: string | null;
};

export type TaskPage = {
  items: TaskRecord[];
  nextCursor: string | null;
};

export type CalendarEventRecord = {
  id: string;
  title: string;
  description: string | null;
  startAt: string;
  endAt: string;
  timeZoneId: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  etag: string;
  isAllDay: boolean;
  sourceUid: string | null;
};

export type CalendarEventPage = {
  items: CalendarEventRecord[];
  nextCursor: string | null;
};

export type DocumentSummary = {
  id: string;
  title: string;
  documentType: string;
  editorMode: string;
  status: string;
  preArchiveStatus: string | null;
  versionNumber: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type DocumentRecord = DocumentSummary & {
  body: string;
};

export type DocumentPage = {
  items: DocumentSummary[];
  nextCursor: string | null;
};

export type NotificationDeliveryRecord = {
  channel: string;
  state: string;
  attempts: number;
  lastErrorCode: string | null;
  updatedAt: string;
};

export type NotificationRecord = {
  id: string;
  kind: string;
  title: string;
  body: string;
  sourceRef: string | null;
  createdAt: string;
  readAt: string | null;
  etag: string;
  deliveries: NotificationDeliveryRecord[];
};

export type NotificationPage = {
  items: NotificationRecord[];
  nextCursor: string | null;
  unreadCount: number;
};

export type TrashItemRecord = {
  id: string;
  resourceType: string;
  resourceId: string;
  deletionBatchId: string;
  priorStatus: string;
  deletedAt: string;
  restoredAt: string | null;
  purgedAt: string | null;
};

export type TrashPage = {
  items: TrashItemRecord[];
  nextCursor: string | null;
};

export type PreferenceRecord = {
  id: string;
  preferenceKey: string;
  schemaVersion: number;
  valueJson: string;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type PreferencePage = {
  items: PreferenceRecord[];
  nextCursor: string | null;
};

export type FinanceCategoryRecord = {
  id: string;
  title: string;
  usageCount: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type FinanceCategoryPage = {
  items: FinanceCategoryRecord[];
  nextCursor: string | null;
};

export type FinanceManualRecord = {
  id: string;
  categoryId: string;
  categoryTitle: string;
  amount: string;
  currencyCode: string;
  occurredOn: string;
  note: string | null;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type FinanceSummary = {
  currencyCode: string;
  amount: string;
};

export type FinanceRecordPage = {
  items: FinanceManualRecord[];
  summaries: FinanceSummary[];
  nextCursor: string | null;
};

export type BookmarkRecord = {
  id: string;
  url: string;
  canonicalUrl: string;
  title: string;
  description: string | null;
  health: string;
  lastCheckedAt: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type BookmarkPage = {
  items: BookmarkRecord[];
  nextCursor: string | null;
};

export type SnippetRecord = {
  id: string;
  title: string;
  language: string;
  body: string;
  description: string | null;
  versionNumber: number;
  status: string;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type SnippetPage = {
  items: SnippetRecord[];
  nextCursor: string | null;
};

export type ReadingItemRecord = {
  id: string;
  sourceType: string;
  sourceId: string;
  state: string;
  progress: number;
  savedAt: string;
  readAt: string | null;
  safeTitleSnapshot: string;
  safeUrlSnapshot: string;
  sourceAvailable: boolean;
  updatedAt: string;
  etag: string;
};

export type ReadingPage = {
  items: ReadingItemRecord[];
  nextCursor: string | null;
};

export type TagRecord = {
  id: string;
  namespace: string;
  name: string;
  color: string | null;
  usageCount: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type TagPage = {
  items: TagRecord[];
  nextCursor: string | null;
};

export type ToolboxTool = {
  code: string;
  name: string;
  category: string;
  description: string;
  executionMode: string;
  actionKey: string;
  acceptsOptions: boolean;
};

export type ToolboxCatalog = {
  items: ToolboxTool[];
};

export type ToolboxRunResult = {
  toolCode: string;
  output: string;
  warning: string | null;
  errorPath: string | null;
  durationMilliseconds: number;
};

export type GoalRecord = {
  id: string;
  title: string;
  description: string | null;
  startDate: string | null;
  endDate: string | null;
  status: string;
  progress: number;
  targetCount: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type GoalTargetRecord = {
  id: string;
  kind: string;
  title: string;
  initialValue: number | null;
  currentValue: number | null;
  targetValue: number | null;
  progress: number;
  updatedAt: string;
  etag: string;
};

export type GoalDetail = {
  goal: GoalRecord;
  targets: GoalTargetRecord[];
};

export type GoalPage = {
  items: GoalRecord[];
  nextCursor: string | null;
};

export type FinanceRecordInput = {
  categoryId: string;
  amount: string;
  currencyCode: string;
  occurredOn: string;
  note: string | null;
};

export type AdminUserRecord = {
  id: string;
  email: string;
  displayName: string;
  state: string;
  emailConfirmed: boolean;
  role: string;
  personalSpaceId: string | null;
  personalSpaceState: string | null;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type AdminUserPage = {
  items: AdminUserRecord[];
  nextCursor: string | null;
};

export type AdminUserAccess = {
  user: AdminUserRecord;
  actionGrants: { actionKey: string; effect: string; status: string; updatedAt: string }[];
  moduleGrants: { code: string; enabled: boolean; state: string; systemEnabled: boolean }[];
};

export type ProfilePatch = Partial<Pick<ProfileResponse, 'displayName' | 'timeZoneId' | 'locale'>>;

export type FieldErrors = Record<string, string[]>;

type ProblemPayload = {
  title?: unknown;
  detail?: unknown;
  code?: unknown;
  traceId?: unknown;
  errors?: unknown;
};

export class NexoraApiError extends Error {
  readonly status: number;
  readonly code: string | null;
  readonly traceId: string | null;
  readonly fieldErrors: FieldErrors;

  constructor(message: string, status: number, code: string | null = null, traceId: string | null = null, fieldErrors: FieldErrors = {}) {
    super(message);
    this.name = 'NexoraApiError';
    this.status = status;
    this.code = code;
    this.traceId = traceId;
    this.fieldErrors = fieldErrors;
  }
}

// These values intentionally live only for the lifetime of this page. Authentication
// authority remains the server's Secure/HttpOnly cookie and the SQL-backed session.
let csrfToken: string | null = null;
let currentProfileETag: string | null = null;

export function createIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
}

export async function getCsrf(): Promise<CsrfEnvelope> {
  let response: Response;
  try {
    response = await fetch('/api/v1/auth/csrf', {
      method: 'GET',
      credentials: 'same-origin',
      headers: { Accept: 'application/json' },
      cache: 'no-store'
    });
  } catch {
    throw new NexoraApiError('Không thể kết nối Nexora API local.', 0, 'NetworkUnavailable');
  }

  if (!response.ok) {
    throw await toApiError(response);
  }

  const rotatedCsrf = response.headers.get('X-CSRF-Token');
  if (rotatedCsrf) {
    csrfToken = rotatedCsrf;
  }

  const body = (await response.json()) as CsrfEnvelope;
  if (!body.requestToken || body.tokenType !== 'csrf') {
    throw new NexoraApiError('CSRF endpoint trả về dữ liệu không hợp lệ.', response.status, 'CsrfInvalid');
  }

  csrfToken = body.requestToken;
  return body;
}

export function getCsrfTokenFromMemory(): string | null {
  return csrfToken;
}

export function getCurrentProfileETag(): string | null {
  return currentProfileETag;
}

export function clearProfileRevision(): void {
  currentProfileETag = null;
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

  let response: Response;
  try {
    response = await fetch(path, {
      ...init,
      method,
      credentials: 'same-origin',
      cache: 'no-store',
      headers
    });
  } catch {
    throw new NexoraApiError('Không thể kết nối Nexora API local.', 0, 'NetworkUnavailable');
  }

  if (!response.ok) {
    if (response.status === 401) {
      currentProfileETag = null;
    }
    throw await toApiError(response);
  }

  if (path === '/api/v1/me') {
    currentProfileETag = response.headers.get('ETag');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('Content-Type') ?? '';
  if (!contentType.includes('json')) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function toApiError(response: Response): Promise<NexoraApiError> {
  const fallback = messageForStatus(response.status);
  const contentType = response.headers.get('Content-Type') ?? '';
  let payload: ProblemPayload = {};

  if (contentType.includes('json')) {
    try {
      payload = (await response.json()) as ProblemPayload;
    } catch {
      payload = {};
    }
  }

  const detail = typeof payload.detail === 'string' ? payload.detail : undefined;
  const title = typeof payload.title === 'string' ? payload.title : undefined;
  const code = typeof payload.code === 'string' ? payload.code : null;
  const traceId = typeof payload.traceId === 'string' ? payload.traceId : null;
  const fieldErrors = parseFieldErrors(payload.errors);

  return new NexoraApiError(detail || title || fallback, response.status, code, traceId, fieldErrors);
}

function parseFieldErrors(value: unknown): FieldErrors {
  if (!value || typeof value !== 'object' || Array.isArray(value)) {
    return {};
  }

  const result: FieldErrors = {};
  for (const [key, messages] of Object.entries(value)) {
    if (Array.isArray(messages)) {
      const safe = messages.filter((message): message is string => typeof message === 'string');
      if (safe.length > 0) {
        result[key] = safe;
      }
    } else if (typeof messages === 'string') {
      result[key] = [messages];
    }
  }

  return result;
}

function messageForStatus(status: number): string {
  switch (status) {
    case 401:
      return 'Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.';
    case 403:
      return 'Thao tác không được phép trong phiên hoặc quyền hiện tại.';
    case 404:
      return 'Tài nguyên không tồn tại hoặc không khả dụng trong phiên này.';
    case 409:
      return 'Thao tác xung đột với thay đổi hiện tại. Hãy tải lại và thử lại.';
    case 410:
      return 'Mã đã hết hạn hoặc đã được sử dụng.';
    case 412:
      return 'Dữ liệu đã thay đổi ở nơi khác. Hãy tải lại trước khi lưu.';
    case 422:
      return 'Dữ liệu gửi lên chưa hợp lệ.';
    case 429:
      return 'Có quá nhiều yêu cầu. Vui lòng thử lại sau.';
    default:
      return status >= 500 ? 'Nexora API đang gặp lỗi. Vui lòng thử lại.' : 'Yêu cầu không thành công.';
  }
}

function jsonMutationHeaders(idempotencyKey?: string, extra?: HeadersInit): HeadersInit {
  const headers = new Headers(extra);
  if (idempotencyKey) {
    headers.set('Idempotency-Key', idempotencyKey);
  }
  return headers;
}

export function registerUser(
  email: string,
  password: string,
  timeZoneId: string,
  displayName: string,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<AcceptedResponse>('/api/v1/auth/registrations', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ email, password, timeZoneId, displayName: displayName || null })
  });
}

export function verifyEmail(token: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<VerificationResponse>('/api/v1/auth/verifications', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ token })
  });
}

export function resendVerification(email: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AcceptedResponse>('/api/v1/auth/verifications/resend', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ email })
  });
}

export function login(email: string, password: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<LoginResponse>('/api/v1/auth/login', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ email, password })
  });
}

export function logout(idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>('/api/v1/auth/logout', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey)
  });
}

export function requestPasswordReset(email: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AcceptedResponse>('/api/v1/auth/password-resets', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ email })
  });
}

export function confirmPasswordReset(token: string, newPassword: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>('/api/v1/auth/password-resets/confirm', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ token, newPassword })
  });
}

export function getMe() {
  return apiFetch<ProfileResponse>('/api/v1/me');
}

export function getDashboard() {
  return apiFetch<DashboardSnapshot>('/api/v1/dashboard');
}

export function updateMe(patch: ProfilePatch, idempotencyKey = createIdempotencyKey()) {
  if (currentProfileETag === null) {
    throw new NexoraApiError('Profile chưa có revision. Hãy tải lại trước khi lưu.', 428, 'PreconditionRequired');
  }

  return apiFetch<ProfileResponse>('/api/v1/me', {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': currentProfileETag }),
    body: JSON.stringify(patch)
  });
}

export function listSessions() {
  return apiFetch<SessionPage>('/api/v1/me/sessions');
}

export function revokeSession(sessionId: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/me/sessions/${encodeURIComponent(sessionId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey)
  });
}

export function revokeAllSessions(idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>('/api/v1/me/sessions/revoke-all', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey)
  });
}

export function listProjects(limit = 50) {
  return apiFetch<ProjectPage>(`/api/v1/projects?limit=${encodeURIComponent(limit)}`);
}

export function createProject(name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ProjectRecord>('/api/v1/projects', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes })
  });
}

export function updateProject(id: string, etag: string, name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes })
  });
}

export function deleteProject(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionProject(id: string, etag: string, status: string, reason: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status, reason })
  });
}

export function listTasks(projectId?: string, limit = 100) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (projectId) query.set('projectId', projectId);
  return apiFetch<TaskPage>(`/api/v1/tasks?${query.toString()}`);
}

export function createTask(
  projectId: string,
  title: string,
  description: string | null,
  status: string,
  dueAt: string | null,
  startAt: string,
  endAt: string,
  priority = 'P3',
  tagsJson: string | null = null,
  acceptanceCriteriaJson: string | null = null,
  rank = 0,
  reminderAt: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<TaskRecord>('/api/v1/tasks', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ projectId, title, description, status, dueAt, startAt, endAt, priority, tagsJson, acceptanceCriteriaJson, rank, reminderAt })
  });
}

export function updateTask(
  id: string,
  etag: string,
  projectId: string,
  title: string,
  description: string | null,
  status: string,
  dueAt: string | null,
  startAt: string,
  endAt: string,
  priority = 'P3',
  tagsJson: string | null = null,
  acceptanceCriteriaJson: string | null = null,
  rank = 0,
  reminderAt: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ projectId, title, description, status, dueAt, startAt, endAt, priority, tagsJson, acceptanceCriteriaJson, rank, reminderAt })
  });
}

export function deleteTask(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/tasks/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionTask(id: string, etag: string, status: string, reason: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status, reason })
  });
}

export function listCalendarEvents(from?: string, to?: string, limit = 100) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (from) query.set('from', from);
  if (to) query.set('to', to);
  return apiFetch<CalendarEventPage>(`/api/v1/calendar/events?${query.toString()}`);
}

export function createCalendarEvent(
  title: string,
  description: string | null,
  startAt: string,
  endAt: string,
  timeZoneId: string,
  isAllDay = false,
  sourceUid: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<CalendarEventRecord>('/api/v1/calendar/events', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, description, startAt, endAt, timeZoneId, isAllDay, sourceUid })
  });
}

export function updateCalendarEvent(
  id: string,
  etag: string,
  title: string,
  description: string | null,
  startAt: string,
  endAt: string,
  timeZoneId: string,
  isAllDay = false,
  sourceUid: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<CalendarEventRecord>(`/api/v1/calendar/events/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, description, startAt, endAt, timeZoneId, isAllDay, sourceUid })
  });
}

export function deleteCalendarEvent(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/calendar/events/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionCalendarEvent(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<CalendarEventRecord>(`/api/v1/calendar/events/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  });
}

export function listDocuments(status?: string, limit = 100) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (status) query.set('status', status);
  return apiFetch<DocumentPage>(`/api/v1/documents?${query.toString()}`);
}

export function getDocument(id: string) {
  return apiFetch<DocumentRecord>(`/api/v1/documents/${encodeURIComponent(id)}`);
}

export function createDocument(title: string, documentType: string, editorMode: string, body: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<DocumentRecord>('/api/v1/documents', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, documentType, editorMode, body })
  });
}

export function saveDocument(id: string, etag: string, title: string, body: string, changeNote: string | null = null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<DocumentRecord>(`/api/v1/documents/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, body, changeNote })
  });
}

export function transitionDocument(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<DocumentRecord>(`/api/v1/documents/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  });
}

export function listNotifications(unreadOnly = false, limit = 50) {
  const query = new URLSearchParams({ unreadOnly: String(unreadOnly), limit: String(limit) });
  return apiFetch<NotificationPage>(`/api/v1/notifications?${query.toString()}`);
}

export function markNotificationRead(id: string, etag: string, read: boolean, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<NotificationRecord>(`/api/v1/notifications/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ read })
  });
}

export function markAllNotificationsRead(idempotencyKey = createIdempotencyKey()) {
  return apiFetch<{ watermark: string; updatedCount: number }>('/api/v1/notifications/mark-all-read', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey)
  });
}

export function deleteNotifications(notificationIds: string[], idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>('/api/v1/notifications/delete', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ notificationIds })
  });
}

export function listTrash(limit = 100) {
  return apiFetch<TrashPage>(`/api/v1/trash?limit=${encodeURIComponent(limit)}`);
}

export function restoreTrashBatch(batchId: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<{ deletionBatchId: string; restoredCount: number; remainingCount: number }>(`/api/v1/trash/batches/${encodeURIComponent(batchId)}/restore`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey)
  });
}

export function purgeTrashBatch(batchId: string, confirmation: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/trash/batches/${encodeURIComponent(batchId)}/purge`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ deletionBatchId: batchId, confirmation })
  });
}

export function listPreferences() {
  return apiFetch<PreferencePage>('/api/v1/settings/preferences');
}

export function updatePreference(key: string, etag: string | '*', value: unknown, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<PreferenceRecord>(`/api/v1/settings/preferences/${encodeURIComponent(key)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ schemaVersion: 1, valueJson: JSON.stringify(value) })
  });
}

export function listFinanceCategories(query = '', limit = 100) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<FinanceCategoryPage>(`/api/v1/finance/categories?${params.toString()}`);
}

export function createFinanceCategory(title: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FinanceCategoryRecord>('/api/v1/finance/categories', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, ifMatch: '*' })
  });
}

export function updateFinanceCategory(id: string, etag: string, title: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FinanceCategoryRecord>(`/api/v1/finance/categories/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, ifMatch: etag })
  });
}

export function removeFinanceCategory(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/finance/categories/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function listFinanceRecords(filters: {
  categoryId?: string;
  currencyCode?: string;
  from?: string;
  to?: string;
  query?: string;
  limit?: number;
} = {}) {
  const params = new URLSearchParams({ limit: String(filters.limit ?? 100) });
  if (filters.categoryId) params.set('categoryId', filters.categoryId);
  if (filters.currencyCode?.trim()) params.set('currencyCode', filters.currencyCode.trim());
  if (filters.from) params.set('from', filters.from);
  if (filters.to) params.set('to', filters.to);
  if (filters.query?.trim()) params.set('query', filters.query.trim());
  return apiFetch<FinanceRecordPage>(`/api/v1/finance/records?${params.toString()}`);
}

export function createFinanceRecord(input: FinanceRecordInput, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FinanceManualRecord>('/api/v1/finance/records', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify(input)
  });
}

export function updateFinanceRecord(id: string, etag: string, input: FinanceRecordInput, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FinanceManualRecord>(`/api/v1/finance/records/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify(input)
  });
}

export function listBookmarks(includeArchived = false, query = '', limit = 100) {
  const params = new URLSearchParams({ includeArchived: String(includeArchived), limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<BookmarkPage>(`/api/v1/bookmarks?${params.toString()}`);
}

export function createBookmark(url: string, title: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<BookmarkRecord>('/api/v1/bookmarks', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ url, title, description })
  });
}

export function updateBookmark(id: string, etag: string, url: string, title: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<BookmarkRecord>(`/api/v1/bookmarks/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ url, title, description })
  });
}

export function transitionBookmark(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<BookmarkRecord>(`/api/v1/bookmarks/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  });
}

export function listSnippets(includeArchived = false, query = '', limit = 100) {
  const params = new URLSearchParams({ includeArchived: String(includeArchived), limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<SnippetPage>(`/api/v1/snippets?${params.toString()}`);
}

export function createSnippet(title: string, language: string, body: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SnippetRecord>('/api/v1/snippets', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, language, body, description })
  });
}

export function saveSnippet(id: string, etag: string, title: string, language: string, body: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SnippetRecord>(`/api/v1/snippets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, language, body, description })
  });
}

export function transitionSnippet(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SnippetRecord>(`/api/v1/snippets/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  });
}

export function listReadingQueue(state = '', limit = 100) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (state.trim()) params.set('state', state.trim());
  return apiFetch<ReadingPage>(`/api/v1/read-later?${params.toString()}`);
}

export function saveReadingItem(sourceType: string, sourceId: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ReadingItemRecord>('/api/v1/read-later', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ sourceType, sourceId })
  });
}

export function removeReadingItem(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/read-later/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function updateReadingItem(id: string, etag: string, state: string, progress?: number, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ReadingItemRecord>(`/api/v1/read-later/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify(progress === undefined ? { state } : { state, progress })
  });
}

export function listOrganizationTags(tagNamespace = '', query = '', limit = 100) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (tagNamespace.trim()) params.set('namespace', tagNamespace.trim());
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<TagPage>(`/api/v1/organization/tags?${params.toString()}`);
}

export function createOrganizationTag(tagNamespace: string, name: string, color: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<TagRecord>('/api/v1/organization/tags', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ namespace: tagNamespace, name, color })
  });
}

export function renameOrganizationTag(id: string, etag: string, name: string, color: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<TagRecord>(`/api/v1/organization/tags/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ name, color })
  });
}

export function removeOrganizationTag(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/organization/tags/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function listDeveloperTools() {
  return apiFetch<ToolboxCatalog>('/api/v1/developer/tools');
}

export function runDeveloperTool(toolCode: string, input: string, options: Record<string, string> = {}) {
  return apiFetch<ToolboxRunResult>('/api/v1/developer/tools/run', {
    method: 'POST',
    body: JSON.stringify({ toolCode, input, options })
  });
}

export function listGoals(status = '', query = '', limit = 100) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (status.trim()) params.set('status', status.trim());
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<GoalPage>(`/api/v1/goals?${params.toString()}`);
}

export function getGoal(id: string) {
  return apiFetch<GoalDetail>(`/api/v1/goals/${encodeURIComponent(id)}`);
}

export function createGoal(
  title: string,
  description: string | null,
  startDate: string | null,
  endDate: string | null,
  numericTarget: { title: string; initialValue: number; currentValue: number; targetValue: number } | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<GoalDetail>('/api/v1/goals', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, description, startDate: startDate || null, endDate: endDate || null, numericTarget })
  });
}

export function updateGoal(
  id: string,
  etag: string,
  title: string,
  description: string | null,
  startDate: string | null,
  endDate: string | null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<GoalDetail>(`/api/v1/goals/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, description, startDate: startDate || null, endDate: endDate || null, numericTarget: null })
  });
}

export function recordGoalProgress(
  goalId: string,
  targetId: string,
  goalEtag: string,
  currentValue: number,
  note: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<GoalDetail>(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/${encodeURIComponent(targetId)}/progress`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': goalEtag }),
    body: JSON.stringify({ currentValue, note })
  });
}

export function transitionGoal(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<GoalDetail>(`/api/v1/goals/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  });
}

export function listAdminUsers(query = '') {
  const suffix = query.trim() ? `?q=${encodeURIComponent(query.trim())}` : '';
  return apiFetch<AdminUserPage>(`/api/v1/admin/users${suffix}`);
}

export function getAdminUserAccess(userId: string) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access`);
}

export function setAdminUserRole(userId: string, etag: string, role: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/role`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify({ role, ifMatch: etag })
  });
}

export function setAdminActionGrant(userId: string, etag: string, actionKey: string, effect: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/permissions`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify({ actionKey, effect, ifMatch: etag })
  });
}

export function setAdminModuleGrant(userId: string, etag: string, moduleCode: string, enabled: boolean, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/modules/${encodeURIComponent(moduleCode)}`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify({ enabled, ifMatch: etag })
  });
}

export function disableAdminUser(userId: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/admin/users/${encodeURIComponent(userId)}/disable`, {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify({ confirmation: 'DISABLE', ifMatch: etag })
  });
}
