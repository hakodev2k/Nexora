import { useState } from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { ResourceFormDialog, useResourceDialog } from './ResourceFormDialog';
function Fixture({ busy = false }: { busy?: boolean }) {
  const [value, setValue] = useState(''); const form = useResourceDialog(value, setValue);
  return <><button onClick={() => form.begin()}>Open</button><ResourceFormDialog open={form.open} title="Edit record" dirty={form.dirty} busy={busy} onClose={form.cancel}>
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
