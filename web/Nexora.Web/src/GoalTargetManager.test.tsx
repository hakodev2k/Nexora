import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { expect, it, vi } from 'vitest';
import { GoalTargetManager } from './GoalTargetManager';
import type { GoalDetail } from './api';
const id = '77777777-7777-4777-8777-777777777777';
const detail: GoalDetail = {
  goal: { id, title: 'Synthetic active goal', description: null, startDate: null, endDate: null, status: 'Active', progress: 0, targetCount: 1, createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"original"' },
  targets: [{ id, title: 'Numeric target', kind: 'Numeric', initialValue: 0, currentValue: 0, targetValue: 10, initialValueExact: '0', currentValueExact: '0', targetValueExact: '10', progress: 0, updatedAt: '2026-10-01T00:00:00Z', etag: '"target"' }]
};
async function draft() {
  const user = userEvent.setup(), command = vi.fn();
  render(<GoalTargetManager detail={detail} busy={false} uncertain={false} onCommand={command} onError={vi.fn()} />);
  await user.click(screen.getByRole('button', { name: 'Ghi tiến độ' })); const modal = screen.getByRole('dialog');
  await user.clear(within(modal).getByLabelText('Giá trị mới')); await user.type(within(modal).getByLabelText('Giá trị mới'), '7');
  await user.type(within(modal).getByLabelText('Ghi chú (tùy chọn)'), 'Private unsaved progress note');
  return { user, command, modal };
}
it('one shared Cancel guards unsaved progress and Continue retains value and note', async () => {
  const { user, command, modal } = await draft();
  expect(within(modal).getAllByRole('button', { name: 'Hủy' })).toHaveLength(1);
  await user.click(within(modal).getByRole('button', { name: 'Hủy' })); expect(within(modal).getByText('Bỏ thay đổi chưa lưu?')).toBeVisible();
  await user.click(within(modal).getByRole('button', { name: 'Tiếp tục sửa' }));
  expect(within(modal).getByLabelText('Giá trị mới')).toHaveValue('7'); expect(within(modal).getByLabelText('Ghi chú (tùy chọn)')).toHaveValue('Private unsaved progress note'); expect(command).not.toHaveBeenCalled();
});
it('explicit discard resets progress draft to the current persisted target values', async () => {
  const { user, command, modal } = await draft(); await user.click(within(modal).getByRole('button', { name: 'Hủy' })); await user.click(within(modal).getByRole('button', { name: 'Bỏ bản nháp' })); expect(screen.queryByRole('dialog')).toBeNull();
  await user.click(screen.getByRole('button', { name: 'Ghi tiến độ' })); expect(screen.getByLabelText('Giá trị mới')).toHaveValue('0'); expect(screen.getByLabelText('Ghi chú (tùy chọn)')).toHaveValue(''); expect(command).not.toHaveBeenCalled();
});
