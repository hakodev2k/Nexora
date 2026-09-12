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

export type SearchResultRecord = {
  id: string;
  resourceType: string;
  sourceModule: string;
  title: string;
  snippet: string | null;
  status: string | null;
  updatedAt: string;
  route: string;
};

export type SearchProviderStatus = {
  resourceType: string;
  sourceModule: string;
  state: string;
  message: string | null;
  count: number;
};

export type SearchPage = {
  query: string;
  resourceType: string | null;
  includeArchived: boolean;
  items: SearchResultRecord[];
  providers: SearchProviderStatus[];
  nextCursor: string | null;
};

export type FavoriteRecord = {
  id: string;
  resourceType: string;
  resourceId: string;
  state: string;
  title: string | null;
  status: string | null;
  updatedAt: string | null;
  route: string | null;
  rank: number;
  createdAt: string;
  favoriteUpdatedAt: string;
  etag: string;
};

export type FavoritePage = {
  items: FavoriteRecord[];
  nextCursor: string | null;
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
  isOverdue: boolean;
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
  sourceKind: string;
  taskId: string | null;
};

export type CalendarEventPage = {
  items: CalendarEventRecord[];
  nextCursor: string | null;
};

export type ReminderDeliveryState = {
  channel: string;
  state: string;
  attempts: number;
  lastErrorCode: string | null;
};

export type ReminderRecord = {
  id: string;
  sourceType: 'Task' | 'CalendarEvent' | string;
  sourceId: string;
  configType: 'None' | 'BeforeStart15m' | 'Exact' | string;
  exactAt: string | null;
  timeZoneId: string;
  dueAt: string | null;
  sourceRevision: number;
  state: 'None' | 'Pending' | 'Dispatched' | 'Canceled' | 'Expired' | 'Missed' | string;
  createdAt: string;
  updatedAt: string;
  etag: string;
  deliveries: ReminderDeliveryState[];
};

export type ReminderSourceView = {
  sourceType: 'Task' | 'CalendarEvent' | string;
  sourceId: string;
  sourceTitle: string;
  sourceStartAt: string;
  timeZoneId: string;
  sourceETag: string;
  reminder: ReminderRecord | null;
};

export type PlannerPinRecord = {
  id: string;
  taskId: string;
  taskTitle: string;
  taskStatus: string;
  projectName: string;
  startAt: string;
  endAt: string;
  planDate: string;
  rank: number;
  notes: string | null;
  sourceAvailable: boolean;
  updatedAt: string;
  etag: string;
};

export type PlannerPlan = {
  from: string;
  to: string;
  pins: PlannerPinRecord[];
  etag: string;
};

export type HabitScheduleRecord = {
  id: string;
  effectiveFrom: string;
  effectiveUntil: string | null;
  weekdayMask: number;
  targetCount: number | null;
  paused: boolean;
  etag: string;
};

export type HabitCheckInRecord = {
  id: string;
  localDate: string;
  count: number;
  note: string | null;
  scheduleId: string;
  updatedAt: string;
  etag: string;
};

