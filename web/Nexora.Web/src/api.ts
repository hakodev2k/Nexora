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
  canViewAdminAccess?: boolean;
  canViewModuleCatalog?: boolean;
  canManageModulePolicy?: boolean;
  modules: ModuleProjection[];
};

export type DashboardItem = {
  id: string;
  kind: string;
  title: string;
  status: string | null;
  at: string | null;
  detail: string | null;
  startDate?: string | null;
  endDateExclusive?: string | null;
  endAt?: string | null;
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
  drilldownToken?: string | null;
};

export type DashboardDrilldownPage = { widget: DashboardWidget; timeZoneId: string; capturedAt: string; localDate: string; nextToken: string | null; previousToken: string | null };
export function getDashboardDrilldown(token: string, signal?: AbortSignal) {
  return apiFetch<DashboardDrilldownPage>(`/api/v1/dashboard/drilldown?${new URLSearchParams({ token })}`, { signal });
}

export type DashboardSnapshot = {
  timeZoneId: string;
  generatedAt: string;
  widgets: DashboardWidget[];
};

export type DashboardWidgetConfig = { id: string | null; key: string; providerVersion: string; configVersion: number; widthUnits: number; enabled: boolean; options: Record<string, never> };
export type DashboardLayout = { id: string; schemaVersion: number; layoutVersion: number; widgets: DashboardWidgetConfig[]; etag: string };
export function getDashboardLayout(signal?: AbortSignal) {
  return apiFetch<{ layout: DashboardLayout | null }>('/api/v1/dashboard/layout', { signal });
}
export async function saveDashboardLayout(current: DashboardLayout | null, widgets: DashboardWidgetConfig[], key: string) {
  const result = await apiFetch<DashboardLayout | ProductivityCommittedAcknowledgement>('/api/v1/dashboard/layout', {
    method: current ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, current ? { 'If-Match': current.etag } : {}), body: JSON.stringify({ schemaVersion: 1, widgets })
  }, current ? 200 : 201);
  if (!result || typeof result.id !== 'string' || !/^[0-9a-f-]{36}$/i.test(result.id) || (current && result.id.toLowerCase() !== current.id.toLowerCase())
      || (('committed' in result || 'detailAvailable' in result) ? !isProductivityAcknowledgement(result) : !Array.isArray(result.widgets) || typeof result.etag !== 'string'))
    throw new NexoraApiError('Chưa xác nhận được bố cục. Giữ nguyên thao tác và thử lại để đối chiếu.', 0, 'UnexpectedAcknowledgement');
  return result;
}

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

export type SavedSearchDefinition = { text: string; resourceType: string | null; from: string | null; to: string | null; includeArchived: boolean; sort: 'relevance' };
export type SavedSearchRecord = { id: string; title: string; schemaVersion: number; query: SavedSearchDefinition | null; updatedAt: string; etag: string; state: 'Compatible' | 'MigrationRequired' };
export function listSavedSearches(cursor = '') {
  return apiFetch<{ items: SavedSearchRecord[]; nextCursor: string | null }>(`/api/v1/saved-searches?cursor=${encodeURIComponent(cursor)}`);
}
export async function saveSavedSearch(id: string | null, etag: string | null, title: string, query: SavedSearchDefinition, key: string) {
  const result = await apiFetch<SavedSearchRecord | ProductivityCommittedAcknowledgement>(`/api/v1/saved-searches${id ? '/' + encodeURIComponent(id) : ''}`, {
    method: id ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, id && etag ? { 'If-Match': etag } : {}), body: JSON.stringify({ title, schemaVersion: 1, query })
  }, id ? 200 : 201);
  if (!result || typeof result.id !== 'string' || !/^[0-9a-f-]{36}$/i.test(result.id) || (id && result.id.toLowerCase() !== id.toLowerCase())
      || (('committed' in result || 'detailAvailable' in result) ? !isProductivityAcknowledgement(result)
        : typeof result.title !== 'string' || typeof result.etag !== 'string' || result.schemaVersion !== 1))
    throw new NexoraApiError('Chưa xác nhận được Saved Search. Giữ nguyên thao tác để thử lại.', 0, 'UnexpectedAcknowledgement');
  return result;
}
export function deleteSavedSearch(item: SavedSearchRecord, key: string) {
  return apiFetch<void>(`/api/v1/saved-searches/${encodeURIComponent(item.id)}`, { method: 'DELETE', headers: jsonMutationHeaders(key, { 'If-Match': item.etag }) }, 204);
}
export function runSavedSearch(id: string, cursor = '', signal?: AbortSignal) {
  return apiFetch<SearchPage>(`/api/v1/saved-searches/${encodeURIComponent(id)}/run?cursor=${encodeURIComponent(cursor)}`, { method: 'POST', signal });
}

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
  priority: string | null;
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
  startDate?: string | null;
  endDateExclusive?: string | null;
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
  taskTitle: string | null;
  taskStatus: string | null;
  projectName: string | null;
  startAt: string | null;
  endAt: string | null;
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
  folderId?: string | null;
  parentPageId?: string | null;
  depth?: number;
  title: string;
  documentType: string;
  editorMode: string;
  status: string;
  preArchiveStatus: string | null;
  versionNumber: number;
  createdAt: string;
  updatedAt: string;
  etag: string;
  tagId?: string | null;
  tagName?: string | null;
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
  sourceUnavailable?: boolean;
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
  usageCount: number | null;
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
  sourceETag?: string | null;
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
  preArchiveStatus?: string | null;
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
  initialValueExact?: string | null;
  currentValueExact?: string | null;
  targetValueExact?: string | null;
  booleanValue?: boolean | null;
  progress: number;
  updatedAt: string;
  etag: string;
};

export type GoalDetail = {
  goal: GoalRecord;
  targets: GoalTargetRecord[];
};

export type GoalProgressEntry = {
  id: string; targetId: string; kind: 'Numeric' | 'Boolean'; currentTargetTitle: string;
  valueExact: string | null; checked: boolean | null; note: string | null; recordedAt: string; targetRemoved?: boolean;
};
export type GoalProgressPage = { items: GoalProgressEntry[]; nextCursor: string | null };
export type GoalStateEntry = {id:string;actionKey:string;fromStatus:string;toStatus:string;recordedAt:string};
export type GoalStatePage = {items:GoalStateEntry[];nextCursor:string|null};
export type GoalMetadataEntry={id:string;event:'Created'|'Updated'|'HistoryStarted';title:string;description:string|null;startDate:string|null;endDate:string|null;capturedAt:string;sourceUpdatedAt:string};
export type GoalMetadataPage={items:GoalMetadataEntry[];nextCursor:string|null};
export type GoalTargetDefinitionEntry={id:string;targetId:string;event:'Created'|'Updated'|'Removed'|'HistoryStarted';kind:'Numeric'|'Boolean';title:string;initialValueExact:string|null;targetValueExact:string|null;removed:boolean;capturedAt:string;sourceUpdatedAt:string};
export type GoalTargetDefinitionPage={items:GoalTargetDefinitionEntry[];nextCursor:string|null};

