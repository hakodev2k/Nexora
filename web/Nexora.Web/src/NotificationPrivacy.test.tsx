import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expect, it, vi } from 'vitest';
import { Shell } from './App';
import { LocaleContext } from './i18n';
import { server } from './test/server';

const id = 'd5eecdc3-2348-4b83-83d1-7519839b335d';
const item = { id, kind: 'System', title: 'Private inbox title', body: 'Private inbox body', sourceRef: null,
  createdAt: '2026-10-01T00:00:00Z', readAt: null, etag: '"AQIDBAUGBwg="', deliveries: [] };
function mount() {
  server.use(http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 60 })));
  render(<LocaleContext.Provider value="en"><Shell profile={{ id: 'synthetic-user', email: 'test@example.invalid', displayName: 'Synthetic owner',
    personalSpaceId: 'f1cde482-2f03-461f-a962-ae8efca10a7d', role: 'User', state: 'Active', locale: 'en', timeZoneId: 'UTC',
    modules: [{ code: 'FX06', enabled: true, unavailableReason: null }] }} location={{ screen: 'notifications' }} navigate={vi.fn()}
    onLogout={async () => undefined} onProfileUpdated={vi.fn()} onThemeChanged={vi.fn()} onAuthLost={async () => undefined} /></LocaleContext.Provider>);
}

it.each(['single', 'all'])('Notification %s ACK clears private detail without an automatic read or a false empty state', async mode => {
  let reads = 0;
  server.use(http.get('*/api/v1/notifications', () => { reads++; return HttpResponse.json({ items: [item], unreadCount: 1, nextCursor: null }); }),
    http.patch('*/api/v1/notifications/:id', () => HttpResponse.json({ id, committed: true, detailAvailable: false })),
    http.post('*/api/v1/notifications/mark-all-read', () => HttpResponse.json({ id: 'f1cde482-2f03-461f-a962-ae8efca10a7d', committed: true, detailAvailable: false })));
  mount(); const user = userEvent.setup(); await screen.findByRole('heading', { name: item.title });
  await user.click(screen.getByRole('button', { name: mode === 'single' ? /^Đánh dấu đã đọc$/ : /^Đánh dấu tất cả đã đọc$/ }));
  await screen.findByText(/Thao tác đã được lưu/); expect(screen.queryByText(item.title)).toBeNull(); expect(screen.queryByText(item.body)).toBeNull();
  expect(screen.queryByRole('heading', { name: 'Inbox trống' })).toBeNull(); expect(reads).toBe(1);
});

it('Notification current module denial clears cached private rows, selections and counts', async () => {
  server.use(http.get('*/api/v1/notifications', () => HttpResponse.json({ items: [item], unreadCount: 1, nextCursor: null })),
    http.patch('*/api/v1/notifications/:id', () => HttpResponse.json({ code: 'ModuleUnavailable', title: 'Current access unavailable' }, { status: 409 })));
  mount(); const user = userEvent.setup(); await screen.findByRole('heading', { name: item.title });
  await user.click(screen.getByRole('checkbox', { name: `Chọn ${item.title}` }));
  await user.click(screen.getByRole('button', { name: /^Đánh dấu đã đọc$/ }));
  await waitFor(() => expect(screen.queryByText(item.title)).toBeNull()); expect(screen.queryByText(item.body)).toBeNull();
  expect(screen.getByRole('alert')).toHaveTextContent('Current access unavailable');
  expect(screen.getByRole('checkbox', { name: 'Chỉ chưa đọc (0)' })).not.toBeChecked();
  expect(screen.getByRole('button', { name: 'Xóa mục đã chọn' })).toBeDisabled(); expect(screen.queryByRole('heading', { name: 'Inbox trống' })).toBeNull();
});
