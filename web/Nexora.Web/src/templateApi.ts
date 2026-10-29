import { apiFetch, jsonMutationHeaders, NexoraApiError } from './api';

export type BookmarkTemplateSeed = { title: string | null; url: string | null; description: string | null };
export type TemplateSummary = { id: string; name: string; moduleCode: 'FX21'; resourceType: 'Bookmark'; schemaVersion: 1; status: 'Active' | 'Archived' | 'Trash'; etag: string };
export type TemplateDetail = { template: TemplateSummary; seed: BookmarkTemplateSeed; canUse: boolean; unavailableReason: string | null };
export type TemplatePage = { items: TemplateSummary[]; hasMore: boolean; page: number };
export type TemplateDraft = { templateId: string; templateETag: string; seed: BookmarkTemplateSeed; moduleCode: 'FX21'; resourceType: 'Bookmark' };
export type TemplateIntent = Readonly<{ id?: string; action: 'create' | 'update' | 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge'; body?: string; etag?: string; key: string }>;
const base = '/api/v1/organization/templates';
export const templateCapabilities = () => apiFetch<Record<string, boolean>>(`${base}/capabilities`);
export const listTemplates = (query: string, state: string, page: number) => apiFetch<TemplatePage>(`${base}?${new URLSearchParams({ query, state, page: String(page) })}`);
export const getTemplate = (id: string) => apiFetch<TemplateDetail>(`${base}/${encodeURIComponent(id)}`);
export async function prepareTemplateDraft(item: TemplateSummary): Promise<TemplateDraft> {
  const value = await apiFetch<TemplateDraft>(`${base}/${encodeURIComponent(item.id)}/draft`, { headers: { 'If-Match': item.etag } });
  const seed = value?.seed;
  if (!value || value.templateId?.toLowerCase() !== item.id.toLowerCase() || value.templateETag !== item.etag ||
      value.moduleCode !== 'FX21' || value.resourceType !== 'Bookmark' || !seed ||
      ![seed.title, seed.url, seed.description].every(v => v === null || typeof v === 'string') ||
      (seed.title?.length ?? 0) > 200 || (seed.url?.length ?? 0) > 2048 || (seed.description?.length ?? 0) > 4000)
    throw new NexoraApiError('Không thể mở mẫu này. Hãy tải lại phiên bản hiện tại.', 409, 'TemplateUnavailable');
  return value;
}
export async function sendTemplateIntent(intent: TemplateIntent): Promise<string> {
  const path = intent.action === 'create' ? '' : `/${encodeURIComponent(intent.id ?? '')}${intent.action === 'update' ? '' : '/lifecycle/' + intent.action}`;
  const result = await apiFetch<{ id: string; committed: boolean; detailAvailable: boolean }>(base + path, {
    method: intent.action === 'update' ? 'PUT' : 'POST',
    headers: jsonMutationHeaders(intent.key, intent.etag ? { 'If-Match': intent.etag } : {}), body: intent.body
  }, intent.action === 'create' ? 201 : 200);
  if (!result || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id) ||
      result.id === '00000000-0000-0000-0000-000000000000' || result.committed !== true || result.detailAvailable !== false ||
      (intent.id && result.id.toLowerCase() !== intent.id.toLowerCase()))
    throw new NexoraApiError('Chưa xác nhận được thao tác. Thử lại cùng yêu cầu.', 0, 'UnexpectedAcknowledgement');
  return result.id;
}
