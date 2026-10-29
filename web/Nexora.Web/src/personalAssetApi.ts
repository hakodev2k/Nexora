import { apiFetch, jsonMutationHeaders } from './api';
const root = '/api/v1/assets/personal';
export const operationalStates = ['Active', 'Stored', 'Repair', 'Sold', 'Disposed', 'Lost'] as const;
export const assetKinds = ['Device', 'Electronics', 'VehicleMetadata', 'Other'] as const;
export type PersonalAsset = { id: string; title: string; kind: string; brand: string | null; model: string | null; category: string | null; notes: string | null; state: string; createdAt: string; updatedAt: string; etag: string };
export type AssetDraft = { title: string; kind: string; state: string; brand: string | null; model: string | null; category: string | null; notes: string | null; reason: string };
export type AssetOperation = 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge';
export type AssetPreview = { itemId: string; etag: string; operation: AssetOperation; referenceCount: number };
export type AssetVersion = { id: string; versionNumber: number; actionKey: string; createdAt: string; createdByUserId: string | null; reason: string | null; snapshot: { schemaVersion: number; resourceType: string; fields: { title: string; kind: string; brand: string | null; model: string | null; category: string | null; notes: string | null; state: string; preArchiveState: string | null; preTrashState: string | null } } };
export const assetCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export const assetGet = (id: string) => apiFetch<PersonalAsset>(root + '/' + encodeURIComponent(id));
export const assetList = (state: string, query: string, kind: string, category: string, cursor?: string) => {
  const params = new URLSearchParams({ state, query }); if (kind) params.set('kind', kind); if (category) params.set('category', category); if (cursor) params.set('cursor', cursor);
  return apiFetch<{ items: PersonalAsset[]; nextCursor: string | null }>(root + '?' + params);
};
export const assetSave = (body: AssetDraft, key: string, item?: PersonalAsset) => {
  const { state, reason: _reason, ...metadata } = body;
  return apiFetch<{ itemId: string; etag: string }>(root + (item ? '/' + item.id : ''), { method: item ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, item ? { 'If-Match': item.etag } : {}), body: JSON.stringify(item ? metadata : { ...metadata, state }) });
};
export const assetSetState = (item: PersonalAsset, state: string, reason: string, key: string) => apiFetch<{ itemId: string; etag: string }>(root + '/' + item.id + '/transition', { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': item.etag }), body: JSON.stringify({ state, reason: reason || null }) });
export const assetPreview = (item: PersonalAsset, operation: AssetOperation) => apiFetch<AssetPreview>(root + '/' + item.id + '/preview-' + operation, { method: 'POST', headers: jsonMutationHeaders(crypto.randomUUID()) });
export const assetTransition = (preview: AssetPreview, key: string) => apiFetch<{ itemId: string; etag: string | null }>(root + '/' + preview.itemId + '/' + preview.operation, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': preview.etag }), body: JSON.stringify({ confirm: true }) });
export type AssetHistoryFilter = { version: string; action: string; from: string; to: string };
export const assetHistory = (id: string, cursor?: string, filter?: AssetHistoryFilter) => {
  const params = new URLSearchParams(); if (cursor) params.set('cursor', cursor);
  if (filter) for (const key of ['version', 'action', 'from', 'to'] as const) if (filter[key]) params.set(key, filter[key]);
  return apiFetch<{ items: AssetVersion[]; nextCursor: string | null }>(root + '/' + encodeURIComponent(id) + '/history?' + params);
};
