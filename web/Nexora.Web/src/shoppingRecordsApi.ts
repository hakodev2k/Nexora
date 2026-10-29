import { apiFetch } from './api';

export type ShoppingKind = 'sellers' | 'orders' | 'warranties';
export type ShoppingPage<T> = { items: T[]; page: number; hasMore: boolean };
export type SellerRecord = { id: string; title: string; url: string | null; linkedOrderCount: number | null; etag: string };
export type ShoppingLine = { id: string; title: string; quantity: string; unitAmount: string; variant: string | null };
export type ShoppingOrderSummary = { id: string; orderNumber: string | null; sellerTitle: string | null; purchasedOn: string; currency: string; status: string; totalAmount: string; etag: string };
export type ShoppingOrder = ShoppingOrderSummary & { sellerId: string | null; shippingAmount: string; taxAmount: string; discountAmount: string; notes: string | null; lines: ShoppingLine[] };
export type WarrantyRecord = { id: string; orderLineId: string; title: string; coverageKind: 'Fixed' | 'Lifetime' | 'Unknown'; startsOn: string | null; endsOn: string | null; provider: string | null; terms: string | null; claimContact: string | null; etag: string };
export type ShoppingRow = SellerRecord | ShoppingOrderSummary | WarrantyRecord;
export type ShoppingRecord = SellerRecord | ShoppingOrder | WarrantyRecord;
export type ShoppingIntent = { kind: ShoppingKind; id?: string; etag?: string; key: string; body: string };
const root = '/api/v1/shopping/records';
export const shoppingRecordCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export function shoppingRecordList<T extends ShoppingRow>(kind: ShoppingKind, page: number, query: string) {
  return apiFetch<ShoppingPage<T>>(root + '/' + kind + '?' + new URLSearchParams({ page: String(page), query }));
}
export const shoppingRecordGet = <T extends ShoppingRecord>(kind: ShoppingKind, id: string) => apiFetch<T>(root + '/' + kind + '/' + encodeURIComponent(id));
export async function sendShoppingIntent(intent: ShoppingIntent): Promise<{ id: string }> {
  let result: unknown;
  try {
    result = await apiFetch(root + '/' + intent.kind + (intent.id ? '/' + encodeURIComponent(intent.id) : ''), {
      method: intent.id ? 'PUT' : 'POST', headers: { 'Idempotency-Key': intent.key, ...(intent.etag ? { 'If-Match': intent.etag } : {}) }, body: intent.body
    });
    const value = result as { id?: unknown; committed?: unknown; detailAvailable?: unknown } | null;
    if (!value || typeof value.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.id) ||
        value.id === '00000000-0000-0000-0000-000000000000' || value.committed !== true || value.detailAvailable !== false ||
        (intent.id && value.id.toLowerCase() !== intent.id.toLowerCase())) throw Error('Missing acknowledgement');
    return { id: value.id };
  } catch (error) {
    if (typeof (error as { status?: unknown })?.status === 'number') throw error;
    throw Object.assign(new Error('Chưa xác định kết quả lưu. Thử lại đúng yêu cầu để khôi phục kết quả.'), { status: 0, code: 'SaveOutcomeUnknown' });
  }
}
