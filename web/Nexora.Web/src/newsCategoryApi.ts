import { apiFetch } from './api';

export type NewsCategoryMetadata = { schemaVersion: 1; name: string };
export type NewsCategoryRecord = { id: string; metadata: NewsCategoryMetadata; createdAt: string; updatedAt: string; etag: string };
export type NewsCategoryAcknowledgement = Pick<NewsCategoryRecord, 'id' | 'etag'>;
export type NewsCategoryPage = { items: NewsCategoryRecord[]; nextCursor: string | null };
const root = '/api/v1/news/categories';
export const newsCategoryCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export function newsCategoryList(query: string, cursor?: string) {
  const params = new URLSearchParams(); if (query) params.set('query', query); if (cursor) params.set('cursor', cursor);
  return apiFetch<NewsCategoryPage>(root + '/?' + params);
}
export const newsCategoryGet = (id: string) => apiFetch<NewsCategoryRecord>(root + '/' + encodeURIComponent(id));
async function acknowledgement(request: Promise<unknown>): Promise<NewsCategoryAcknowledgement> {
  try {
    const value = await request;
    if (!value || typeof value !== 'object') throw new Error('Missing acknowledgement');
    const ack = value as Partial<NewsCategoryAcknowledgement>;
    if (typeof ack.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(ack.id) ||
        ack.id === '00000000-0000-0000-0000-000000000000' || typeof ack.etag !== 'string' ||
        !/^"[A-Za-z0-9+/]{11}="$/.test(ack.etag)) throw new Error('Invalid acknowledgement');
    const decoded = atob(ack.etag.slice(1, -1));
    if (decoded.length !== 8 || '"' + btoa(decoded) + '"' !== ack.etag) throw new Error('Invalid acknowledgement');
    return { id: ack.id, etag: ack.etag };
  } catch (error) {
    if (typeof (error as { status?: unknown })?.status === 'number') throw error;
    throw Object.assign(new Error('The save acknowledgement is unavailable. Retry the same save to recover its outcome.'), { status: 0, code: 'SaveOutcomeUnknown' });
  }
}
export function newsCategoryCreate(metadata: NewsCategoryMetadata, key: string) {
  return acknowledgement(apiFetch(root + '/', { method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify({ metadata }) }));
}
export function newsCategoryUpdate(item: NewsCategoryRecord, metadata: NewsCategoryMetadata, key: string) {
  return acknowledgement(apiFetch(root + '/' + encodeURIComponent(item.id), { method: 'PUT', headers: { 'Idempotency-Key': key, 'If-Match': item.etag }, body: JSON.stringify({ metadata }) }));
}
