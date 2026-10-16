import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { beforeEach, expect, it, vi } from 'vitest';
import { getShareLink, updateShareLink, type ShareLinkRecord } from './api';
import { ShareLinkEditor } from './ShareLinkEditor';
import { StrictMode } from 'react';
vi.mock('./api', () => ({ getShareLink: vi.fn(), updateShareLink: vi.fn() }));
vi.mock('./App', () => ({ registerDirtyLeaveGuard: () => () => {} }));
const item: ShareLinkRecord = { id: '11111111-1111-1111-1111-111111111111', resourceType: 'Project', resourceId: '22222222-2222-2222-2222-222222222222', mode: 'PublicLink', expiresAt: null, revokedAt: null, projectionVersion: 'v1', isActive: true, createdAt: '', updatedAt: '', etag: '"AAAAAAAAAAE="', allowedUserIds: [], token: null };
const updated = vi.fn(), closed = vi.fn(), denied = vi.fn();
beforeEach(() => { vi.mocked(getShareLink).mockReset().mockResolvedValue(item); vi.mocked(updateShareLink).mockReset(); updated.mockReset(); closed.mockReset(); denied.mockReset(); });
async function open() { render(<ShareLinkEditor item={item} onUpdated={updated} onClose={closed} onDenied={denied} />); await waitFor(() => expect(screen.getByLabelText('Audience')).toBeEnabled()); }
it('keeps an uncertain save frozen and retries the exact key, body and ETag', async () => {
  vi.mocked(updateShareLink).mockRejectedValueOnce(new SyntaxError('Bad JSON')).mockResolvedValueOnce({ ...item, mode: 'AuthenticatedLink' });
  await open(); fireEvent.change(screen.getByLabelText('Audience'), { target: { value: 'AuthenticatedLink' } }); fireEvent.click(screen.getByRole('button', { name: 'Lưu link' }));
  await screen.findByRole('button', { name: 'Retry cùng request' }); expect(screen.getByLabelText('Audience')).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Retry cùng request' })); await waitFor(() => expect(updated).toHaveBeenCalledOnce());
  expect(vi.mocked(updateShareLink).mock.calls[0]).toEqual(vi.mocked(updateShareLink).mock.calls[1]);
});
it('requires comparison again if the current version changes before explicit reapply', async () => {
  const conflict = { ...item, etag: '"AAAAAAAAAAI="', mode: 'RestrictedUsers', allowedUserIds: [item.id] };
  vi.mocked(getShareLink).mockResolvedValueOnce(item).mockResolvedValueOnce(conflict).mockResolvedValueOnce({ ...conflict, etag: '"AAAAAAAAAAM="' });
  vi.mocked(updateShareLink).mockRejectedValue(Object.assign(new Error('Conflict'), { status: 412 }));
  await open(); fireEvent.change(screen.getByLabelText('Audience'), { target: { value: 'AuthenticatedLink' } }); fireEvent.click(screen.getByRole('button', { name: 'Lưu link' }));
  await screen.findByRole('button', { name: 'Áp dụng lại bản nháp' }); fireEvent.click(screen.getByRole('button', { name: 'Áp dụng lại bản nháp' }));
  await screen.findByText('Bản hiện tại lại thay đổi. Hãy so sánh lại.'); expect(updateShareLink).toHaveBeenCalledOnce(); expect(screen.getByRole('button', { name: 'Lưu link' })).toBeDisabled();
});
it('clears editor authority after a module denial', async () => {
  vi.mocked(updateShareLink).mockRejectedValue(Object.assign(new Error('Unavailable'), { status: 409, code: 'ModuleUnavailable' }));
  await open(); fireEvent.click(screen.getByRole('button', { name: 'Lưu link' })); await waitFor(() => expect(denied).toHaveBeenCalledOnce()); expect(updated).not.toHaveBeenCalled();
});
it('keeps missing successful ACK recoverable instead of silently closing', async () => {
  vi.mocked(updateShareLink).mockResolvedValue(undefined as any);
  await open(); fireEvent.click(screen.getByRole('button', { name: 'Lưu link' })); await screen.findByRole('button', { name: 'Retry cùng request' }); expect(updated).not.toHaveBeenCalled();
});
it('ignores the first StrictMode GET after the current generation permits editing', async () => {
  let resolve!: (value: ShareLinkRecord) => void;
  vi.mocked(getShareLink).mockImplementationOnce(() => new Promise(done => { resolve = done; })).mockResolvedValueOnce(item);
  render(<StrictMode><ShareLinkEditor item={item} onUpdated={updated} onClose={closed} onDenied={denied} /></StrictMode>);
  await waitFor(() => expect(screen.getByLabelText('Audience')).toBeEnabled());
  fireEvent.change(screen.getByLabelText('Audience'), { target: { value: 'AuthenticatedLink' } });
  await act(async () => { resolve({ ...item, mode: 'RestrictedUsers' }); });
  await waitFor(() => expect(screen.getByLabelText('Audience')).toHaveValue('AuthenticatedLink'));
});