export type HabitRecord = {
  id: string;
  title: string;
  kind: 'Boolean' | 'Count' | string;
  targetCount: number | null;
  unit: string | null;
  state: 'Active' | 'Paused' | 'Archived' | string;
  timeZoneId: string;
  reminderLocalTime: string | null;
  currentSchedule: HabitScheduleRecord | null;
  currentStreak: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type HabitDetail = {
  habit: HabitRecord;
  schedules: HabitScheduleRecord[];
  checkIns: HabitCheckInRecord[];
};

export type HabitPage = {
  items: HabitRecord[];
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

export type ShareLinkRecord = {
  id: string;
  resourceType: string;
  resourceId: string;
  mode: string;
  expiresAt: string | null;
  revokedAt: string | null;
  projectionVersion: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  etag: string;
  allowedUserIds: string[];
  token: string | null;
};

export type ShareLinkPage = { items: ShareLinkRecord[]; nextCursor: string | null };

export type SharedTask = {
  id: string;
  title: string;
  description: string | null;
  status: string;
  dueAt: string | null;
  startAt: string;
  endAt: string;
  priority: string;
  tagsJson: string;
  isOverdue: boolean;
};

export type SharedResource = {
  resourceType: string;
  resourceId: string;
  mode: string;
  expiresAt: string | null;
  projectionVersion: string;
  project: { id: string; name: string; description: string | null; status: string; startAt: string; endAt: string; priority: string; tagsJson: string; tasks: SharedTask[] } | null;
  document: { id: string; title: string; documentType: string; editorMode: string; body: string; status: string; versionNumber: number; updatedAt: string } | null;
};

export type SupportGrantRecord = {
  id: string;
  moduleCode: string;
  durationMode: string;
  expiresAt: string | null;
  revokedAt: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  etag: string;
};

export type SupportGrantPage = { items: SupportGrantRecord[]; nextCursor: string | null };
export type SupportSessionRecord = {
  id: string;
  targetUserId: string;
  moduleCode: string;
  mode: string;
  expiresAt: string;
  endedAt: string | null;
  supportGrantId: string | null;
  reason: string | null;
  createdAt: string;
  etag: string;
};
export type SupportSessionPage = { items: SupportSessionRecord[]; nextCursor: string | null };

export type FileRecord = {
  id: string;
  originalName: string;
  mediaType: string;
  byteLength: number;
  scanState: string;
  lifecycle: string;
  currentRevision: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
};
export type FilePage = { items: FileRecord[]; nextCursor: string | null };
export type FileUploadSession = {
  id: string;
  expectedBytes: number;
  receivedBytes: number;
  state: string;
  expiresAt: string;
  uploadHandle: string;
  etag: string;
};
export type FileReferenceRecord = {
  id: string;
  fileObjectId: string;
  resourceType: string;
  resourceId: string;
  versionNumber: number | null;
  purpose: string;
  referenceKey: string;
  createdAt: string;
  etag: string;
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

  throw new NexoraApiError('Trình duyệt không hỗ trợ tạo UUID an toàn cho mutation.', 0, 'IdempotencyKeyUnavailable');
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

  const rotatedCsrf = response.headers.get('X-CSRF-Token');
  if (rotatedCsrf) {
    csrfToken = rotatedCsrf;
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

async function rawUploadFetch<T>(path: string, body: BodyInit, contentType: string, uploadHandle: string, idempotencyKey: string): Promise<T> {
  if (csrfToken === null) await getCsrf();
  const headers = new Headers({
    Accept: 'application/json',
    'Content-Type': contentType || 'application/octet-stream',
    'X-Upload-Handle': uploadHandle,
    'Idempotency-Key': idempotencyKey
  });
  if (csrfToken !== null) headers.set('X-CSRF-Token', csrfToken);
  let response: Response;
  try {
    response = await fetch(path, { method: 'PUT', credentials: 'same-origin', cache: 'no-store', headers, body });
  } catch {
    throw new NexoraApiError('Không thể kết nối Nexora API local.', 0, 'NetworkUnavailable');
  }
  if (!response.ok) throw await toApiError(response);
  const rotatedCsrf = response.headers.get('X-CSRF-Token');
  if (rotatedCsrf) csrfToken = rotatedCsrf;
  return (await response.json()) as T;
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

export function searchResources(
  query = '',
  resourceType = '',
  from = '',
  to = '',
  includeArchived = false,
  limit = 25
) {
  const params = new URLSearchParams();
  if (query.trim()) params.set('q', query.trim());
  if (resourceType) params.set('resourceType', resourceType);
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  if (includeArchived) params.set('includeArchived', 'true');
  params.set('limit', String(limit));
  return apiFetch<SearchPage>(`/api/v1/search?${params.toString()}`);
}

export function listFavorites(resourceType = '', limit = 100, cursor = '') {
  const params = new URLSearchParams();
  if (resourceType) params.set('resourceType', resourceType);
  params.set('limit', String(limit));
  if (cursor) params.set('cursor', cursor);
  return apiFetch<FavoritePage>(`/api/v1/favorites?${params.toString()}`);
}

export function addFavorite(resourceType: string, resourceId: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FavoriteRecord>('/api/v1/favorites', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ resourceType, resourceId })
  });
}

export function removeFavorite(favoriteId: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/favorites/${encodeURIComponent(favoriteId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function reorderFavorite(favoriteId: string, etag: string, rank: number, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FavoriteRecord>(`/api/v1/favorites/${encodeURIComponent(favoriteId)}/rank`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ rank })
  });
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

export function createProject(name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey(), confirmTaskBounds = false) {
  return apiFetch<ProjectRecord>('/api/v1/projects', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes, confirmTaskBounds })
  });
}

export function updateProject(id: string, etag: string, name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey(), confirmTaskBounds = false) {
  return apiFetch<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes, confirmTaskBounds })
  });
}