export async function getGoalTargetDefinitionHistory(goalId:string,cursor:string|null,signal?:AbortSignal) {
  const page=await apiFetch<GoalTargetDefinitionPage>(`/api/v1/goals/${encodeURIComponent(goalId)}/target-history${cursor?'?cursor='+encodeURIComponent(cursor):''}`,{signal});
  const guid=(value:unknown)=>typeof value==='string'&&/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value);
  const exact=(value:unknown)=>typeof value==='string'&&/^-?\d{1,20}(?:\.\d{1,8})?$/.test(value);
  if(!page||!Array.isArray(page.items)||page.items.length>25||(page.nextCursor!==null&&(typeof page.nextCursor!=='string'||page.nextCursor.length>1024))
    ||page.items.some(item=>!item||!guid(item.id)||!guid(item.targetId)||!['Created','Updated','Removed','HistoryStarted'].includes(item.event)
      ||typeof item.title!=='string'||!item.title.trim()||item.title.length>200||typeof item.removed!=='boolean'||item.removed!==(item.event==='Removed')
      ||(item.kind==='Numeric'?!exact(item.initialValueExact)||!exact(item.targetValueExact):item.kind!=='Boolean'||item.initialValueExact!==null||item.targetValueExact!==null)
      ||typeof item.capturedAt!=='string'||!Number.isFinite(Date.parse(item.capturedAt))||typeof item.sourceUpdatedAt!=='string'||!Number.isFinite(Date.parse(item.sourceUpdatedAt)))
    ||(page.nextCursor!==null&&page.items.length!==25))throw new NexoraApiError('Dữ liệu phiên bản target không hợp lệ. Vui lòng tải lại.',0,'InvalidHistoryResponse');
  return page;
}

export async function getGoalMetadataHistory(goalId:string,cursor:string|null,signal?:AbortSignal) {
  const query=cursor?'?cursor='+encodeURIComponent(cursor):'';
  const page=await apiFetch<GoalMetadataPage>(`/api/v1/goals/${encodeURIComponent(goalId)}/metadata-history${query}`,{signal});
  const day=(value:unknown)=>value===null || (typeof value==='string'&&/^\d{4}-\d{2}-\d{2}$/.test(value)&&Number.isFinite(Date.parse(value+'T00:00:00Z'))&&new Date(value+'T00:00:00Z').toISOString().slice(0,10)===value);
  if(!page||!Array.isArray(page.items)||page.items.length>25||(page.nextCursor!==null&&(typeof page.nextCursor!=='string'||page.nextCursor.length>1024))
    ||page.items.some(item=>!item||typeof item.id!=='string'||!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(item.id)
      ||!['Created','Updated','HistoryStarted'].includes(item.event)||typeof item.title!=='string'||!item.title.trim()||item.title.length>200
      ||(item.description!==null&&(typeof item.description!=='string'||item.description.length>20000))||!day(item.startDate)||!day(item.endDate)
      ||(item.startDate!==null&&item.endDate!==null&&item.endDate<item.startDate)
      ||typeof item.capturedAt!=='string'||!Number.isFinite(Date.parse(item.capturedAt))||typeof item.sourceUpdatedAt!=='string'||!Number.isFinite(Date.parse(item.sourceUpdatedAt)))
    ||(page.nextCursor!==null&&page.items.length!==25))throw new NexoraApiError('Dữ liệu phiên bản Goal không hợp lệ. Vui lòng tải lại.',0,'InvalidHistoryResponse');
  return page;
}

export async function getGoalStateHistory(goalId:string,cursor:string|null,signal?:AbortSignal) {
  const query=cursor?'?cursor='+encodeURIComponent(cursor):'';
  const page=await apiFetch<GoalStatePage>(`/api/v1/goals/${encodeURIComponent(goalId)}/state-history${query}`,{signal});
  const validPair=(item:GoalStateEntry)=>{
    switch(item.actionKey) {
      case 'goals.goal.start':return item.fromStatus==='Draft'&&item.toStatus==='Active';
      case 'goals.goal.reopen':return ['Completed','Abandoned'].includes(item.fromStatus)&&item.toStatus==='Active';
      case 'goals.goal.complete':return item.fromStatus==='Active'&&item.toStatus==='Completed';
      case 'goals.goal.abandon':return ['Draft','Active'].includes(item.fromStatus)&&item.toStatus==='Abandoned';
      case 'goals.goal.archive':return ['Draft','Active','Completed','Abandoned'].includes(item.fromStatus)&&item.toStatus==='Archived';
      case 'goals.goal.unarchive':return item.fromStatus==='Archived'&&['Draft','Active','Completed','Abandoned'].includes(item.toStatus);
      default:return false;
    }
  };
  if(!page || !Array.isArray(page.items) || page.items.length>25 || (page.nextCursor!==null && (typeof page.nextCursor!=='string'||page.nextCursor.length>1024))
      || page.items.some(item=>!item || typeof item.id!=='string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(item.id)
        || typeof item.recordedAt!=='string' || !Number.isFinite(Date.parse(item.recordedAt)) || !validPair(item))
      || (page.nextCursor!==null && page.items.length!==25))
    throw new NexoraApiError('Dữ liệu lịch sử trạng thái không hợp lệ. Vui lòng tải lại.',0,'InvalidHistoryResponse');
  return page;
}

export async function getGoalProgressHistory(goalId: string, cursor: string | null, signal?: AbortSignal) {
  const query = cursor ? `?cursor=${encodeURIComponent(cursor)}` : '';
  const result = await apiFetch<GoalProgressPage>(`/api/v1/goals/${encodeURIComponent(goalId)}/progress-history${query}`, { signal });
  if (!result || !Array.isArray(result.items) || result.items.length > 25
      || (result.nextCursor !== null && typeof result.nextCursor !== 'string')
      || result.items.some(item => !item || typeof item.id !== 'string' || typeof item.targetId !== 'string'
        || typeof item.currentTargetTitle !== 'string' || !Number.isFinite(Date.parse(item.recordedAt))
        || (item.note !== null && typeof item.note !== 'string')
        || (item.kind === 'Numeric' ? typeof item.valueExact !== 'string' || item.checked !== null
          : item.kind !== 'Boolean' || typeof item.checked !== 'boolean' || item.valueExact !== null)))
    throw new NexoraApiError('Dữ liệu lịch sử không hợp lệ. Vui lòng tải lại.', 0, 'InvalidHistoryResponse');
  return result;
}

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
  moduleGrants: { moduleId: string; code: string; enabled: boolean; state: string; systemEnabled: boolean }[];
};

export type AdminAccessChange =
  | { kind: 'role'; role: string }
  | { kind: 'permissions'; changes: { actionKey: string; effect: 'Allow' | 'Deny' | 'Unset' }[] }
  | { kind: 'modules'; changes: { moduleId: string; enabled: boolean }[] };

export type AdminAccessPreview = {
  previewToken: string;
  expiresAt: string;
  etag: string;
  changes: { field: string; before: string | null; after: string | null }[];
  blockers: { code: string; message: string }[];
};

export type AdminModuleRecord = {
  id: string;
  code: string;
  name: string;
  state: string;
  systemEnabled: boolean;
  registrationEnabled: boolean;
  sharingEnabled: boolean;
  policyRevision: string;
  etag: string;
  requiredDependencies: string[];
  requiredBy: string[];
  unavailableReason: string | null;
};

export type AdminModulePage = { items: AdminModuleRecord[]; nextCursor: string | null };

export type ModulePolicyPreview = {
  previewToken: string;
  expiresAt: string;
  etag: string;
  changes: { field: string; before: string; after: string }[];
  blockers: { code: string; message: string; field: string | null }[];
  affectedSharingLinks: number;
  affectedUsers: number;
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
  startAt: string;
  endAt: string;
  priority: string | null;
  tagsJson: string;
  isOverdue: boolean;
  acceptanceCriteriaJson: string | null;
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
let csrfRefreshPromise: Promise<CsrfEnvelope> | null = null;
let currentProfileETag: string | null = null;

export function createIdempotencyKey(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }

  throw new NexoraApiError('Trình duyệt không hỗ trợ tạo UUID an toàn cho mutation.', 0, 'IdempotencyKeyUnavailable');
}

