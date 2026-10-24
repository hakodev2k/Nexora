import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { expect, test, vi } from 'vitest';
import { FileRestoreDialog } from './FileRestoreDialog';
import { server } from './test/server';

const fileId = '71f3946e-1166-4a3e-9b23-40d6f40193ba';
const batchId = '5184201b-b6ce-4a37-b978-345925c5c36b';
const etag = '"AAAAAAAAAAA="';

test('a historical committed restore acknowledgement clears private preview and never replays another cohort', async () => {
  const user = userEvent.setup(); const done = vi.fn(); const denied = vi.fn(); let writes = 0;
  server.use(
    http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 1800 })),
    http.get(`*/api/v1/files/${fileId}/restore-preview`, () => HttpResponse.json({
      file: { id: fileId, originalName: 'Private synthetic name.txt', lifecycle: 'Trash', scanState: 'Clean', etag },
      deletionBatchId: batchId, canRestore: true, blockCode: null
    })),
    http.post(`*/api/v1/files/${fileId}/restore`, async ({ request }) => {
      writes++;
      expect(request.headers.get('If-Match')).toBe(etag);
      expect(request.headers.get('Idempotency-Key')).toMatch(/^[0-9a-f-]{36}$/i);
      expect(await request.json()).toEqual({ deletionBatchId: batchId });
      return HttpResponse.json({ id: fileId, committed: true, detailAvailable: false });
    })
  );
  render(<FileRestoreDialog fileId={fileId} onClose={vi.fn()} onDone={done} onDenied={denied} />);
  await waitFor(() => expect(screen.getByRole('button', { name: /^Restore$/ })).toBeEnabled());
  await user.click(screen.getByRole('button', { name: /^Restore$/ }));
  await waitFor(() => expect(done).toHaveBeenCalledWith(false));
  expect(done).toHaveBeenCalledTimes(1); expect(denied).not.toHaveBeenCalled(); expect(writes).toBe(1);
  expect(screen.queryByText(/Private synthetic name/)).toBeNull();
  expect(screen.queryByRole('button', { name: 'Retry cùng request' })).toBeNull();
  expect(screen.getByRole('button', { name: /^Restore$/ })).toBeDisabled();
});
