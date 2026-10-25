import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CertificationScreen } from './CertificationScreen';
import * as api from './certificationApi';
vi.mock('./certificationApi');
const id = '77777777-7777-4777-8777-777777777777';
const row: api.Certification = { id, title: 'Chứng chỉ riêng tư', issuer: 'Đơn vị cấp', issuedOn: '2025-01-01', expiresOn: '2026-01-01', verificationUrl: null,
  status: 'Active', effectiveStatus: 'Expired', versionNumber: '1', createdAt: '2025-01-01T00:00:00Z', updatedAt: '2025-01-01T00:00:00Z', etag: '"AAAAAAAAAAE="' };
const caps = Object.fromEntries(['read', 'create', 'update', 'renew', 'archive', 'unarchive'].map(action => ['learning.certification.' + action, true]));
beforeEach(() => {
  vi.resetAllMocks(); vi.mocked(api.certificationCapabilities).mockResolvedValue(caps);
  vi.mocked(api.certificationList).mockResolvedValue({ items: [row], nextCursor: null }); vi.mocked(api.certificationGet).mockResolvedValue(row);
  vi.mocked(api.certificationCreate).mockResolvedValue({ id, committed: true, detailAvailable: false });
});
describe('Certification manual authority and recovery', () => {
  it.each([408, 500, 502, 503, 504])('freezes and retries the exact create body/key after HTTP%s', async status => {
    vi.mocked(api.certificationCreate).mockRejectedValueOnce(Object.assign(new Error('Kết quả chưa rõ'), { status }));
    const user = userEvent.setup(); render(<CertificationScreen onAuthLost={vi.fn()} />);
    await user.click(await screen.findByRole('button', { name: 'Thêm chứng chỉ' }));
    await user.type(screen.getByLabelText('Tên chứng chỉ'), 'Chứng chỉ mới'); await user.type(screen.getByLabelText('Đơn vị cấp'), 'Issuer');
    await user.click(screen.getByRole('button', { name: 'Lưu chứng chỉ' })); await screen.findByText('Kết quả chưa rõ');
    expect(screen.getByLabelText('Tên chứng chỉ')).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'Thử lại cùng yêu cầu' }));
    await waitFor(() => expect(api.certificationCreate).toHaveBeenCalledTimes(2));
    expect(vi.mocked(api.certificationCreate).mock.calls[1]).toEqual(vi.mocked(api.certificationCreate).mock.calls[0]);
    await waitFor(() => expect(screen.queryByLabelText('Tên chứng chỉ')).toBeNull());
  });
  it('does not read private certificates when only create is authorized', async () => {
    vi.mocked(api.certificationCapabilities).mockResolvedValue({ 'learning.certification.create': true });
    render(<CertificationScreen onAuthLost={vi.fn()} />);
    await screen.findByText('Bạn có thể thêm chứng chỉ. Xem thông tin đã lưu cần quyền đọc riêng.');
    expect(api.certificationList).not.toHaveBeenCalled(); expect(api.certificationGet).not.toHaveBeenCalled();
    expect(screen.queryByText('Chứng chỉ riêng tư')).toBeNull();
  });
  it('clears the private detail and draft when a current mutation is forbidden', async () => {
    vi.mocked(api.certificationUpdate).mockRejectedValueOnce(Object.assign(new Error('Action denied'), { status: 403 }));
    const user = userEvent.setup(); render(<CertificationScreen onAuthLost={vi.fn()} certificationId={id} />);
    await user.click(await screen.findByRole('button', { name: 'Sửa thông tin' }));
    await user.clear(screen.getByLabelText('Tên chứng chỉ')); await user.type(screen.getByLabelText('Tên chứng chỉ'), 'Private changed title');
    await user.click(screen.getByRole('button', { name: 'Lưu chứng chỉ' })); await screen.findByText('Action denied');
    expect(screen.queryByText('Chứng chỉ riêng tư')).toBeNull(); expect(screen.queryByDisplayValue('Private changed title')).toBeNull();
  });
  it('shows archived metadata read-only without renewal or metadata edits', async () => {
    vi.mocked(api.certificationGet).mockResolvedValue({ ...row, status: 'Archived', effectiveStatus: 'Archived' });
    render(<CertificationScreen onAuthLost={vi.fn()} certificationId={id} />);
    await screen.findByText('Chứng chỉ đang được lưu trữ ở chế độ chỉ đọc. Bỏ lưu trữ trước khi cập nhật.');
    expect(screen.queryByRole('button', { name: 'Sửa thông tin' })).toBeNull(); expect(screen.queryByRole('button', { name: 'Ghi nhận gia hạn' })).toBeNull();
    expect(screen.getByRole('button', { name: 'Bỏ lưu trữ' })).toBeEnabled();
  });
});