export async function getCsrf(force = false): Promise<CsrfEnvelope> {
  if (!force && csrfToken !== null) {
    return { requestToken: csrfToken, tokenType: 'csrf', expiresInSeconds: 1800 };
  }
  if (csrfRefreshPromise !== null) {
    return csrfRefreshPromise;
  }

  csrfToken = null;
  const refresh = requestCsrfToken();
  csrfRefreshPromise = refresh;
  try {
    return await refresh;
  } finally {
    if (csrfRefreshPromise === refresh) {
      csrfRefreshPromise = null;
    }
  }
}

async function requestCsrfToken(): Promise<CsrfEnvelope> {
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

export async function apiFetch<T>(path: string, init: RequestInit = {}, expectedStatus?: 200 | 201 | 202 | 204 | readonly (200 | 201 | 202 | 204)[]): Promise<T> {
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
  for (let attempt = 0; ; attempt += 1) {
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

    if (response.ok) break;

    const apiError = await toApiError(response);
    // The endpoint filter rejects CSRF before invoking a mutation handler, so
    // this one retry cannot duplicate a business effect. The same body and
    // idempotency key are retained; all other failures are surfaced.
    if (unsafe && attempt === 0 && response.status === 403 && apiError.code === 'CsrfInvalid' &&
        (init.body === undefined || typeof init.body === 'string')) {
      await getCsrf(true);
      if (csrfToken !== null) headers.set('X-CSRF-Token', csrfToken);
      continue;
    }
    if (response.status === 401) {
      currentProfileETag = null;
    }
    throw apiError;
  }

  const rotatedCsrf = response.headers.get('X-CSRF-Token');
  if (rotatedCsrf) {
    csrfToken = rotatedCsrf;
  }

  if (path === '/api/v1/me') {
    currentProfileETag = response.headers.get('ETag');
  }

  if (expectedStatus !== undefined && !(Array.isArray(expectedStatus) ? expectedStatus.includes(response.status as 200 | 201 | 202 | 204) : response.status === expectedStatus)) {
    throw new NexoraApiError('Không thể xác nhận acknowledgement của API.', 0, 'UnexpectedAcknowledgement');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get('Content-Type') ?? '';
  if (!contentType.includes('json')) {
    if (expectedStatus !== undefined) throw new NexoraApiError('Không thể xác nhận kết quả API.', 0, 'UnexpectedAcknowledgement');
    return undefined as T;
  }

  return (await response.json()) as T;
}

export async function apiFetchBlob(path: string): Promise<Blob> {
  let response: Response;
  try { response = await fetch(path, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'text/calendar' } }); }
  catch { throw new NexoraApiError('Không thể kết nối Nexora API local.', 0, 'NetworkUnavailable'); }
  if (!response.ok) throw await toApiError(response);
  const rotatedCsrf = response.headers.get('X-CSRF-Token'); if (rotatedCsrf) csrfToken = rotatedCsrf;
  const blob = await response.blob();
  if (blob.size > 1048576 || !blob.type.toLowerCase().startsWith('text/calendar')) throw new NexoraApiError('File ICS trả về không hợp lệ.', 0, 'ArtifactUnavailable');
  return blob;
}

export async function documentExportBlob(id: string, format: 'MD' | 'DOCX'): Promise<Blob> {
  const expected = format === 'MD' ? 'text/markdown' : 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
  let response: Response;
  try { response = await fetch(`/api/v1/document-exports/${encodeURIComponent(id)}/content`, { credentials: 'same-origin', cache: 'no-store', headers: { Accept: expected } }); }
  catch { throw new NexoraApiError('Không thể kết nối để tải artifact.', 0, 'NetworkUnavailable'); }
  if (!response.ok) throw await toApiError(response);
  const rotated = response.headers.get('X-CSRF-Token'); if (rotated) csrfToken = rotated;
  if (response.headers.get('Content-Type')?.split(';')[0].trim().toLowerCase() !== expected || Number(response.headers.get('Content-Length') ?? 0) > 5242880)
    throw new NexoraApiError('Artifact trả về không hợp lệ.', 0, 'ArtifactUnavailable');
  if (!response.body) {
    if (format === 'MD' && Number(response.headers.get('Content-Length') ?? 0) === 0) return new Blob([], { type: expected });
    throw new NexoraApiError('Artifact trả về không hợp lệ.', 0, 'ArtifactUnavailable');
  }
  const reader = response.body.getReader(), chunks: Uint8Array<ArrayBuffer>[] = []; let length = 0;
  try { for (;;) { const chunk = await reader.read(); if (chunk.done) break; length += chunk.value.byteLength; if (length > 5242880) { await reader.cancel(); throw new NexoraApiError('Artifact vượt giới hạn.', 0, 'ArtifactUnavailable'); } chunks.push(new Uint8Array(chunk.value)); } }
  finally { reader.releaseLock(); }
  if (format === 'DOCX' && length < 4) throw new NexoraApiError('Artifact DOCX không hợp lệ.', 0, 'ArtifactUnavailable');
  return new Blob(chunks, { type: expected });
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

export function jsonMutationHeaders(idempotencyKey?: string, extra?: HeadersInit): HeadersInit {
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
  for (let attempt = 0; ; attempt += 1) {
    try {
      response = await fetch(path, { method: 'PUT', credentials: 'same-origin', cache: 'no-store', headers, body });
    } catch {
      throw new NexoraApiError('Không thể kết nối Nexora API local.', 0, 'NetworkUnavailable');
    }

    if (response.ok) break;

    const apiError = await toApiError(response);
    // CSRF validation runs before the upload handler. Retain the same
    // idempotency key and body for one bounded retry after a cookie rotation;
    // no business effect has started when this filter rejects the request.
    if (attempt === 0 && response.status === 403 && apiError.code === 'CsrfInvalid') {
      await getCsrf(true);
      if (csrfToken !== null) headers.set('X-CSRF-Token', csrfToken);
      continue;
    }

    throw apiError;
  }
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

export function getDashboard(signal?: AbortSignal, refresh = false) {
  return apiFetch<DashboardSnapshot>(refresh ? '/api/v1/dashboard/refresh' : '/api/v1/dashboard', { signal });
}

export function searchResources(
  query = '',
  resourceType = '',
  from = '',
  to = '',
  includeArchived = false,
  limit = 25,
  signal?: AbortSignal,
  cursor = ''
) {
  const params = new URLSearchParams();
  if (query.trim()) params.set('q', query.trim());
  if (resourceType) params.set('resourceType', resourceType);
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  if (includeArchived) params.set('includeArchived', 'true');
  params.set('limit', String(limit));
  if (cursor) params.set('cursor', cursor);
  return apiFetch<SearchPage>(`/api/v1/search?${params.toString()}`, { signal });
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

export type ProductivityCommittedAcknowledgement = { id: string; committed: true; detailAvailable: false };
export function isProductivityAcknowledgement<T>(value: T | ProductivityCommittedAcknowledgement): value is ProductivityCommittedAcknowledgement {
  return !!value && typeof value === 'object' && 'committed' in value && value.committed === true
    && 'detailAvailable' in value && value.detailAvailable === false;
}

async function productivityMutation<T extends { id: string }>(path: string, init: RequestInit, expectedId?: string): Promise<T | ProductivityCommittedAcknowledgement> {
  const value = await apiFetch<T | ProductivityCommittedAcknowledgement>(path, init, expectedId ? 200 : 201);
  const validId = !!value && typeof value === 'object' && typeof value.id === 'string'
    && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.id)
    && value.id !== '00000000-0000-0000-0000-000000000000'
    && (!expectedId || value.id.toLowerCase() === expectedId.toLowerCase());
  const hasAcknowledgement = !!value && typeof value === 'object' && ('committed' in value || 'detailAvailable' in value);
  if (!validId || (hasAcknowledgement && !isProductivityAcknowledgement(value)))
    throw new NexoraApiError('Chưa xác nhận được kết quả đã lưu. Giữ nguyên thao tác và idempotency key để đối chiếu; không tạo thao tác mới.', 0, 'UnexpectedAcknowledgement');
  return value;
}

export function listProjects(limit = 25, cursor = '') {
  const query = new URLSearchParams({ limit: String(limit) });
  if (cursor) query.set('cursor', cursor);
  return apiFetch<ProjectPage>(`/api/v1/projects?${query.toString()}`);
}

export function getProject(id: string) {
  return apiFetch<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}`);
}

export function createProject(name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey(), confirmTaskBounds = false) {
  return productivityMutation<ProjectRecord>('/api/v1/projects', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes, confirmTaskBounds })
  });
}

export function updateProject(id: string, etag: string, name: string, description: string | null, startAt: string, endAt: string, priority = 'P3', tagsJson: string | null = null, notes: string | null = null, idempotencyKey = createIdempotencyKey(), confirmTaskBounds = false) {
  return productivityMutation<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ name, description, startAt, endAt, priority, tagsJson, notes, confirmTaskBounds })
  }, id);
}

export function deleteProject(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/projects/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionProject(id: string, etag: string, status: string, reason: string | null, idempotencyKey = createIdempotencyKey(), confirm = false) {
  return productivityMutation<ProjectRecord>(`/api/v1/projects/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status, reason, confirm })
  }, id);
}

