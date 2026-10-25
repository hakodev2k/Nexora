import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiFetch } from './api';
import { sendShoppingIntent, type ShoppingIntent } from './shoppingRecordsApi';
vi.mock('./api', () => ({ apiFetch: vi.fn() }));
const id = '23ef11a6-a65d-4d78-a15d-5df95ba7ba26';
const intent: ShoppingIntent = { kind: 'sellers', id, etag: '"AAAAAAAAAAE="', key: '726242e2-cc56-46a1-b7b2-1922ed20acb6', body: '{"title":"Synthetic"}' };
beforeEach(() => vi.resetAllMocks());
describe('Shopping acknowledgement boundary', () => {
  it('requires a matching opaque committed acknowledgement and preserves the original serialized command', async () => {
    vi.mocked(apiFetch).mockResolvedValue({ id, committed: true, detailAvailable: false });
    await expect(sendShoppingIntent(intent)).resolves.toEqual({ id });
    expect(apiFetch).toHaveBeenCalledWith('/api/v1/shopping/records/sellers/' + id, { method: 'PUT', body: intent.body,
      headers: { 'Idempotency-Key': intent.key, 'If-Match': intent.etag } });
  });
  it.each([null, {}, { id, committed: false, detailAvailable: false }, { id: 'deadbeef', committed: true, detailAvailable: false },
    { id: '00000000-0000-0000-0000-000000000000', committed: true, detailAvailable: false },
    { id: '8c43848e-2740-4b77-b98c-cfc087147fd6', committed: true, detailAvailable: false }])('does not turn a malformed or foreign success into an accepted write: %j', async result => {
    vi.mocked(apiFetch).mockResolvedValue(result);
    await expect(sendShoppingIntent(intent)).rejects.toMatchObject({ status: 0, code: 'SaveOutcomeUnknown' });
  });
});