export function deleteProject(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionProject(id: string, etag: string, status: string, reason: string | null, idempotencyKey = createIdempotencyKey(), confirm = false) {
  return apiFetch<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status, reason, confirm })
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
  idempotencyKey = createIdempotencyKey(),
  confirmProjectTimeBounds = false
) {
  return apiFetch<TaskRecord>('/api/v1/tasks', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ projectId, title, description, status, dueAt, startAt, endAt, priority, tagsJson, acceptanceCriteriaJson, rank, reminderAt, confirmProjectTimeBounds })
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
  idempotencyKey = createIdempotencyKey(),
  confirmProjectTimeBounds = false
) {
  return apiFetch<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ projectId, title, description, status, dueAt, startAt, endAt, priority, tagsJson, acceptanceCriteriaJson, rank, reminderAt, confirmProjectTimeBounds })
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

export function runDeveloperTool(
  toolCode: string,
  input: string,
  options: Record<string, string> = {},
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<ToolboxRunResult>('/api/v1/developer/tools/run', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ toolCode, input, options })
  });
}

export function getReminderSource(sourceType: 'Task' | 'CalendarEvent', sourceId: string) {
  return apiFetch<ReminderSourceView>(`/api/v1/reminders/${encodeURIComponent(sourceType)}/${encodeURIComponent(sourceId)}`);
}

export function setReminder(
  sourceType: 'Task' | 'CalendarEvent',
  sourceId: string,
  configType: 'None' | 'BeforeStart15m' | 'Exact',
  exactAt: string | null,
  sourceETag: string,
  reminderETag: string | null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<ReminderSourceView>(`/api/v1/reminders/${encodeURIComponent(sourceType)}/${encodeURIComponent(sourceId)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, reminderETag ? { 'If-Match': reminderETag } : undefined),
    body: JSON.stringify({ configType, exactAt, sourceETag })
  });
}

export function removeReminder(
  sourceType: 'Task' | 'CalendarEvent',
  sourceId: string,
  sourceETag: string,
  reminderETag: string,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<void>(`/api/v1/reminders/${encodeURIComponent(sourceType)}/${encodeURIComponent(sourceId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': reminderETag }),
    body: JSON.stringify({ sourceETag })
  });
}

export function listPlanner(from: string, to = from) {
  const params = new URLSearchParams({ from, to });
  return apiFetch<PlannerPlan>(`/api/v1/planner?${params.toString()}`);
}

export function pinPlannerTask(taskId: string, planDate: string, notes: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<PlannerPinRecord>('/api/v1/planner/pins', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ taskId, planDate, notes })
  });
}

export function updatePlannerPin(
  pinId: string,
  etag: string,
  planDate: string,
  notes: string | null,
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<PlannerPinRecord>(`/api/v1/planner/pins/${encodeURIComponent(pinId)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ planDate, notes })
  });
}

export function reorderPlanner(planDate: string, pinIds: string[], etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<PlannerPlan>('/api/v1/planner/reorder', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ planDate, pinIds })
  });
}

export function unpinPlannerTask(pinId: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/planner/pins/${encodeURIComponent(pinId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function listHabits(state = '', query = '', limit = 100) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (state.trim()) params.set('state', state.trim());
  if (query.trim()) params.set('query', query.trim());
  return apiFetch<HabitPage>(`/api/v1/habits?${params.toString()}`);
}

export function getHabit(id: string, from?: string, to?: string) {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  const query = params.size ? `?${params.toString()}` : '';
  return apiFetch<HabitDetail>(`/api/v1/habits/${encodeURIComponent(id)}${query}`);
}

export function createHabit(
  value: {
    title: string; kind: 'Boolean' | 'Count'; targetCount: number | null; unit: string | null;
    effectiveFrom: string; weekdayMask: number; timeZoneId: string; reminderLocalTime: string | null;
  },
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<HabitDetail>('/api/v1/habits', {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify(value)
  });
}

export function updateHabit(
  id: string,
  etag: string,
  value: { title: string; unit: string | null; reminderLocalTime: string | null; timeZoneId: string },
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<HabitDetail>(`/api/v1/habits/${encodeURIComponent(id)}`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  });
}

export function setHabitSchedule(
  id: string,
  etag: string,
  value: { effectiveFrom: string; weekdayMask: number; targetCount: number | null },
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<HabitDetail>(`/api/v1/habits/${encodeURIComponent(id)}/schedule`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  });
}

export function recordHabitCheckIn(
  id: string,
  etag: string,
  value: { localDate: string; count: number; note: string | null },
  idempotencyKey = createIdempotencyKey()
) {
  return apiFetch<HabitDetail>(`/api/v1/habits/${encodeURIComponent(id)}/check-ins`, {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  });
}

export function transitionHabit(id: string, etag: string, state: 'Active' | 'Paused' | 'Archived' | 'Unarchive', idempotencyKey = createIdempotencyKey()) {
  return apiFetch<HabitDetail>(`/api/v1/habits/${encodeURIComponent(id)}/transition`, {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ state })
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

export function listShareLinks(limit = 100) {
  return apiFetch<ShareLinkPage>(`/api/v1/sharing/links?limit=${encodeURIComponent(limit)}`);
}

export function createShareLink(resourceType: string, resourceId: string, mode: string, expiresAt: string | null, allowedUserIds: string[] = [], noExpiry = false, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ShareLinkRecord>('/api/v1/sharing/links', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ resourceType, resourceId, mode, expiresAt, noExpiry, allowedUserIds: allowedUserIds.length ? allowedUserIds : null })
  });
}

export function updateShareLink(id: string, etag: string, mode: string, expiresAt: string | null, allowedUserIds: string[] = [], noExpiry = false, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<ShareLinkRecord>(`/api/v1/sharing/links/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ mode, expiresAt, noExpiry, allowedUserIds: allowedUserIds.length ? allowedUserIds : null })
  });
}