export function listTasks(projectId?: string, limit = 25, cursor = '') {
  const query = new URLSearchParams({ limit: String(limit) });
  if (projectId) query.set('projectId', projectId);
  if (cursor) query.set('cursor', cursor);
  return apiFetch<TaskPage>(`/api/v1/tasks?${query.toString()}`);
}

export function getTask(id: string) {
  return apiFetch<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}`);
}

export function createTask(
  projectId: string,
  title: string,
  description: string | null,
  status: string,
  dueAt: string | null,
  startAt: string,
  endAt: string,
  priority: string | null = null,
  tagsJson: string | null = null,
  acceptanceCriteriaJson: string | null = null,
  rank = 0,
  reminderAt: string | null = null,
  idempotencyKey = createIdempotencyKey(),
  confirmProjectTimeBounds = false,
  manageReminder = false
) {
  return productivityMutation<TaskRecord>('/api/v1/tasks', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ projectId, title, description, status, dueAt, startAt, endAt, priority, tagsJson, acceptanceCriteriaJson, rank, reminderAt, confirmProjectTimeBounds, manageReminder })
  });
}

export type TaskEditInput = {
  title: string; description: string | null; dueAt: string | null; startAt: string; endAt: string;
  priority: string | null; tagsJson: string; confirmProjectTimeBounds?: boolean;
};

export function updateTask(id: string, etag: string, input: TaskEditInput, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify(input)
  }, id);
}

export function setTaskCriteria(id: string, etag: string, acceptanceCriteriaJson: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}/criteria`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ acceptanceCriteriaJson })
  }, id);
}

export function reorderTask(id: string, etag: string, rank: number, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}/rank`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ rank })
  }, id);
}

export function deleteTask(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/tasks/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionTask(id: string, etag: string, status: string, reason: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<TaskRecord>(`/api/v1/tasks/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status, reason })
  }, id);
}

export function listCalendarEvents(from?: string, to?: string, limit = 25, cursor = '') {
  const query = new URLSearchParams({ limit: String(limit) });
  if (from) query.set('from', from);
  if (to) query.set('to', to);
  if (cursor) query.set('cursor', cursor);
  return apiFetch<CalendarEventPage>(`/api/v1/calendar/events?${query.toString()}`);
}

export function getCalendarEvent(id: string) {
  return apiFetch<CalendarEventRecord>(`/api/v1/calendar/events/${encodeURIComponent(id)}`);
}

export function createCalendarEvent(
  title: string,
  description: string | null,
  startAt: string,
  endAt: string,
  timeZoneId: string,
  isAllDay = false,
  sourceUid: string | null = null,
  idempotencyKey = createIdempotencyKey(),
  dates?: { startDate: string; endDateExclusive: string }
) {
  return productivityMutation<CalendarEventRecord>('/api/v1/calendar/events', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, description, startAt, endAt, timeZoneId, isAllDay, sourceUid, ...dates })
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
  idempotencyKey = createIdempotencyKey(),
  dates?: { startDate: string; endDateExclusive: string }
) {
  return productivityMutation<CalendarEventRecord>(`/api/v1/calendar/events/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, description, startAt, endAt, timeZoneId, isAllDay, sourceUid, ...dates })
  }, id);
}

export function deleteCalendarEvent(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/calendar/events/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}

export function transitionCalendarEvent(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<CalendarEventRecord>(`/api/v1/calendar/events/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  }, id);
}

export function listDocuments(status?: string, limit = 25, cursor = '', titleQuery = '', documentType = '', signal?: AbortSignal, createdFrom = '', createdTo = '', tagId = '', folderId?: string, parentPageId?: string) {
  const query = new URLSearchParams({ limit: String(limit) });
  if (status) query.set('status', status);
  if (cursor) query.set('cursor', cursor);
  if (titleQuery.trim()) query.set('query', titleQuery.trim());
  if (documentType) query.set('documentType', documentType);
  if (createdFrom) query.set('createdFrom', createdFrom);
  if (createdTo) query.set('createdTo', createdTo);
  if (tagId) query.set('tagId', tagId);
  if (folderId) query.set('folderId', folderId);
  if (parentPageId) query.set('parentPageId', parentPageId);
  return apiFetch<DocumentPage>(`/api/v1/documents?${query.toString()}`, { signal });
}

export type DocumentVersionSummary = {
  id: string; pageId: string; versionNumber: number; title: string; documentType: string;
  editorMode: string; savedStatus: string; changeNote: string | null; createdAt: string;
  createdByUserId: string | null; tagSnapshotCaptured: boolean; tagId: string | null; tagName: string | null;
};
export type DocumentVersionPage = { items: DocumentVersionSummary[]; nextCursor: string | null };
export type DocumentVersionPreview = { snapshot: DocumentVersionSummary; body: string };

function validateDocumentVersion(value: DocumentVersionSummary, pageId: string) {
  if (!value || value.pageId?.toLowerCase() !== pageId.toLowerCase() || !Number.isSafeInteger(value.versionNumber) || value.versionNumber < 1 ||
      typeof value.id !== 'string' || typeof value.title !== 'string' || typeof value.tagSnapshotCaptured !== 'boolean')
    throw new NexoraApiError('Không xác nhận được phiên bản Document.', 0, 'UnexpectedResponse');
  return value;
}
export async function listDocumentVersions(id: string, cursor = '', signal?: AbortSignal) {
  const query = new URLSearchParams(); if (cursor) query.set('cursor', cursor);
  const page = await apiFetch<DocumentVersionPage>(`/api/v1/documents/${encodeURIComponent(id)}/versions?${query}`, { signal });
  if (!page || !Array.isArray(page.items) || page.items.length > 25 || (page.nextCursor !== null && typeof page.nextCursor !== 'string'))
    throw new NexoraApiError('Không xác nhận được danh sách phiên bản.', 0, 'UnexpectedResponse');
  page.items.forEach(item => validateDocumentVersion(item, id));
  return page;
}
export async function getDocumentVersion(id: string, version: number, signal?: AbortSignal) {
  const result = await apiFetch<DocumentVersionPreview>(`/api/v1/documents/${encodeURIComponent(id)}/versions/${version}`, { signal });
  if (!result || typeof result.body !== 'string' || validateDocumentVersion(result.snapshot, id).versionNumber !== version)
    throw new NexoraApiError('Không xác nhận được nội dung phiên bản.', 0, 'UnexpectedResponse');
  return result;
}

export function getDocument(id: string) {
  return apiFetch<DocumentRecord>(`/api/v1/documents/${encodeURIComponent(id)}`);
}

