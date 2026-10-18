import { apiFetch, jsonMutationHeaders, isProductivityAcknowledgement, NexoraApiError } from './api';

export type DocumentKind = 'Page' | 'Folder';
export type TreeOperation = 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge';
export type TreeTarget = { id: string; kind: DocumentKind; operation: TreeOperation };
export type TreePreview = TreeTarget & { etag: string; previewToken: string; detailsHidden: boolean; affectedCount: number | null; members: { id: string; kind: DocumentKind; title: string | null; documentType: string | null; state: string; depth: number }[] | null };
export type Folder = { id: string; title: string; parentFolderId: string | null; depth: number; etag: string };
export type TrashEntry = { id: string; kind: DocumentKind; title: string | null; documentType: string | null; deletedAt: string; etag: string };
export const documentCapabilities = () => apiFetch<Record<string, boolean>>('/api/v1/documents/capabilities');
export const getDocumentFolder = (id: string) => apiFetch<Folder>(`/api/v1/documents/folders/${encodeURIComponent(id)}`);
export const documentFolders = (parentFolderId = '', cursor = '', signal?: AbortSignal) => apiFetch<{ items: Folder[]; nextCursor: string | null }>(`/api/v1/documents/folders?${new URLSearchParams({ ...(parentFolderId ? { parentFolderId } : {}), ...(cursor ? { cursor } : {}) })}`, { signal });
export const documentTrash = (cursor = '', signal?: AbortSignal) => apiFetch<{ items: TrashEntry[]; nextCursor: string | null }>(`/api/v1/documents/trash?${new URLSearchParams({ cursor })}`, { signal });
const path = (target: TreeTarget) => `/api/v1/documents/${target.kind === 'Page' ? 'pages' : 'folders'}/${encodeURIComponent(target.id)}/`;
export async function previewDocumentTree(target: TreeTarget, signal?: AbortSignal) {
  const value = await apiFetch<TreePreview>(path(target) + 'preview-' + target.operation, { method: 'POST', headers: jsonMutationHeaders(), signal });
  if (!value || value.id !== target.id || value.kind !== target.kind || value.operation !== target.operation || typeof value.etag !== 'string' || typeof value.previewToken !== 'string' || typeof value.detailsHidden !== 'boolean' || (!value.detailsHidden && (!Array.isArray(value.members) || !Number.isSafeInteger(value.affectedCount))))
    throw new NexoraApiError('Chưa xác nhận được phạm vi thao tác.', 0, 'UnexpectedResponse');
  return value;
}
export async function executeDocumentTree(preview: TreePreview, confirmation: string, key: string) {
  const value = await apiFetch<unknown>(path(preview) + preview.operation, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': preview.etag }), body: JSON.stringify({ confirm: true, previewToken: preview.previewToken, confirmation: preview.operation === 'purge' ? confirmation : null }) }, preview.operation === 'purge' ? 204 : 200);
  if (preview.operation === 'purge' && value === undefined) return;
  if (!isProductivityAcknowledgement(value) || (value as { id: string }).id !== preview.id) throw new NexoraApiError('Chưa xác nhận được kết quả. Giữ nguyên thao tác để đối chiếu.', 0, 'UnexpectedAcknowledgement');
}
export async function writeDocumentFolder(folder: Folder | null, title: string, parentFolderId: string | null, key: string) {
  const value = await apiFetch<Folder | unknown>('/api/v1/documents/folders' + (folder ? '/' + encodeURIComponent(folder.id) : ''), { method: folder ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, folder ? { 'If-Match': folder.etag } : {}), body: JSON.stringify(folder ? { title } : { title, parentFolderId }) }, folder ? 200 : 201);
  if (isProductivityAcknowledgement(value) && typeof value.id === 'string' && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.id) && (!folder || value.id === folder.id)) return;
  const result = value as Folder;
  if (!result || typeof result.id !== 'string' || typeof result.title !== 'string' || typeof result.etag !== 'string' || (folder && folder.id !== result.id)) throw new NexoraApiError('Chưa xác nhận được thư mục.', 0, 'UnexpectedAcknowledgement');
}
