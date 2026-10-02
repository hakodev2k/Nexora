import { apiFetch, jsonMutationHeaders } from './api';

const root = '/api/v1/learning/skills';
export type Skill = { id: string; title: string; level: string; description: string | null; category: string | null; lastUsed: string | null; status: 'Active' | 'Archived' | 'Trash'; createdAt: string; updatedAt: string; etag: string };
export type SkillDraft = { title: string; level: string; description: string | null; category: string | null; lastUsed: string | null };
export type SkillOperation = 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge';
export type SkillPreview = { itemId: string; etag: string; operation: SkillOperation; referenceCount: number };
export const skillCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export const skillGet = (id: string) => apiFetch<Skill>(`${root}/${encodeURIComponent(id)}`);
export const skillList = (status: string, query: string, cursor?: string) => {
  const params = new URLSearchParams({ status, query }); if (cursor) params.set('cursor', cursor);
  return apiFetch<{ items: Skill[]; nextCursor: string | null }>(root + '?' + params);
};
export const skillSave = (body: SkillDraft, key: string, item?: Skill) => {
  const { level, ...metadata } = body;
  return apiFetch<{ itemId: string; etag: string }>(`${root}${item ? '/' + item.id : ''}`, { method: item ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, item ? { 'If-Match': item.etag } : {}), body: JSON.stringify(item ? metadata : { ...metadata, level }) });
};
export const skillSetProficiency = (item: Skill, level: string, key: string) => apiFetch<{ itemId: string; etag: string }>(`${root}/${item.id}/proficiency`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': item.etag }), body: JSON.stringify({ level }) });
export const skillPreview = (item: Skill, operation: SkillOperation) => apiFetch<SkillPreview>(`${root}/${item.id}/preview-${operation}`, { method: 'POST', headers: jsonMutationHeaders(crypto.randomUUID()) });
export const skillTransition = (preview: SkillPreview, key: string) => apiFetch<{ itemId: string; etag: string | null }>(`${root}/${preview.itemId}/${preview.operation}`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': preview.etag }), body: JSON.stringify({ confirm: true }) });
