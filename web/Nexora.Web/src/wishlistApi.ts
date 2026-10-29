import { apiFetch, jsonMutationHeaders } from './api';

export type WishlistItem = { id: string; title: string; url: string | null; quantity: string; targetAmount: string | null; currency: string | null; notes: string | null; status: 'Wanted' | 'Purchased' | 'Archived' | 'Trash'; createdAt: string; updatedAt: string; etag: string };
export type WishlistCommand = { title: string; quantity: string; url: string | null; targetAmount: string | null; currency: string | null; notes: string | null };
export type WishlistOperation = 'mark-purchased' | 'archive' | 'unarchive' | 'trash' | 'restore' | 'purge';
export type WishlistPreview = { itemId: string; etag: string; operation: WishlistOperation; referenceCount: number };
export const wishlistCapabilities = () => apiFetch<Record<string, boolean>>('/api/v1/shopping/wishlist/capabilities');
export const wishlistGet = (id: string) => apiFetch<WishlistItem>(`/api/v1/shopping/wishlist/${encodeURIComponent(id)}`);
export const wishlistList = (status: string, query: string, cursor?: string) => {
  const params = new URLSearchParams({ status, query }); if (cursor) params.set('cursor', cursor);
  return apiFetch<{ items: WishlistItem[]; nextCursor: string | null }>('/api/v1/shopping/wishlist?' + params);
};
export const wishlistSave = (body: WishlistCommand, key: string, item?: WishlistItem) => apiFetch<{ itemId: string; etag: string }>(`/api/v1/shopping/wishlist${item ? '/' + item.id : ''}`, { method: item ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, item ? { 'If-Match': item.etag } : {}), body: JSON.stringify(body) });
export const wishlistPreview = (item: WishlistItem, operation: WishlistOperation) => apiFetch<WishlistPreview>(`/api/v1/shopping/wishlist/${item.id}/preview-${operation}`, { method: 'POST', headers: jsonMutationHeaders(crypto.randomUUID()) });
export const wishlistTransition = (preview: WishlistPreview, key: string) => apiFetch<{ itemId: string; etag: string | null }>(`/api/v1/shopping/wishlist/${preview.itemId}/${preview.operation}`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': preview.etag }), body: JSON.stringify({ confirm: true }) });
