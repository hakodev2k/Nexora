import { apiFetch, jsonMutationHeaders, NexoraApiError } from './api';

export type CollectionSummary = { id: string; name: string; description: string | null; etag: string };
export type CollectionMember = { id: string; resourceType: string; resourceId: string; title: string; status: string };
export type CollectionDetail = { collection: CollectionSummary; members: CollectionMember[] };
export type CollectionCandidate = Omit<CollectionMember, 'id'>;
export type CollectionPage = { items: CollectionSummary[]; hasMore: boolean; page: number };
export type CollectionAck = { id: string; committed: true; detailAvailable: false };
export const collectionTypes = ['Project', 'Document', 'Bookmark', 'Snippet'] as const;
const base = '/api/v1/organization/collections';
export const collectionCapabilities = (signal?: AbortSignal) => apiFetch<Record<string, boolean>>(`${base}/capabilities`, { signal });
export const listCollections = (query: string, page: number, signal?: AbortSignal) =>
  apiFetch<CollectionPage>(`${base}?${new URLSearchParams({ query, page: String(page) })}`, { signal });
export const getCollection = (id: string, signal?: AbortSignal) => apiFetch<CollectionDetail>(`${base}/${encodeURIComponent(id)}`, { signal });
export const collectionCandidates = (resourceType: string, query: string, signal?: AbortSignal) =>
  apiFetch<CollectionCandidate[]>(`${base}/candidates?${new URLSearchParams({ resourceType, query })}`, { signal });
export type CollectionIntent = Readonly<{ path: string; method: 'POST' | 'PUT' | 'DELETE'; body?: string; etag?: string; key: string; expectedId?: string }>;
export async function sendCollectionIntent(intent: CollectionIntent): Promise<CollectionAck> {
  const result = await apiFetch<CollectionAck>(`${base}${intent.path}`, {
    method: intent.method, headers: jsonMutationHeaders(intent.key, intent.etag ? { 'If-Match': intent.etag } : {}),
    body: intent.body
  }, intent.method === 'POST' && intent.path === '' ? 201 : 200);
  if (!result || !/^[0-9a-f-]{36}$/i.test(result.id) || result.committed !== true || result.detailAvailable !== false ||
      (intent.expectedId && result.id.toLowerCase() !== intent.expectedId.toLowerCase()))
    throw new NexoraApiError('Chưa xác nhận được thao tác. Thử lại cùng yêu cầu.', 0, 'UnexpectedAcknowledgement');
  return result;
}