export async function compareDocumentVersions(id: string, before: number, after: number, signal?: AbortSignal) {
  const result = await apiFetch<{ before: DocumentVersionPreview; after: DocumentVersionPreview }>(
    `/api/v1/documents/${encodeURIComponent(id)}/versions/compare?before=${before}&after=${after}`, { signal });
  for (const [value, expected] of [[result?.before, before], [result?.after, after]] as const) {
    if (!value || typeof value.body !== 'string' || validateDocumentVersion(value.snapshot, id).versionNumber !== expected)
      throw new NexoraApiError('Không xác nhận được kết quả so sánh phiên bản.', 0, 'UnexpectedResponse');
  }
  return result;
}

export function restoreDocumentVersion(id: string, etag: string, versionNumber: number, tag: { id: string | null } | undefined, key: string) {
  return apiFetch<DocumentRecord | ProductivityCommittedAcknowledgement>(`/api/v1/documents/${encodeURIComponent(id)}/versions/restore`, {
    method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': etag }), body: JSON.stringify({ versionNumber, tag })
  }, 200).then(result => validateDocumentMutation(result, id));
}

function validateDocumentMutation(result: DocumentRecord | ProductivityCommittedAcknowledgement | void, expectedId?: string) {
  if (result === undefined) return result;
  if (!result || typeof result.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id) || (expectedId !== undefined && result.id.toLowerCase() !== expectedId.toLowerCase())) throw new NexoraApiError('Không thể xác nhận kết quả Document.', 0, 'UnexpectedAcknowledgement');
  if (isProductivityAcknowledgement(result)) return result;
  if (typeof result.title !== 'string' || typeof result.body !== 'string' || typeof result.etag !== 'string')
    throw new NexoraApiError('Không thể xác nhận chi tiết Document.', 0, 'UnexpectedAcknowledgement');
  return result;
}

export function createDocument(title: string, documentType: string, editorMode: string, body: string, idempotencyKey = createIdempotencyKey(), tagId?: string | null, scope?: { folderId?: string | null; parentPageId?: string | null }) {
  return apiFetch<DocumentRecord | ProductivityCommittedAcknowledgement | void>('/api/v1/documents', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, documentType, editorMode, body, tagId, ...scope })
  }, [201, 204]).then(validateDocumentMutation);
}

export function saveDocument(id: string, etag: string, title: string, body: string, changeNote: string | null = null, idempotencyKey = createIdempotencyKey(), tag?: { id: string | null }) {
  return apiFetch<DocumentRecord | ProductivityCommittedAcknowledgement | void>(`/api/v1/documents/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, body, changeNote, tag })
  }, [200, 204]).then(result => validateDocumentMutation(result, id));
}

export function transitionDocument(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<DocumentRecord | ProductivityCommittedAcknowledgement | void>(`/api/v1/documents/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  }, [200, 204]).then(result => validateDocumentMutation(result, id));
}

export function listNotifications(unreadOnly = false, limit = 25, cursor = '', filters: { category?: string; from?: string; to?: string } = {}) {
  const query = new URLSearchParams({ unreadOnly: String(unreadOnly), limit: String(limit) });
  if (cursor) query.set('cursor', cursor);
  if (filters.category) query.set('category', filters.category);
  if (filters.from) query.set('from', filters.from);
  if (filters.to) query.set('to', filters.to);
  return apiFetch<NotificationPage>(`/api/v1/notifications?${query.toString()}`);
}

export type DashboardCreateType = 'Project' | 'Task' | 'CalendarEvent' | 'Document' | 'FinanceRecord';
export async function openDashboardCreate(resourceType: DashboardCreateType) {
  const target = await apiFetch<{ resourceType: DashboardCreateType; moduleCode: string }>(`/api/v1/dashboard/quick-create/${encodeURIComponent(resourceType)}`);
  const modules = { Project: 'FX11', Task: 'FX12', CalendarEvent: 'FX13', Document: 'FX20', FinanceRecord: 'FX27' } as const;
  if (!target || target.resourceType !== resourceType || target.moduleCode !== modules[resourceType])
    throw new NexoraApiError('Luồng tạo không hợp lệ.', 0, 'UnexpectedResponse');
  return target;
}

export async function openNotificationSource(id: string) {
  const source = await apiFetch<{ resourceType: 'Task' | 'Event' | 'NewsArticle'; resourceId: string; title: string; route: string }>(`/api/v1/notifications/${encodeURIComponent(id)}/source`);
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  if (!source || !['Task', 'Event', 'NewsArticle'].includes(source.resourceType) || !uuid.test(source.resourceId) || typeof source.title !== 'string'
      || source.route !== ({ Task: '/tasks/', Event: '/calendar/events/', NewsArticle: '/news/articles/' }[source.resourceType]) + source.resourceId)
    throw new NexoraApiError('Nguồn thông báo không hợp lệ.', 0, 'UnexpectedResponse');
  return source;
}

export function markNotificationRead(id: string, etag: string, read: boolean, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<NotificationRecord>(`/api/v1/notifications/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ read })
  }, id);
}

export function markAllNotificationsRead(idempotencyKey = createIdempotencyKey()) {
  return apiFetch<{ watermark: string; updatedCount: number } | ProductivityCommittedAcknowledgement>('/api/v1/notifications/mark-all-read', {
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

export function listFinanceCategories(query = '', limit = 25, cursor = '') {
  const params = new URLSearchParams({ limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  if (cursor) params.set('cursor', cursor);
  return apiFetch<FinanceCategoryPage>(`/api/v1/finance/categories?${params.toString()}`);
}

export function getFinanceCategory(id: string) {
  return apiFetch<FinanceCategoryRecord>(`/api/v1/finance/categories/${encodeURIComponent(id)}`);
}

export function getFinanceRecord(id: string) {
  return apiFetch<FinanceManualRecord>(`/api/v1/finance/records/${encodeURIComponent(id)}`);
}

export function createFinanceCategory(title: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<FinanceCategoryRecord>('/api/v1/finance/categories', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, ifMatch: '*' })
  });
}

export function updateFinanceCategory(id: string, etag: string, title: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<FinanceCategoryRecord>(`/api/v1/finance/categories/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, ifMatch: etag })
  }, id);
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
  cursor?: string;
} = {}) {
  const params = new URLSearchParams({ limit: String(filters.limit ?? 25) });
  if (filters.categoryId) params.set('categoryId', filters.categoryId);
  if (filters.currencyCode?.trim()) params.set('currencyCode', filters.currencyCode.trim());
  if (filters.from) params.set('from', filters.from);
  if (filters.to) params.set('to', filters.to);
  if (filters.query?.trim()) params.set('query', filters.query.trim());
  if (filters.cursor) params.set('cursor', filters.cursor);
  return apiFetch<FinanceRecordPage>(`/api/v1/finance/records?${params.toString()}`);
}

export function createFinanceRecord(input: FinanceRecordInput, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<FinanceManualRecord>('/api/v1/finance/records', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify(input)
  });
}

export function updateFinanceRecord(id: string, etag: string, input: FinanceRecordInput, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<FinanceManualRecord>(`/api/v1/finance/records/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify(input)
  }, id);
}

export function listBookmarks(includeArchived = false, query = '', limit = 25, cursor = '') {
  const params = new URLSearchParams({ includeArchived: String(includeArchived), limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  if (cursor) params.set('cursor', cursor);
  return apiFetch<BookmarkPage>(`/api/v1/bookmarks?${params.toString()}`);
}

export function bookmarkCapabilities() {
  return apiFetch<Record<string, boolean>>('/api/v1/bookmarks/capabilities');
}

export function getBookmark(id: string) {
  return apiFetch<BookmarkRecord>(`/api/v1/bookmarks/${encodeURIComponent(id)}`);
}

export function createBookmark(url: string, title: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<BookmarkRecord>('/api/v1/bookmarks', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ url, title, description })
  });
}

export function updateBookmark(id: string, etag: string, url: string, title: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<BookmarkRecord>(`/api/v1/bookmarks/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ url, title, description })
  }, id);
}