export function revokeShareLink(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/sharing/links/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function resolveShareLink(token: string) {
  return apiFetch<SharedResource>(`/api/v1/sharing/resolve/${encodeURIComponent(token)}`);
}

export function listSupportGrants(limit = 100) {
  return apiFetch<SupportGrantPage>(`/api/v1/support/grants?limit=${encodeURIComponent(limit)}`);
}

export function grantSupportConsent(moduleCode: string, durationMode: string, expiresAt: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SupportGrantRecord>('/api/v1/support/grants', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ moduleCode, durationMode, expiresAt })
  });
}

export function revokeSupportConsent(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/support/grants/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function listSupportSessions(limit = 100) {
  return apiFetch<SupportSessionPage>(`/api/v1/support/sessions?limit=${encodeURIComponent(limit)}`);
}

export function openSupportSession(targetUserId: string, moduleCode: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SupportSessionRecord>('/api/v1/support/sessions', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ targetUserId, moduleCode })
  });
}

export function openEmergencySession(targetUserId: string, moduleCode: string, reason: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<SupportSessionRecord>('/api/v1/support/emergency', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ targetUserId, moduleCode, reason })
  });
}

export function endSupportSession(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/support/sessions/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function listFiles(limit = 100) {
  return apiFetch<FilePage>(`/api/v1/files?limit=${encodeURIComponent(limit)}`);
}

export function initiateFileUpload(originalName: string, mediaType: string, expectedBytes: number, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FileUploadSession>('/api/v1/files/upload-sessions', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ originalName, mediaType, expectedBytes })
  });
}

export function completeFileUpload(session: FileUploadSession, file: File, idempotencyKey = createIdempotencyKey()) {
  return rawUploadFetch<FileRecord>(`/api/v1/files/upload-sessions/${encodeURIComponent(session.id)}/content`, file,
    file.type, session.uploadHandle, idempotencyKey);
}

export function cancelFileUpload(session: FileUploadSession, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/files/upload-sessions/${encodeURIComponent(session.id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'X-Upload-Handle': session.uploadHandle })
  });
}

export function renameFile(id: string, etag: string, originalName: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FileRecord>(`/api/v1/files/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ originalName })
  });
}

export function fileContentUrl(id: string, inline = false) {
  return `/api/v1/files/${encodeURIComponent(id)}/content?inline=${inline ? 'true' : 'false'}`;
}

export function attachFile(fileId: string, resourceType: string, resourceId: string, purpose: string, referenceKey: string, versionNumber: number | null = null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FileReferenceRecord>(`/api/v1/files/${encodeURIComponent(fileId)}/references`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ resourceType, resourceId, versionNumber, purpose, referenceKey })
  });
}

export function detachFile(referenceId: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/files/references/${encodeURIComponent(referenceId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function trashFile(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/files/${encodeURIComponent(id)}/trash`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function restoreFile(id: string, etag: string, deletionBatchId: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<FileRecord>(`/api/v1/files/${encodeURIComponent(id)}/restore`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ deletionBatchId })
  });
}

export function purgeFile(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/files/${encodeURIComponent(id)}/purge`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

