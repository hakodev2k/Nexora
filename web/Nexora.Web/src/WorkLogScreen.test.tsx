import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, expect, it, vi } from 'vitest';
import { WorkLogScreen } from './WorkLogScreen';
import * as api from './workLogApi';
vi.mock('./workLogApi');
beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.workLogCapabilities).mockResolvedValue({ 'learning.worklog.read': true, 'learning.worklog.create': true });
  vi.mocked(api.workLogList).mockResolvedValue({ items: [], nextCursor: null });
});
async function dirtyDraft() {
  const user = userEvent.setup(); render(<WorkLogScreen onAuthLost={vi.fn()} timeZoneId="UTC" />);
  await user.click(await screen.findByRole('button', { name: 'Nhật ký mới' }));
  const modal = screen.getByRole('dialog');
  await user.type(within(modal).getByLabelText('Tiêu đề'), 'Keep unsaved work');
  await user.type(within(modal).getByLabelText('Thời lượng (phút)'), '15');
  return { user, modal };
}
it('a single Cancel uses the shared dirty guard and Continue retains entered fields', async () => {
  const { user, modal } = await dirtyDraft();
  expect(within(modal).getAllByRole('button', { name: 'Hủy' })).toHaveLength(1);
  await user.click(within(modal).getByRole('button', { name: 'Hủy' }));
  expect(within(modal).getByText('Bỏ thay đổi chưa lưu?')).toBeVisible();
  await user.click(within(modal).getByRole('button', { name: 'Tiếp tục sửa' }));
  expect(within(modal).getByLabelText('Tiêu đề')).toHaveValue('Keep unsaved work');
  expect(within(modal).getByLabelText('Thời lượng (phút)')).toHaveValue(15);
  expect(api.workLogSave).not.toHaveBeenCalled();
});
it('only explicit discard closes and resets the private unsaved draft', async () => {
  const { user, modal } = await dirtyDraft();
  await user.click(within(modal).getByRole('button', { name: 'Hủy' }));
  await user.click(within(modal).getByRole('button', { name: 'Bỏ bản nháp' }));
  expect(screen.queryByRole('dialog')).toBeNull();
  await user.click(screen.getByRole('button', { name: 'Nhật ký mới' }));
  expect(screen.getByLabelText('Tiêu đề', { selector: '#worklog-title-input' })).toHaveValue('');
  expect(screen.getByLabelText('Thời lượng (phút)')).toHaveValue(null);
  expect(api.workLogSave).not.toHaveBeenCalled();
});
