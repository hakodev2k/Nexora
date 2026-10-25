import { apiFetch } from './api';

export type Certification = { id: string; title: string; issuer: string; issuedOn: string | null; expiresOn: string | null;
  verificationUrl: string | null; status: 'Active' | 'Archived'; effectiveStatus: 'Active' | 'Expired' | 'Archived'; versionNumber: string;
  createdAt: string; updatedAt: string; etag: string };
export type CertificationPage = { items: Certification[]; nextCursor: string | null };
export type CertificationSnapshot = { schemaVersion: 1; title: string; issuer: string; issuedOn: string | null;
  expiresOn: string | null; verificationUrl: string | null; status: string };
export type CertificationVersion = { id: string; versionNumber: string; operation: string; snapshot: CertificationSnapshot; reason: string | null; createdAt: string };
export type CertificationHistoryPage = { items: CertificationVersion[]; nextCursor: string | null };
export type CertificationPreview = { id: string; etag: string; operation: 'archive' | 'unarchive'; versionNumber: string; expiresAt: string; previewToken: string };
export type CertificationDraft = { title: string; issuer: string; verificationUrl: string | null; issuedOn: string | null; expiresOn: string | null };
export type CertificationAcknowledgement = { id: string; committed: true; detailAvailable: false };
const root = '/api/v1/learning/certifications';
export const certificationCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export function certificationList(status: string, query: string, cursor?: string | null) {
  const params = new URLSearchParams({ status }); if (query) params.set('query', query); if (cursor) params.set('cursor', cursor);
  return apiFetch<CertificationPage>(root + '/?' + params);
}
export const certificationGet = (id: string) => apiFetch<Certification>(root + '/' + encodeURIComponent(id));
export const certificationHistory = (id: string, cursor?: string | null) => apiFetch<CertificationHistoryPage>(root + '/' + encodeURIComponent(id) + '/history' + (cursor ? '?cursor=' + encodeURIComponent(cursor) : ''));
export const certificationPreview = (id: string, operation: 'archive' | 'unarchive') => apiFetch<CertificationPreview>(root + '/' + encodeURIComponent(id) + '/preview-' + operation, { method: 'POST' });
async function acknowledgement(request: Promise<unknown>, expectedId?: string): Promise<CertificationAcknowledgement> {
  try {
    const value = await request as Partial<CertificationAcknowledgement> | null;
    if (!value || typeof value.id !== 'string' || !/^[\da-f]{8}-(?:[\da-f]{4}-){3}[\da-f]{12}$/i.test(value.id)
      || value.id === '00000000-0000-0000-0000-000000000000' || (expectedId !== undefined && value.id !== expectedId) || value.committed !== true || value.detailAvailable !== false)
      throw new Error('Invalid acknowledgement');
    return { id: value.id, committed: true, detailAvailable: false };
  } catch (error) {
    if (typeof (error as { status?: unknown })?.status === 'number') throw error;
    throw Object.assign(new Error('Chưa xác nhận được kết quả lưu. Hãy thử lại cùng yêu cầu.'), { status: 0, code: 'SaveOutcomeUnknown' });
  }
}
export const certificationCreate = (body: CertificationDraft, key: string) => acknowledgement(apiFetch(root + '/', { method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify(body) }));
export const certificationUpdate = (row: Certification, body: Omit<CertificationDraft, 'issuedOn' | 'expiresOn'>, key: string) => acknowledgement(apiFetch(root + '/' + row.id,
  { method: 'PUT', headers: { 'Idempotency-Key': key, 'If-Match': row.etag }, body: JSON.stringify(body) }), row.id);
export const certificationRenew = (row: Certification, body: { issuedOn: string | null; expiresOn: string | null; reason: string | null }, key: string) => acknowledgement(apiFetch(root + '/' + row.id + '/renew',
  { method: 'POST', headers: { 'Idempotency-Key': key, 'If-Match': row.etag }, body: JSON.stringify(body) }), row.id);
export const certificationTransition = (preview: CertificationPreview, key: string) => acknowledgement(apiFetch(root + '/' + preview.id + '/' + preview.operation,
  { method: 'POST', headers: { 'Idempotency-Key': key, 'If-Match': preview.etag }, body: JSON.stringify({ confirm: true, expiresAt: preview.expiresAt, previewToken: preview.previewToken }) }), preview.id);
