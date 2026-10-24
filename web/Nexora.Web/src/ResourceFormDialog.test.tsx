import { requestDirtyLeave } from './navigationGuard';
import { useState } from 'react';
import { act, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { ResourceFormDialog, useResourceDialog } from './ResourceFormDialog';
function Fixture({ busy = false, closeBlocked = false }: { busy?: boolean; closeBlocked?: boolean }) {
  const [value, setValue] = useState(''); const form = useResourceDialog(value, setValue);
  return <><button onClick={() => form.begin()}>Open</button><ResourceFormDialog open={form.open} title="Edit record" dirty={form.dirty} busy={busy} closeBlocked={closeBlocked} onClose={form.cancel}>
    <label htmlFor="value">Value</label><input id="value" value={value} onChange={event => setValue(event.target.value)} /><button disabled={busy}>Save</button>
  </ResourceFormDialog></>;
}
describe('ResourceFormDialog', () => {
  it('retains dirty text on Escape, discards explicitly, and restores trigger focus', async () => {
    const user = userEvent.setup(); render(<Fixture />); await user.click(screen.getByText('Open')); const input = screen.getByLabelText('Value'); expect(input).toHaveFocus();
    await user.type(input, 'draft'); await user.keyboard('{Escape}'); expect(screen.getByText('Bỏ thay đổi chưa lưu?')).toBeVisible(); await user.click(screen.getByText('Tiếp tục sửa')); expect(input).toHaveValue('draft'); await user.keyboard('{Escape}'); await user.click(screen.getByText('Bỏ bản nháp'));
    await waitFor(() => expect(screen.getByText('Open')).toHaveFocus()); expect(screen.queryByRole('dialog')).toBeNull(); await user.click(screen.getByText('Open')); expect(screen.getByLabelText('Value')).toHaveValue('');
  });
  it('traps keyboard focus and blocks Escape/cancel while a request is busy', async () => {
    const user = userEvent.setup(); render(<Fixture busy />); await user.click(screen.getByText('Open')); await user.keyboard('{Escape}'); expect(screen.getByRole('dialog')).toBeVisible(); expect(screen.getByText('Hủy')).toBeDisabled(); expect(screen.getByLabelText('Value')).toBeDisabled(); await user.tab({ shift: true }); expect(screen.getByRole('dialog')).toHaveFocus();
  });
  it('focuses the first field when initial asynchronous loading finishes', async () => {
    const user = userEvent.setup(); const view = render(<Fixture busy />);
    await user.click(screen.getByText('Open')); expect(screen.getByRole('dialog')).toHaveFocus();
    view.rerender(<Fixture busy={false} />);
    await waitFor(() => expect(screen.getByLabelText('Value')).toHaveFocus());
  });
});

it('protects a dirty dialog from navigation and reload until explicitly discarded', async () => {
  const user = userEvent.setup(); render(<Fixture />); await user.click(screen.getByText('Open'));
  await user.type(screen.getByLabelText('Value'), 'private draft'); const navigate = vi.fn();
  await act(async () => { requestDirtyLeave(navigate); }); expect(navigate).not.toHaveBeenCalled();
  await user.click(screen.getByText('Tiếp tục sửa')); expect(screen.getByLabelText('Value')).toHaveValue('private draft');
  const unload = new Event('beforeunload', { cancelable: true }); window.dispatchEvent(unload); expect(unload.defaultPrevented).toBe(true);
  await act(async () => { requestDirtyLeave(navigate); }); await user.click(screen.getByText('Bỏ bản nháp')); expect(navigate).toHaveBeenCalledTimes(1);
  const cleanUnload = new Event('beforeunload', { cancelable: true }); window.dispatchEvent(cleanUnload); expect(cleanUnload.defaultPrevented).toBe(false);
});
it('blocks navigation during a pending command and announces the reason', async () => {
  const user = userEvent.setup(); render(<Fixture busy />); await user.click(screen.getByText('Open')); const navigate = vi.fn();
  await act(async () => { requestDirtyLeave(navigate); }); expect(navigate).not.toHaveBeenCalled(); expect(screen.getByRole('status')).toHaveTextContent('Đang xử lý');
});

it('retains unresolved commands even when the draft is clean and not actively sending', async () => {
  const user = userEvent.setup(); render(<Fixture closeBlocked />); await user.click(screen.getByText('Open'));
  const navigate = vi.fn(); await act(async () => { requestDirtyLeave(navigate); });
  expect(navigate).not.toHaveBeenCalled(); expect(screen.queryByText('Bỏ thay đổi chưa lưu?')).toBeNull();
  await user.keyboard('{Escape}'); expect(screen.getByRole('dialog')).toBeVisible(); expect(screen.getByText('Hủy')).toBeDisabled();
  const unload = new Event('beforeunload', { cancelable: true }); window.dispatchEvent(unload); expect(unload.defaultPrevented).toBe(true);
});