export function transitionBookmark(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<BookmarkRecord>(`/api/v1/bookmarks/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  }, id);
}

export function listSnippets(includeArchived = false, query = '', limit = 25, cursor = '') {
  const params = new URLSearchParams({ includeArchived: String(includeArchived), limit: String(limit) });
  if (query.trim()) params.set('query', query.trim());
  if (cursor) params.set('cursor', cursor);
  return apiFetch<SnippetPage>(`/api/v1/snippets?${params.toString()}`);
}

export function snippetCapabilities() {
  return apiFetch<Record<string, boolean>>('/api/v1/snippets/capabilities');
}

export function getSnippet(id: string) {
  return apiFetch<SnippetRecord>(`/api/v1/snippets/${encodeURIComponent(id)}`);
}

export function createSnippet(title: string, language: string, body: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<SnippetRecord>('/api/v1/snippets', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ title, language, body, description })
  });
}

export function saveSnippet(id: string, etag: string, title: string, language: string, body: string, description: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<SnippetRecord>(`/api/v1/snippets/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, language, body, description })
  }, id);
}

export function transitionSnippet(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<SnippetRecord>(`/api/v1/snippets/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  }, id);
}

export function readingCapabilities() {
  return apiFetch<Record<string, boolean>>('/api/v1/read-later/capabilities');
}

export function listReadingQueue(state = '', limit = 25, cursor = '') {
  const params = new URLSearchParams({ limit: String(limit) });
  if (state.trim()) params.set('state', state.trim());
  if (cursor) params.set('cursor', cursor);
  return apiFetch<ReadingPage>(`/api/v1/read-later?${params.toString()}`);
}

async function readingResourceMutation(path: string, init: RequestInit, expectedId?: string) {
  const result = await apiFetch<ReadingItemRecord | ProductivityCommittedAcknowledgement | undefined>(path, init,
    expectedId ? [200, 204] : [200, 201, 204]);
  if (result === undefined) return result; // Only an accepted204 can return no body.
  if (!result || typeof result !== 'object' || typeof result.id !== 'string'
      || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id)
      || result.id === '00000000-0000-0000-0000-000000000000'
      || (expectedId && result.id.toLowerCase() !== expectedId.toLowerCase())
      || (('committed' in result || 'detailAvailable' in result) && !isProductivityAcknowledgement(result)))
    throw new NexoraApiError('Chưa xác nhận được kết quả đã lưu. Giữ nguyên lệnh và khóa để đối chiếu.', 0, 'UnexpectedAcknowledgement');
  return result;
}

export function saveReadingItem(sourceType: string, sourceId: string, idempotencyKey = createIdempotencyKey()) {
  return readingResourceMutation('/api/v1/read-later', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ sourceType, sourceId })
  });
}

export function removeReadingItem(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/read-later/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  }, 204);
}

export function updateReadingItem(id: string, etag: string, state: string, progress?: number, idempotencyKey = createIdempotencyKey(), sourceETag?: string | null) {
  return readingResourceMutation(`/api/v1/read-later/${encodeURIComponent(id)}`, {
    method: 'PATCH',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag, ...(sourceETag ? { 'If-Source-Match': sourceETag } : {}) }),
    body: JSON.stringify(progress === undefined ? { state } : { state, progress })
  }, id);
}

export function listOrganizationTags(tagNamespace = '', query = '', limit = 25, cursor?: string, signal?: AbortSignal) {
  const params = new URLSearchParams({ limit: String(limit) });
  if (tagNamespace.trim()) params.set('namespace', tagNamespace.trim());
  if (query.trim()) params.set('query', query.trim());
  if (cursor) params.set('cursor', cursor);
  return apiFetch<TagPage>(`/api/v1/${tagNamespace === 'documents' ? 'documents' : 'organization'}/tags?${params.toString()}`, { signal });
}

export function getOrganizationTag(id: string, tagNamespace = '') {
  return apiFetch<TagRecord>(`/api/v1/${tagNamespace === 'documents' ? 'documents' : 'organization'}/tags/${encodeURIComponent(id)}`);
}

function validateTagMutation(result: TagRecord | ProductivityCommittedAcknowledgement, expectedId?: string) {
  if (!result || typeof result.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id) || result.id === '00000000-0000-0000-0000-000000000000' || (expectedId !== undefined && result.id.toLowerCase() !== expectedId.toLowerCase()))
    throw new NexoraApiError('Không thể xác nhận kết quả Tag.', 0, 'UnexpectedAcknowledgement');
  if (('committed' in result || 'detailAvailable' in result) && !isProductivityAcknowledgement(result))
    throw new NexoraApiError('Không thể xác nhận kết quả Tag.', 0, 'UnexpectedAcknowledgement');
  if (isProductivityAcknowledgement(result)) return result;
  if (typeof result.name !== 'string' || typeof result.namespace !== 'string' || typeof result.etag !== 'string' || !Number.isInteger(result.usageCount) || result.usageCount < 0)
    throw new NexoraApiError('Không thể xác nhận chi tiết Tag.', 0, 'UnexpectedAcknowledgement');
  return result;
}

export function createOrganizationTag(tagNamespace: string, name: string, color: string | null, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<TagRecord | ProductivityCommittedAcknowledgement>(`/api/v1/${tagNamespace === 'documents' ? 'documents' : 'organization'}/tags`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey),
    body: JSON.stringify({ namespace: tagNamespace, name, color })
  }, [201]).then(result => validateTagMutation(result));
}

export function renameOrganizationTag(id: string, etag: string, name: string, color: string | null, idempotencyKey = createIdempotencyKey(), tagNamespace = '') {
  return apiFetch<TagRecord | ProductivityCommittedAcknowledgement>(`/api/v1/${tagNamespace === 'documents' ? 'documents' : 'organization'}/tags/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ name, color })
  }, [200]).then(result => validateTagMutation(result, id));
}

export function removeOrganizationTag(id: string, etag: string, idempotencyKey = createIdempotencyKey(), tagNamespace = '') {
  return apiFetch<void>(`/api/v1/${tagNamespace === 'documents' ? 'documents' : 'organization'}/tags/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  }, [204]);
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

export async function setReminder(
  sourceType: 'Task' | 'CalendarEvent',
  sourceId: string,
  configType: 'None' | 'BeforeStart15m' | 'Exact',
  exactAt: string | null,
  sourceETag: string,
  reminderETag: string | null,
  idempotencyKey = createIdempotencyKey()
) {
  const saved = await apiFetch<ReminderSourceView | ProductivityCommittedAcknowledgement>(`/api/v1/reminders/${encodeURIComponent(sourceType)}/${encodeURIComponent(sourceId)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, reminderETag ? { 'If-Match': reminderETag } : undefined),
    body: JSON.stringify({ configType, exactAt, sourceETag })
  }, reminderETag ? 200 : 201);
  if (isProductivityAcknowledgement(saved)) {
    if (typeof saved.id !== 'string' || saved.id.toLowerCase() !== sourceId.toLowerCase())
      throw new NexoraApiError('Chưa xác nhận được kết quả reminder. Giữ idempotency key để đối chiếu.', 0, 'UnexpectedAcknowledgement');
  } else if (!saved || saved.sourceId !== sourceId || saved.sourceType !== sourceType) {
    throw new NexoraApiError('Chưa xác nhận được nguồn reminder đã lưu.', 0, 'UnexpectedAcknowledgement');
  }
  return saved;
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

