import { beforeEach, describe, expect, it, vi } from 'vitest';
import { apiFetch } from './api';
import { certificationCreate, certificationRenew, type Certification } from './certificationApi';
vi.mock('./api', () => ({ apiFetch: vi.fn() }));
const id = '77777777-7777-4777-8777-777777777777';
beforeEach(() => vi.resetAllMocks());
describe('Certification committed acknowledgement boundary', () => {
  it('accepts only an opaque committed result and never substitutes a full private DTO', async () => {
    vi.mocked(apiFetch).mockResolvedValueOnce({ id, committed: true, detailAvailable: false });
    await expect(certificationCreate({ title: 'Title', issuer: 'Issuer', verificationUrl: null, issuedOn: null, expiresOn: null }, 'key')).resolves.toEqual({ id, committed: true, detailAvailable: false });
    vi.mocked(apiFetch).mockResolvedValueOnce({ id, title: 'Private DTO' });
    await expect(certificationCreate({ title: 'Title', issuer: 'Issuer', verificationUrl: null, issuedOn: null, expiresOn: null }, 'key')).rejects.toMatchObject({ status: 0, code: 'SaveOutcomeUnknown' });
  });
  it('keeps a mismatched resource ACK uncertain after a renewal', async () => {
    vi.mocked(apiFetch).mockResolvedValue({ id: '88888888-8888-4888-8888-888888888888', committed: true, detailAvailable: false });
    await expect(certificationRenew({ id, etag: 'etag' } as Certification, { issuedOn: null, expiresOn: null, reason: null }, 'key')).rejects.toMatchObject({ status: 0, code: 'SaveOutcomeUnknown' });
  });
});
