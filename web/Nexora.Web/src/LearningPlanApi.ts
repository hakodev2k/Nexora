import { apiFetch } from './api';
export type LearningPlanKind = 'Goal' | 'Task' | 'Course';
export type LearningPlanRef = { kind: LearningPlanKind; id: string };
export type LearningPlanMetadata = { title: string; startsOn: string | null; endsOn: string | null; notes: string | null };
export type LearningPlan = LearningPlanMetadata & { id: string; status: string; etag: string; updatedAt: string };
export type LearningPlanItem = { id: string; source: LearningPlanRef; position: number; targetOn: string | null;
  projection: { status: string; title: string | null; sourceState: string | null; percent: number | null } };
export type LearningPlanDetail = { plan: LearningPlan; items: LearningPlanItem[]; known: number; unavailable: number; progressLabel: string };
export type LearningPlanPage = { items: LearningPlan[]; nextCursor: string | null };
export type LearningPlanChoice = { source: LearningPlanRef; title: string };
export type LearningPlanSources = { items: LearningPlanChoice[]; nextCursor: string | null };
export type LearningPlanAck = { id: string; committed: true; detailAvailable: false };
export type LearningPlanLifecycleOperation = 'archive' | 'unarchive';
export type LearningPlanLifecyclePreview = { planId: string; operation: LearningPlanLifecycleOperation; etag: string;
  fromState: string; toState: string; retainedItems: number; expiresAt: string; previewToken: string };
const root = '/api/v1/learning/plans';
export const learningPlanCapabilities = (signal?: AbortSignal) => apiFetch<Record<string, boolean>>(`${root}/capabilities`, { signal });
export const learningPlans = (status: string, cursor: string | null = null, signal?: AbortSignal) =>
  apiFetch<LearningPlanPage>(`${root}?${new URLSearchParams({ status, ...(cursor ? { cursor } : {}) })}`, { signal });
export const learningPlan = (id: string, signal?: AbortSignal) => apiFetch<LearningPlanDetail>(`${root}/${encodeURIComponent(id)}`, { signal });
export const learningPlanLifecyclePreview = (id: string, operation: LearningPlanLifecycleOperation, etag: string, signal?: AbortSignal) =>
  apiFetch<LearningPlanLifecyclePreview>(`${root}/${encodeURIComponent(id)}/lifecycle-preview?${new URLSearchParams({ operation })}`,
    { signal, headers: { 'If-Match': etag } });
export const learningPlanSources = (kind: LearningPlanKind, cursor: string | null = null, signal?: AbortSignal) =>
  apiFetch<LearningPlanSources>(`${root}/sources?${new URLSearchParams({ kind, ...(cursor ? { cursor } : {}) })}`, { signal });
export type LearningPlanIntent = Readonly<{ path: string; method: 'POST' | 'PUT' | 'DELETE'; key: string; etag: string | null; body: string | undefined }>;
// Freeze the complete HTTP intent, including key and original revision, before send.
export function learningPlanIntent(path: string, method: LearningPlanIntent['method'], key: string, etag: string | null, body?: unknown): LearningPlanIntent {
  return Object.freeze({ path: root + path, method, key, etag, body: body === undefined ? undefined : JSON.stringify(body) });
}
export async function sendLearningPlan(intent: LearningPlanIntent): Promise<LearningPlanAck> {
  const result = await apiFetch<LearningPlanAck>(intent.path, { method: intent.method,
    headers: { 'Idempotency-Key': intent.key, ...(intent.etag ? { 'If-Match': intent.etag } : {}) }, body: intent.body }, [200, 201]);
  if (result?.committed !== true || result.detailAvailable !== false || typeof result.id !== 'string')
    throw new Error('Unconfirmed Plan acknowledgement');
  return result;
}