export function listPlanner(from?: string, to = from) {
  const params = new URLSearchParams();
  if (from) params.set('from', from);
  if (to) params.set('to', to);
  const query = params.size ? `?${params.toString()}` : '';
  return apiFetch<PlannerPlan>(`/api/v1/planner${query}`);
}

export function pinPlannerTask(taskId: string, planDate: string, notes: string | null, idempotencyKey = createIdempotencyKey()) {
  return productivityMutation<PlannerPinRecord>('/api/v1/planner/pins', {
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
  return productivityMutation<PlannerPinRecord>(`/api/v1/planner/pins/${encodeURIComponent(pinId)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ planDate, notes })
  }, pinId);
}

export async function reorderPlanner(planDate: string, pinIds: string[], etag: string, idempotencyKey = createIdempotencyKey()) {
  const result = await apiFetch<PlannerPlan | ProductivityCommittedAcknowledgement>('/api/v1/planner/reorder', {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ planDate, pinIds })
  }, 200);
  if (!result || typeof result !== 'object'
      || (('committed' in result || 'detailAvailable' in result)
        ? !isProductivityAcknowledgement(result) || result.id?.toLowerCase() !== pinIds[0]?.toLowerCase()
          || Object.keys(result).some(key => !['id', 'committed', 'detailAvailable'].includes(key))
        : !('pins' in result) || !Array.isArray(result.pins) || typeof result.etag !== 'string'
          || result.from !== planDate || result.to !== planDate))
    throw new NexoraApiError('Chưa xác nhận được kết quả sắp xếp. Thử lại thao tác cũ để đối chiếu.', 0, 'UnexpectedAcknowledgement');
  return result;
}

export function unpinPlannerTask(pinId: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/planner/pins/${encodeURIComponent(pinId)}`, {
    method: 'DELETE',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  }, 204);
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

async function habitMutation(path: string, init: RequestInit, expectedId?: string): Promise<HabitDetail | ProductivityCommittedAcknowledgement> {
  const result = await apiFetch<HabitDetail | ProductivityCommittedAcknowledgement>(path, init, expectedId ? 200 : 201);
  const validId = (id: unknown): id is string => typeof id === 'string'
    && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)
    && id !== '00000000-0000-0000-0000-000000000000';
  const invalid = () => { throw new NexoraApiError('Chưa xác nhận được kết quả Habit. Thử lại đúng thao tác cũ để đối chiếu.', 0, 'UnexpectedAcknowledgement'); };
  if (!result || typeof result !== 'object') return invalid();
  if ('committed' in result || 'detailAvailable' in result) {
    if (!isProductivityAcknowledgement(result) || !validId(result.id)
        || (expectedId && result.id.toLowerCase() !== expectedId.toLowerCase())
        || Object.keys(result).some(key => !['id', 'committed', 'detailAvailable'].includes(key))) return invalid();
    return result;
  }
  if (!('habit' in result) || !result.habit || !validId(result.habit.id)
      || (expectedId && result.habit.id.toLowerCase() !== expectedId.toLowerCase())
      || typeof result.habit.title !== 'string' || typeof result.habit.etag !== 'string'
      || !['Active', 'Paused', 'Archived'].includes(result.habit.state)
      || !Number.isInteger(result.habit.currentStreak) || !Array.isArray(result.schedules) || !Array.isArray(result.checkIns)) return invalid();
  return result;
}

export function createHabit(
  value: {
    title: string; kind: 'Boolean' | 'Count'; targetCount: number | null; unit: string | null;
    effectiveFrom: string; weekdayMask: number; timeZoneId: string; reminderLocalTime: string | null;
  },
  idempotencyKey = createIdempotencyKey()
) {
  return habitMutation('/api/v1/habits', {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey), body: JSON.stringify(value)
  });
}

export function updateHabit(
  id: string,
  etag: string,
  value: { title: string; unit: string | null; reminderLocalTime: string | null; timeZoneId: string },
  idempotencyKey = createIdempotencyKey()
) {
  return habitMutation(`/api/v1/habits/${encodeURIComponent(id)}`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  }, id);
}

export function setHabitSchedule(
  id: string,
  etag: string,
  value: { effectiveFrom: string; weekdayMask: number; targetCount: number | null },
  idempotencyKey = createIdempotencyKey()
) {
  return habitMutation(`/api/v1/habits/${encodeURIComponent(id)}/schedule`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  }, id);
}

export function recordHabitCheckIn(
  id: string,
  etag: string,
  value: { localDate: string; count: number; note: string | null },
  idempotencyKey = createIdempotencyKey()
) {
  return habitMutation(`/api/v1/habits/${encodeURIComponent(id)}/check-ins`, {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify(value)
  }, id);
}

export function transitionHabit(id: string, etag: string, state: 'Active' | 'Paused' | 'Archived' | 'Unarchive', idempotencyKey = createIdempotencyKey()) {
  return habitMutation(`/api/v1/habits/${encodeURIComponent(id)}/transition`, {
    method: 'POST', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ state })
  }, id);
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

async function goalMutation(path: string, init: RequestInit, expectedId?: string, expectedStatus?: 200 | 201): Promise<GoalDetail | ProductivityCommittedAcknowledgement> {
  const result = await apiFetch<GoalDetail | ProductivityCommittedAcknowledgement>(path, init, expectedStatus ?? (expectedId ? 200 : 201));
  const uuid = (value: unknown): value is string => typeof value === 'string'
    && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
    && value !== '00000000-0000-0000-0000-000000000000';
  const etag = (value: unknown): value is string => typeof value === 'string' && /^"[A-Za-z0-9+/]{11}="$/.test(value);
  const fail = () => { throw new NexoraApiError('Chưa xác nhận được kết quả đã lưu. Thử lại đúng thao tác cũ để đối chiếu.', 0, 'UnexpectedAcknowledgement'); };
  if (!result || typeof result !== 'object') return fail();
  if ('committed' in result || 'detailAvailable' in result) {
    if (!isProductivityAcknowledgement(result) || !uuid(result.id)
        || (expectedId && result.id.toLowerCase() !== expectedId.toLowerCase())
        || Object.keys(result).some(key => !['id', 'committed', 'detailAvailable'].includes(key))) return fail();
    return result;
  }
  if (!('goal' in result) || !result.goal || !uuid(result.goal.id)
      || (expectedId && result.goal.id.toLowerCase() !== expectedId.toLowerCase())
      || typeof result.goal.title !== 'string' || !etag(result.goal.etag)
      || !['Draft', 'Active', 'Completed', 'Abandoned', 'Archived'].includes(result.goal.status)
      || (result.goal.preArchiveStatus != null && !['Draft', 'Active', 'Completed', 'Abandoned'].includes(result.goal.preArchiveStatus))
      || !Number.isFinite(result.goal.progress) || !Number.isInteger(result.goal.targetCount) || result.goal.targetCount < 0
      || !Array.isArray(result.targets) || result.targets.some(target => !target || !uuid(target.id)
        || typeof target.title !== 'string' || !['Numeric', 'Boolean', 'Tasks'].includes(target.kind)
        || !etag(target.etag) || !Number.isFinite(target.progress)
        || (target.kind === 'Boolean' && typeof target.booleanValue !== 'boolean'))) return fail();
  return result;
}

