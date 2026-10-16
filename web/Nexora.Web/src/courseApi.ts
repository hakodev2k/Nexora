import { apiFetch, jsonMutationHeaders } from './api';

const root = '/api/v1/learning/courses';
export type Course = { id: string; title: string; provider: string | null; url: string | null; notes: string | null;
  progressMode: 'ManualPercent' | 'Milestones'; manualProgress: string | null; progressModeLocked: boolean;
  milestonesDone: number; milestonesTotal: number; status: string; startedOn: string | null; completedOn: string | null; updatedAt: string; etag: string };
export type Milestone = { id: string; title: string; position: number; completed: boolean; completedAt: string | null; etag: string };
export type CourseDraft = { title: string; provider: string | null; url: string | null; notes: string | null; progressMode: string; startedOn: string | null };
export type CourseOperation = 'complete' | 'abandon' | 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge';
export type CoursePreview = { itemId: string; etag: string; operation: CourseOperation; referenceCount: number };
export type MilestonePage = { items: Milestone[]; nextCursor: string | null; courseETag: string };
export const courseCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export const courseGet = (id: string) => apiFetch<Course>(`${root}/${id}`);
export const courseList = (status: string, query: string, cursor?: string) => {
  const params = new URLSearchParams({ status, query }); if (cursor) params.set('cursor', cursor);
  return apiFetch<{ items: Course[]; nextCursor: string | null }>(root + '?' + params);
};
export const courseSave = (body: CourseDraft, key: string, item?: Course) => apiFetch<{ itemId: string; etag: string }>(root + (item ? '/' + item.id : ''),
  { method: item ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, item ? { 'If-Match': item.etag } : {}), body: JSON.stringify(body) });
export const courseProgress = (item: Course, body: { manualProgress?: string; start: boolean; startedOn?: string }, key: string) => apiFetch<{ itemId: string; etag: string }>(`${root}/${item.id}/progress`,
  { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': item.etag }), body: JSON.stringify(body) });
export const coursePreview = (item: Course, operation: CourseOperation) => apiFetch<CoursePreview>(`${root}/${item.id}/preview-${operation}`, { method: 'POST', headers: jsonMutationHeaders(crypto.randomUUID()) });
export const courseTransition = (preview: CoursePreview, key: string, completedOn?: string) => apiFetch<{ itemId: string; etag: string | null }>(`${root}/${preview.itemId}/${preview.operation}`,
  { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': preview.etag }), body: JSON.stringify({ confirm: true, ...(completedOn ? { completedOn } : {}) }) });
export const courseMilestones = (item: Course, cursor?: string) => apiFetch<MilestonePage>(`${root}/${item.id}/milestones` + (cursor ? '?cursor=' + cursor : ''));
export const milestoneAdd = (item: Course, title: string, key: string) => apiFetch<{ itemId: string; etag: string }>(`${root}/${item.id}/milestones`,
  { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': item.etag }), body: JSON.stringify({ title }) });
export const milestoneChange = (item: Course, child: Milestone, operation: 'update' | 'delete' | 'completion' | 'move', body: object, key: string) => apiFetch<{ itemId: string; etag: string }>(`${root}/${item.id}/milestones/${child.id}` + (['update', 'delete'].includes(operation) ? '' : '/' + operation),
  { method: operation === 'update' ? 'PUT' : operation === 'delete' ? 'DELETE' : 'POST', headers: jsonMutationHeaders(key, { 'If-Match': item.etag, 'Milestone-If-Match': child.etag }), body: JSON.stringify(body) });

// Shift decimal digits as text; a percentage has at most six fraction digits so SQL never rounds.
export function percentToFraction(text: string): string {
  if (!/^(?:0|[1-9][0-9]?|100)(?:\.[0-9]{1,6})?$/.test(text)) throw new Error('Enter progress from 0 to 100 with at most six decimal places.');
  const [whole, decimal = ''] = text.split('.'); if (whole === '100' && /[1-9]/.test(decimal)) throw new Error('Progress cannot exceed 100%.');
  const digits = whole.padStart(3, '0'); const fraction = (digits.slice(1) + decimal).replace(/0+$/, '');
  return digits[0] + (fraction ? '.' + fraction : '');
}
export function fractionToPercent(text: string | null): string {
  if (text === null) return ''; const [whole, fraction = ''] = text.split('.'); const digits = fraction.padEnd(2, '0');
  const result = (whole === '1' ? '100' : digits.slice(0, 2).replace(/^0+(?=\d)/, ''));
  const tail = digits.slice(2).replace(/0+$/, ''); return result + (tail ? '.' + tail : '');
}
