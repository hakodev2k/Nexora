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
export function newsCategoryCreate(metadata: NewsCategoryMetadata, key: string) {
  return apiFetch<NewsCategoryAcknowledgement>(root + '/', { method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify({ metadata }) });
}
export function newsCategoryUpdate(item: NewsCategoryRecord, metadata: NewsCategoryMetadata, key: string) {
  return apiFetch<NewsCategoryAcknowledgement>(root + '/' + encodeURIComponent(item.id), { method: 'PUT', headers: { 'Idempotency-Key': key, 'If-Match': item.etag }, body: JSON.stringify({ metadata }) });
}