export function createGoal(
  title: string,
  description: string | null,
  startDate: string | null,
  endDate: string | null,
  numericTarget: { title: string; initialValue: string | number; currentValue: string | number; targetValue: string | number } | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return goalMutation('/api/v1/goals', {
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
  return goalMutation(`/api/v1/goals/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ title, description, startDate: startDate || null, endDate: endDate || null, numericTarget: null })
  }, id);
}

export function recordGoalProgress(
  goalId: string,
  targetId: string,
  goalEtag: string,
  currentValue: string | number,
  note: string | null = null,
  idempotencyKey = createIdempotencyKey()
) {
  return goalMutation(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/${encodeURIComponent(targetId)}/progress`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': goalEtag }),
    body: JSON.stringify({ currentValue, note })
  }, goalId);
}

export function createBooleanGoalTarget(goalId: string, etag: string, title: string, booleanValue: boolean, key: string) {
  return goalMutation(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/boolean`, {
    method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': etag }),
    body: JSON.stringify({ title, booleanValue })
  }, goalId, 201);
}

export function createNumericGoalTarget(goalId: string, etag: string, title: string, initialValue: string, currentValue: string, targetValue: string, key: string) {
  return goalMutation(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/numeric`, {
    method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': etag }),
    body: JSON.stringify({ title, initialValue, currentValue, targetValue })
  }, goalId, 201);
}

export type GoalTargetChangePreview = {
  previewToken: string; expiresAt: string; goalETag: string; targetETag: string; targetTitle: string; kind: string;
  beforeCount: number; afterCount: number; beforeProgress: number; afterProgress: number;
  proposedTitle: string | null; initialValueExact: string | null; targetValueExact: string | null;
};

export async function previewGoalTargetChange(goalId: string, targetId: string, etag: string,
  change: { mode: 'Update' | 'Remove'; title?: string; initialValue?: string; targetValue?: string }, signal?: AbortSignal) {
  const result = await apiFetch<GoalTargetChangePreview>(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/${encodeURIComponent(targetId)}/change-preview`, {
    method: 'POST', headers: { 'If-Match': etag }, body: JSON.stringify(change), signal
  }, 200);
  if (!result || typeof result.previewToken !== 'string' || !result.previewToken.startsWith('gt1.') || result.previewToken.length > 4096
      || result.goalETag !== etag || typeof result.targetETag !== 'string' || typeof result.targetTitle !== 'string'
      || !['Numeric', 'Boolean'].includes(result.kind) || !Number.isFinite(Date.parse(result.expiresAt))
      || ![result.beforeProgress, result.afterProgress].every(value => Number.isFinite(value) && value >= 0 && value <= 1)
      || ![result.beforeCount, result.afterCount].every(value => Number.isSafeInteger(value) && value >= 0)
      || (change.mode === 'Remove' ? result.afterCount !== result.beforeCount - 1 || result.proposedTitle !== null
        : result.afterCount !== result.beforeCount || typeof result.proposedTitle !== 'string')
      || (result.initialValueExact !== null && typeof result.initialValueExact !== 'string')
      || (result.targetValueExact !== null && typeof result.targetValueExact !== 'string'))
    throw new NexoraApiError('Không thể xác nhận bản xem trước. Hãy tải revision mới.', 0, 'InvalidPreviewResponse');
  return result;
}

export function changeGoalTarget(goalId: string, targetId: string, etag: string, mode: 'Update' | 'Remove', previewToken: string, key: string) {
  const action = mode === 'Update' ? 'update-definition' : 'remove';
  return goalMutation(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/${encodeURIComponent(targetId)}/${action}`, {
    method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': etag }), body: JSON.stringify({ previewToken })
  }, goalId);
}

export function recordBooleanGoalProgress(goalId: string, targetId: string, etag: string, booleanValue: boolean, note: string | null, key: string) {
  return goalMutation(`/api/v1/goals/${encodeURIComponent(goalId)}/targets/${encodeURIComponent(targetId)}/boolean-progress`, {
    method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': etag }),
    body: JSON.stringify({ booleanValue, note })
  }, goalId);
}

export function transitionGoal(id: string, etag: string, status: string, idempotencyKey = createIdempotencyKey()) {
  return goalMutation(`/api/v1/goals/${encodeURIComponent(id)}/transition`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ status })
  }, id);
}

export function listAdminUsers(query = '') {
  const suffix = query.trim() ? `?q=${encodeURIComponent(query.trim())}` : '';
  return apiFetch<AdminUserPage>(`/api/v1/admin/users${suffix}`);
}

export function getAdminUserAccess(userId: string) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access`);
}

export function previewAdminAccess(userId: string, change: AdminAccessChange) {
  return apiFetch<AdminAccessPreview>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access/preview`, {
    method: 'POST', headers: jsonMutationHeaders(), body: JSON.stringify(change)
  });
}

export function commitAdminUserRole(userId: string, etag: string, role: string, previewToken: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess | undefined>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access/role`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ kind: 'role', role, previewToken })
  });
}

export function commitAdminActionGrant(userId: string, etag: string, changes: { actionKey: string; effect: 'Allow' | 'Deny' | 'Unset' }[], previewToken: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access/permissions`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ kind: 'permissions', changes, previewToken })
  });
}

export function commitAdminModuleGrant(userId: string, etag: string, changes: { moduleId: string; enabled: boolean }[], previewToken: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminUserAccess>(`/api/v1/admin/users/${encodeURIComponent(userId)}/access/modules`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ kind: 'modules', changes, previewToken })
  });
}

export function reauthenticate(password: string) {
  return apiFetch<void>('/api/v1/auth/reauth', {
    method: 'POST', headers: jsonMutationHeaders(createIdempotencyKey()), body: JSON.stringify({ password })
  });
}

export function listAdminModules() {
  return apiFetch<AdminModulePage>('/api/v1/admin/modules/?limit=100');
}

export function previewModulePolicy(moduleId: string, change: { systemEnabled?: boolean; registrationEnabled?: boolean; sharingEnabled?: boolean }) {
  return apiFetch<ModulePolicyPreview>(`/api/v1/admin/modules/${encodeURIComponent(moduleId)}/preview`, {
    method: 'POST', headers: jsonMutationHeaders(), body: JSON.stringify(change)
  });
}

export function commitModulePolicy(moduleId: string, etag: string, change: { systemEnabled?: boolean; registrationEnabled?: boolean; sharingEnabled?: boolean }, previewToken: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<AdminModuleRecord>(`/api/v1/admin/modules/${encodeURIComponent(moduleId)}/policy`, {
    method: 'PUT', headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }), body: JSON.stringify({ ...change, previewToken })
  });
}

export function listShareLinks(limit = 25, filters: { mode?: string; state?: string; resourceType?: string; resourceId?: string; cursor?: string } = {}) {
  const query = new URLSearchParams({ limit: String(limit) });
  for (const [key, value] of Object.entries(filters)) if (value) query.set(key, value);
  return apiFetch<ShareLinkPage>('/api/v1/sharing/links?' + query);
}

export function getShareLink(id: string) {
  return apiFetch<ShareLinkRecord>(`/api/v1/sharing/links/${encodeURIComponent(id)}`);
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

export function previewShareDisclosure(resourceType: string, resourceId: string) {
  return apiFetch<SharedResource>(`/api/v1/sharing/preview?${new URLSearchParams({ resourceType, resourceId })}`);
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

export function getFileCapabilities() { return apiFetch<Record<string, boolean>>('/api/v1/files/capabilities'); }

export function listFiles(limit = 25, filters: { cursor?: string; query?: string; mediaType?: string; scanState?: string; lifecycle?: string } = {}) {
  const params = new URLSearchParams({ limit: String(limit) });
  for (const [key, value] of Object.entries(filters)) if (value) params.set(key, value);
  return apiFetch<FilePage>('/api/v1/files?' + params);
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
  return productivityMutation<FileRecord>(`/api/v1/files/${encodeURIComponent(id)}/restore`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag }),
    body: JSON.stringify({ deletionBatchId })
  }, id);
}

export function purgeFile(id: string, etag: string, idempotencyKey = createIdempotencyKey()) {
  return apiFetch<void>(`/api/v1/files/${encodeURIComponent(id)}/purge`, {
    method: 'POST',
    headers: jsonMutationHeaders(idempotencyKey, { 'If-Match': etag })
  });
}
