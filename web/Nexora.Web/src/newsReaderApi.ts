import { apiFetch, isProductivityAcknowledgement, jsonMutationHeaders, NexoraApiError, type ReadingPage } from './api';
export type NewsSource = { id: string; feedId: string; url: string; title: string; titleOverride: string | null; categoryId: string | null; state: string; fetchState: string; lastAttempt: string | null; lastSuccess: string | null; lastErrorCode: string | null; etag: string };
export type NewsArticle = { id: string; subscriptionId: string; title: string; sourceTitle: string; originalUrl: string; publishedAt: string | null; fetchedAt: string; isExcerpt: boolean; body: string | null; read: boolean; stateETag: string };
export type NewsHistoryItem = { articleId: string; openedAt: string; article: NewsArticle | null };
const root = '/api/v1/news';
export const newsCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export function newsSources(filters: { query: string; categoryId: string; state: string }, cursor?: string) {
  const query = new URLSearchParams(); (['query', 'categoryId', 'state'] as const).forEach(key => { if (filters[key]) query.set(key, filters[key]); }); if (cursor) query.set('cursor', cursor);
  return apiFetch<{ items: NewsSource[]; nextCursor: string | null }>(root + '/sources?' + query);
}
export function newsArticles(filters: { subscriptionId: string; categoryId: string; query: string; unreadOnly: boolean; from?: string; to?: string }, cursor?: string) {
  const query = new URLSearchParams({ unreadOnly: String(filters.unreadOnly) }); if (filters.subscriptionId) query.set('subscriptionId', filters.subscriptionId);
  if (filters.categoryId) query.set('categoryId', filters.categoryId); if (filters.query) query.set('query', filters.query); if (cursor) query.set('cursor', cursor);
  if (filters.from) query.set('from', filters.from); if (filters.to) query.set('to', filters.to);
  return apiFetch<{ items: NewsArticle[]; nextCursor: string | null; watermark: string }>(root + '/articles?' + query);
}
export async function newsArticle(id: string) {
  const article = await apiFetch<NewsArticle>(root + '/articles/' + encodeURIComponent(id));
  if (!article || article.id !== id || typeof article.title !== 'string' || (article.body !== null && typeof article.body !== 'string') || typeof article.stateETag !== 'string')
    throw new NexoraApiError('Phản hồi bài viết không hợp lệ.', 0, 'UnexpectedResponse');
  return article;
}
async function committed(path: string, method: string, key: string, body?: unknown, etag?: string, expectedId?: string) {
  const result = await apiFetch<unknown>(root + path, { method, headers: jsonMutationHeaders(key, etag ? { 'If-Match': etag } : {}), body: body === undefined ? undefined : JSON.stringify(body) }, [200, 201]);
  if (!isProductivityAcknowledgement(result) || typeof result.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(result.id)
      || result.id === '00000000-0000-0000-0000-000000000000' || (expectedId && expectedId !== result.id))
    throw new NexoraApiError('Không xác định được kết quả ghi News.', 0, 'UnexpectedResponse');
  return result;
}
export const newsFollow = (body: { url: string; categoryId: string | null; titleOverride: string | null }, key: string) => committed('/sources', 'POST', key, body);
export const newsUpdateSource = (source: NewsSource, body: { categoryId: string | null; titleOverride: string | null }, key: string) => committed('/sources/' + source.id, 'PUT', key, body, source.etag, source.id);
export const newsSourceState = (source: NewsSource, operation: 'pause' | 'resume' | 'unfollow', key: string) => committed('/sources/' + source.id + '/' + operation, 'POST', key, undefined, source.etag, source.id);
export const newsMarkRead = (article: NewsArticle, read: boolean, key: string) => committed('/articles/' + article.id + '/read', 'POST', key, { read }, article.stateETag, article.id);
export const newsSaveArticle = (id: string, key: string) => committed('/articles/' + id + '/save', 'POST', key);
export const newsMarkAllRead = (subscriptionId: string, through: string, key: string) => committed('/articles/mark-all-read', 'POST', key, { subscriptionIds: [subscriptionId], through }, undefined, subscriptionId);
export const newsHistory = (cursor?: string) => apiFetch<{ items: NewsHistoryItem[]; nextCursor: string | null; watermark: string }>(root + '/history' + (cursor ? '?cursor=' + encodeURIComponent(cursor) : ''));
export const newsClearHistory = (through: string, key: string) => committed('/history/clear', 'POST', key, { through });
export const newsSaved = (cursor?: string) => apiFetch<ReadingPage>(root + '/articles/saved' + (cursor ? '?cursor=' + encodeURIComponent(cursor) : ''));
