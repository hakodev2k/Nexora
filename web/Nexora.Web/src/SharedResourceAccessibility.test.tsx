import { act, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expect, test, vi } from 'vitest';
import { SharedResourceScreen } from './App';
import { server } from './test/server';

test('public share loading and uniform unavailable state remain inside one main landmark', async () => {
  let finish!: () => void;
  const deferred = new Promise<void>(resolve => { finish = resolve; });
  const login = vi.fn();
  server.use(http.get('*/api/v1/sharing/resolve/synthetic-unavailable', async () => {
    await deferred;
    return HttpResponse.json({ code: 'ResourceUnavailable', title: 'Unavailable' }, { status: 404 });
  }));
  render(<SharedResourceScreen token="synthetic-unavailable" onLogin={login} />);
  const main = screen.getByRole('main');
  expect(screen.getAllByRole('main')).toHaveLength(1);
  expect(within(main).getByRole('status')).toHaveTextContent('Server đang kiểm tra link');
  finish();
  expect(await within(main).findByRole('heading', { name: 'Không thể mở nội dung' })).toBeVisible();
  expect(within(main).getByText(/Link không tồn tại, đã hết hạn, đã bị thu hồi/)).toBeVisible();
  expect(screen.queryByText('synthetic-unavailable')).not.toBeInTheDocument();
  await userEvent.setup().click(within(main).getByRole('button', { name: 'Đăng nhập để kiểm tra quyền' }));
  expect(login).toHaveBeenCalledOnce();
});

test('missing share token is unavailable without performing a source request', async () => {
  let requests = 0;
  server.use(http.get('*/api/v1/sharing/resolve/*', () => {
    requests++;
    return HttpResponse.json({ code: 'ResourceUnavailable' }, { status: 404 });
  }));
  render(<SharedResourceScreen />);
  expect(await within(screen.getByRole('main')).findByRole('heading', { name: 'Không thể mở nội dung' })).toBeVisible();
  expect(requests).toBe(0);
});

test('changing a share token cannot revive a late formerly available projection', async () => {
  let finish!: () => void;
  let oldRequestStarted = false;
  let oldReplyReturned = false;
  const deferred = new Promise<void>(resolve => { finish = resolve; });
  server.use(
    http.get('*/api/v1/sharing/resolve/synthetic-old', async () => {
      oldRequestStarted = true;
      await deferred;
      oldReplyReturned = true;
      return HttpResponse.json({ resourceType: 'Project', resourceId: 'old', mode: 'PublicLink', expiresAt: null, projectionVersion: 'v1', document: null,
        project: { id: 'old', name: 'Private stale title', description: null, status: 'InProgress', startAt: '2026-10-01T00:00:00Z', endAt: '2026-10-02T00:00:00Z', priority: null, tagsJson: '[]', tasks: [] } });
    }),
    http.get('*/api/v1/sharing/resolve/synthetic-new', () => HttpResponse.json({ code: 'ResourceUnavailable', title: 'Unavailable' }, { status: 404 }))
  );
  const view = render(<SharedResourceScreen token="synthetic-old" />);
  await waitFor(() => expect(oldRequestStarted).toBe(true));
  view.rerender(<SharedResourceScreen token="synthetic-new" />);
  expect(await screen.findByRole('heading', { name: 'Không thể mở nội dung' })).toBeVisible();
  await act(async () => {
    finish();
    await waitFor(() => expect(oldReplyReturned).toBe(true));
  });
  await waitFor(() => expect(screen.queryByText('Private stale title')).not.toBeInTheDocument());
  expect(screen.getAllByRole('main')).toHaveLength(1);
});
