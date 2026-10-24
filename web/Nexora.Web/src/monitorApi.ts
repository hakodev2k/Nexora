import { apiFetch } from './api';

export type MonitorMetadata = { schemaVersion: 1; title: string; kind: 'Http'; target: string; intervalSeconds: number; expectedStatus: number | null };
export type MonitorRecord = { id: string; metadata: MonitorMetadata; enabled: boolean; state: 'Unknown' | 'Paused'; lastObservedAt: null; createdAt: string; updatedAt: string; etag: string };
export type MonitorAcknowledgement = Pick<MonitorRecord, 'id' | 'enabled' | 'state' | 'etag'>;
export type MonitorPage = { items: MonitorRecord[]; nextCursor: string | null };
const root = '/api/v1/monitoring/monitors';
export const monitorCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export function monitorList(query: string, state: string, cursor?: string) {
  const params = new URLSearchParams(); if (query) params.set('query', query); if (state) params.set('state', state); if (cursor) params.set('cursor', cursor);
  return apiFetch<MonitorPage>(root + '/?' + params);
}
export const monitorGet = (id: string) => apiFetch<MonitorRecord>(root + '/' + encodeURIComponent(id));
export function monitorCreate(metadata: MonitorMetadata, enabled: boolean, key: string) {
  return apiFetch<MonitorAcknowledgement>(root + '/', { method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify({ metadata, enabled }) });
}
export function monitorUpdate(item: MonitorRecord, metadata: MonitorMetadata, key: string) {
  return apiFetch<MonitorAcknowledgement>(root + '/' + encodeURIComponent(item.id), { method: 'PUT', headers: { 'Idempotency-Key': key, 'If-Match': item.etag }, body: JSON.stringify({ metadata }) });
}
export function monitorEnabled(item: MonitorRecord, enabled: boolean, key: string) {
  return apiFetch<MonitorAcknowledgement>(root + '/' + encodeURIComponent(item.id) + (enabled ? '/resume' : '/pause'), { method: 'POST', headers: { 'Idempotency-Key': key, 'If-Match': item.etag }, body: JSON.stringify({ confirmed: true }) });
}
