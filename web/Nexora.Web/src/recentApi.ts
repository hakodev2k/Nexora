import { apiFetch, jsonMutationHeaders, NexoraApiError } from './api';
export const recentTypes = ['Project', 'Task', 'Event', 'Document', 'Bookmark', 'Snippet', 'Goal'] as const;
export type RecentType = typeof recentTypes[number];
export type RecentItem = { id: string; resourceType: RecentType; resourceId: string; title: string; status: string; lastOpenedAt: string };
export type RecentPage = { items: RecentItem[]; nextCursor: string | null; clearToken: string | null };
export type RecentCapabilities = { canRead: boolean; canClear: boolean };
const validToken = (value: unknown): value is string | null => value === null || (typeof value === 'string' && value.length > 0 && value.length <= 4096);
export async function recentCapabilities(signal?: AbortSignal) {
  const value = await apiFetch<RecentCapabilities>('/api/v1/recents/capabilities', { signal });
  if (!value || typeof value.canRead !== 'boolean' || typeof value.canClear !== 'boolean') throw new NexoraApiError('Quyền Recents không hợp lệ.', 0, 'UnexpectedResponse');
  return value;
}
export async function recentList(type: string, cursor?: string, signal?: AbortSignal) {
  const query = new URLSearchParams(); if (type) query.set('resourceType', type); if (cursor) query.set('cursor', cursor);
  const value = await apiFetch<RecentPage>('/api/v1/recents?' + query, { signal });
  const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
  if (!value || !Array.isArray(value.items) || value.items.length > 25 || !validToken(value.nextCursor) || !validToken(value.clearToken)
      || value.items.some(item => !item || !uuid.test(item.id) || !uuid.test(item.resourceId) || !recentTypes.includes(item.resourceType)
        || typeof item.title !== 'string' || typeof item.status !== 'string' || typeof item.lastOpenedAt !== 'string' || !Number.isFinite(Date.parse(item.lastOpenedAt))))
    throw new NexoraApiError('Dữ liệu Recents không hợp lệ.', 0, 'UnexpectedResponse');
  return value;
}
export async function recentClearPreview() {
  const value = await apiFetch<{ token: string }>('/api/v1/recents/clear-preview');
  if (!value || !value.token || !validToken(value.token)) throw new NexoraApiError('Không thể xác nhận phạm vi xóa.', 0, 'UnexpectedResponse');
  return value.token;
}
export const recentClear = (token: string, key: string) => apiFetch<void>('/api/v1/recents/clear', {
  method: 'POST', headers: jsonMutationHeaders(key), body: JSON.stringify({ token })
}, 204);
