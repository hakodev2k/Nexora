import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiFetch } from './api';
import { newsCategoryCreate, newsCategoryUpdate } from './newsCategoryApi';

vi.mock('./api', () => ({ apiFetch: vi.fn() }));
const fetch = vi.mocked(apiFetch);
const metadata = { schemaVersion: 1 as const, name: 'News' };
const ack = { id: '11111111-1111-1111-1111-111111111111', etag: '"AAAAAAAAAAE="' };

describe('category save acknowledgement recovery', () => {
  beforeEach(() => { fetch.mockReset(); });
  it.each([undefined, {}, { ...ack, id: 'invalid' }, { ...ack, etag: '"AAAAAAAAAAF="' }])('keeps a missing or invalid ACK outcome unknown', async value => {
    fetch.mockResolvedValue(value);
    await expect(newsCategoryCreate(metadata, 'same-key')).rejects.toMatchObject({ status: 0, code: 'SaveOutcomeUnknown' });
  });
  it('keeps JSON decoding failure recoverable with the same update request', async () => {
    fetch.mockRejectedValueOnce(new SyntaxError('Malformed JSON')).mockResolvedValueOnce(ack);
    const item = { ...ack, metadata, createdAt: '', updatedAt: '' };
    await expect(newsCategoryUpdate(item, metadata, 'same-key')).rejects.toMatchObject({ status: 0 });
    await expect(newsCategoryUpdate(item, metadata, 'same-key')).resolves.toEqual(ack);
    expect(fetch.mock.calls[0]).toEqual(fetch.mock.calls[1]);
  });
  it.each([403, 412, 503])('preserves known HTTP failure %s', async status => {
    const error = Object.assign(new Error('Request failed'), { status });
    fetch.mockRejectedValue(error);
    let caught: unknown;
    try { await newsCategoryCreate(metadata, 'same-key'); } catch (failure) { caught = failure; }
    expect(caught === error).toBe(true);
    expect((caught as { status: number }).status).toBe(status);
  });
  it('accepts a canonical ACK without exposing extra response fields', async () => {
    fetch.mockResolvedValue({ ...ack, ignored: 'value' });
    await expect(newsCategoryCreate(metadata, 'same-key')).resolves.toEqual(ack);
  });
});
