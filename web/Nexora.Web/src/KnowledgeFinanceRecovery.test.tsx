import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { expect, it, vi } from 'vitest';
import { Shell, type Screen } from './App';
import { LocaleContext } from './i18n';
import { server } from './test/server';
const bookmarkId = '57e37922-1633-425b-b819-3f20266ce0e2', categoryId = 'b2048918-a582-4487-8c10-63251632a74b', recordId = 'cfe37d28-dbb3-460e-b3c2-e97b59a3d12c';
const bookmark = { id: bookmarkId, url: 'https://example.invalid/private', canonicalUrl: 'https://example.invalid/private', title: 'Private Bookmark', description: 'Private description', health: 'Unknown', status: 'Active', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"old"' };
const category = { id: categoryId, title: 'Private Category', usageCount: 1, createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"category"' };
const record = { id: recordId, categoryId, categoryTitle: category.title, amount: '10.00', currencyCode: 'USD', occurredOn: '2026-10-01', note: 'Private Note', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"old"' };
function mount(screenName: Screen) {
  server.use(http.get('*/api/v1/bookmarks/capabilities', () => HttpResponse.json(Object.fromEntries(['read', 'create', 'update', 'archive', 'unarchive'].map(action => ['bookmarks.bookmark.' + action, true])))), http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 1800 })));
  return render(<LocaleContext.Provider value="en"><Shell profile={{ id: 'synthetic-user', email: 'test@example.invalid', displayName: 'Test', personalSpaceId: 'synthetic-owner', role: 'User', state: 'Active', locale: 'en', timeZoneId: 'UTC', modules: ['FX21', 'FX23', 'FX27'].map(code => ({ code, enabled: true, unavailableReason: null })) }} location={{ screen: screenName }} navigate={vi.fn()} onLogout={async () => undefined} onProfileUpdated={vi.fn()} onThemeChanged={vi.fn()} onAuthLost={async () => undefined} /></LocaleContext.Provider>);
}
it('Bookmarks seeks, deduplicates and resets the cursor when changing query', async () => {
  const requests: URL[] = [];
  server.use(http.get('*/api/v1/bookmarks', ({ request }) => { const url = new URL(request.url); requests.push(url); return HttpResponse.json({ items: url.searchParams.has('query') ? [] : url.searchParams.has('cursor') ? [bookmark, { ...bookmark, id: categoryId, title: 'Second Bookmark' }] : [bookmark], nextCursor: url.searchParams.has('cursor') || url.searchParams.has('query') ? null : 'opaque-cursor' }); }));
  const user = userEvent.setup(); mount('bookmarks'); await screen.findByRole('heading', { name: bookmark.title }); await user.click(screen.getByRole('button', { name: /^Tải thêm$/ })); await screen.findByRole('heading', { name: 'Second Bookmark' }); expect(screen.getAllByRole('heading', { name: bookmark.title })).toHaveLength(1);
  await user.type(screen.getByLabelText('Tìm theo tiêu đề, URL hoặc mô tả'), 'changed'); await user.click(screen.getByRole('button', { name: /^Áp dụng$/ })); await waitFor(() => expect(screen.queryByRole('heading', { name: bookmark.title })).toBeNull());
  expect(requests.every(url => url.searchParams.get('limit') === '25')).toBe(true); expect(requests[1].searchParams.get('cursor')).toBe('opaque-cursor'); expect(requests.at(-1)?.searchParams.has('cursor')).toBe(false);
});
it('denied Bookmark save removes private detail and draft without empty-success feedback', async () => {
  server.use(http.get('*/api/v1/bookmarks', () => HttpResponse.json({ items: [bookmark], nextCursor: null })), http.put('*/api/v1/bookmarks/:id', () => HttpResponse.json({ title: 'Permission denied', code: 'PermissionDenied' }, { status: 403 })));
  const user = userEvent.setup(); mount('bookmarks'); await user.click(await screen.findByRole('button', { name: 'Sửa' })); await user.type(screen.getByLabelText('Tiêu đề'), ' draft'); await user.click(screen.getByRole('button', { name: 'Lưu bookmark' })); await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  expect(screen.queryByText(bookmark.title)).toBeNull(); expect(screen.queryByText(bookmark.description)).toBeNull(); expect(screen.getByRole('alert')).toHaveTextContent('Permission denied'); expect(screen.queryByText('Chưa có bookmark')).toBeNull();
});
it('committed Bookmark ACK removes private payload without repeating mutation or detail reload', async () => {
  let reads = 0, writes = 0;
  server.use(http.get('*/api/v1/bookmarks', () => { reads++; return HttpResponse.json({ items: [bookmark], nextCursor: null }); }), http.put('*/api/v1/bookmarks/:id', () => { writes++; return HttpResponse.json({ id: bookmarkId, committed: true, detailAvailable: false }); }));
  const user = userEvent.setup(); mount('bookmarks'); await user.click(await screen.findByRole('button', { name: 'Sửa' })); await user.click(screen.getByRole('button', { name: 'Lưu bookmark' })); await screen.findByText(/Thao tác đã được lưu/);
  expect(writes).toBe(1); expect(reads).toBe(1); expect(screen.queryByText(bookmark.title)).toBeNull(); expect(screen.queryByRole('dialog')).toBeNull();
});
it('Finance preserves conflict draft and reviews exact-ID revision before explicitly reapplying', async () => {
  const revisions: string[] = []; let detailReads = 0;
  server.use(http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })), http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [record], summaries: [], nextCursor: null })), http.get('*/api/v1/finance/records/:id', ({ params }) => { expect(params.id).toBe(recordId); detailReads++; return HttpResponse.json({ ...record, note: 'Server Note', etag: '"new"' }); }), http.put('*/api/v1/finance/records/:id', ({ request }) => { revisions.push(request.headers.get('If-Match') ?? ''); return revisions.length === 1 ? HttpResponse.json({ code: 'RevisionConflict', title: 'Changed' }, { status: 412 }) : HttpResponse.json({ ...record, etag: '"saved"' }); }));
  const user = userEvent.setup(); mount('finance'); await screen.findByRole('heading', { name: '10.00 USD' }); const article = screen.getByRole('heading', { name: '10.00 USD' }).closest('article')!; await user.click(within(article).getByRole('button', { name: 'Sửa' })); await user.clear(screen.getByLabelText(/^Ghi chú/)); await user.type(screen.getByLabelText(/^Ghi chú/), 'My Draft'); await user.click(screen.getByRole('button', { name: 'Lưu record' }));
  await screen.findByRole('region', { name: 'Finance record conflict' }); expect(screen.getByLabelText(/^Ghi chú/)).toHaveValue('My Draft'); expect(screen.getByRole('button', { name: 'Lưu record' })).toBeDisabled(); await user.click(screen.getByRole('button', { name: 'Tải bản hiện tại' })); await screen.findByText('Server Note'); await user.click(screen.getByRole('button', { name: 'Áp dụng draft trên revision mới' })); expect(screen.getByLabelText(/^Ghi chú/)).toHaveValue('My Draft'); await user.click(screen.getByRole('button', { name: 'Lưu record' })); await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull()); expect(detailReads).toBe(2); expect(revisions).toEqual(['"old"', '"new"']);
});
it('Finance continuation denial removes prior records, amounts and summaries', async () => {
  server.use(http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })), http.get('*/api/v1/finance/records', ({ request }) => new URL(request.url).searchParams.has('cursor') ? HttpResponse.json({ code: 'PermissionDenied', title: 'Finance denied' }, { status: 403 }) : HttpResponse.json({ items: [record], summaries: [{ currencyCode: 'USD', amount: '10.00' }], nextCursor: 'finance-cursor' })));
  const user = userEvent.setup(); mount('finance'); await screen.findByRole('heading', { name: '10.00 USD' }); await user.click(screen.getByRole('button', { name: 'Load more Finance records' })); await screen.findByRole('alert'); expect(screen.queryByText(record.note)).toBeNull(); expect(screen.queryByRole('heading', { name: '10.00 USD' })).toBeNull(); expect(screen.queryByLabelText('Tổng theo currency')).toBeNull(); expect(screen.queryByText('Chưa có record')).toBeNull();
});

it('Finance freezes an uncertain command and retries its original body, revision and idempotency key', async () => {
  const commands: { key: string | null; etag: string | null; body: string }[] = [];
  server.use(
    http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })),
    http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [record], summaries: [], nextCursor: null })),
    http.put('*/api/v1/finance/records/:id', async ({ request }) => {
      commands.push({ key: request.headers.get('Idempotency-Key'), etag: request.headers.get('If-Match'), body: await request.text() });
      return commands.length === 1 ? HttpResponse.json({ code: 'Unavailable', title: 'Synthetic uncertain outcome' }, { status: 503 }) : HttpResponse.json({ id: recordId, committed: true, detailAvailable: false });
    })
  );
  const user = userEvent.setup(); mount('finance'); await screen.findByRole('heading', { name: '10.00 USD' });
  const article = screen.getByRole('heading', { name: '10.00 USD' }).closest('article')!;
  await user.click(within(article).getByRole('button', { name: 'Sửa' }));
  await user.clear(screen.getByLabelText(/^Ghi chú/)); await user.type(screen.getByLabelText(/^Ghi chú/), 'Original exact intent');
  await user.click(screen.getByRole('button', { name: 'Lưu record' }));
  await screen.findByRole('button', { name: 'Thử lại thao tác đang chờ' });
  expect(screen.getByLabelText(/^Ghi chú/)).toBeDisabled(); expect(screen.getByRole('button', { name: /^Hủy$/ })).toBeDisabled();
  await user.keyboard('{Escape}'); expect(screen.getByRole('dialog')).toBeVisible();
  await user.click(screen.getByRole('button', { name: 'Thử lại thao tác đang chờ' }));
  await screen.findByText(/Thao tác đã được lưu/); expect(commands).toHaveLength(2); expect(commands[0].key).toBeTruthy(); expect(commands[1]).toEqual(commands[0]); expect(screen.queryByRole('dialog')).toBeNull();
});

it('Finance explicit uncertain-outcome abandonment restores the interactive background after nested confirmation', async () => {
  server.use(http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })), http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [record], summaries: [], nextCursor: null })), http.put('*/api/v1/finance/records/:id', () => HttpResponse.json({ code: 'Unavailable', title: 'Synthetic uncertain outcome' }, { status: 503 })));
  const user = userEvent.setup(); const rendered = mount('finance');
  await screen.findByRole('heading', { name: '10.00 USD' });
  await user.click(within(screen.getByRole('heading', { name: '10.00 USD' }).closest('article')!).getByRole('button', { name: 'Sửa' }));
  await user.click(screen.getByRole('button', { name: 'Lưu record' }));
  await user.click(await screen.findByRole('button', { name: 'Dừng đối chiếu' }));
  const confirmation = await screen.findByRole('dialog', { name: 'Dừng đối chiếu thao tác Finance?' });
  await user.click(within(confirmation).getByRole('button', { name: 'Dừng đối chiếu' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  expect(rendered.container).not.toHaveAttribute('inert'); expect(rendered.container).not.toHaveAttribute('aria-hidden');
  expect(screen.getByRole('button', { name: 'Tải lại' })).toBeEnabled();
});

it.each(['ModuleUnavailable', 'DependencyUnavailable'])('Finance clears private editor and values when mutation detects %s with HTTP409', async code => {
  server.use(http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })), http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [record], summaries: [{ currencyCode: 'USD', amount: '10.00' }], nextCursor: null })), http.put('*/api/v1/finance/records/:id', () => HttpResponse.json({ code, title: 'Finance no longer available' }, { status: 409 })));
  const user = userEvent.setup(); mount('finance'); await screen.findByRole('heading', { name: '10.00 USD' });
  await user.click(within(screen.getByRole('heading', { name: '10.00 USD' }).closest('article')!).getByRole('button', { name: 'Sửa' }));
  await user.type(screen.getByLabelText(/^Ghi chú/), ' protected draft'); await user.click(screen.getByRole('button', { name: 'Lưu record' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  expect(screen.queryByText(record.note)).toBeNull(); expect(screen.queryByRole('heading', { name: '10.00 USD' })).toBeNull(); expect(screen.queryByLabelText('Tổng theo currency')).toBeNull(); expect(screen.queryByText(category.title)).toBeNull();
  expect(screen.getByRole('alert')).toHaveTextContent('Finance no longer available');
});
it('Finance exact-ID resource revocation clears the conflicting draft and prior values', async () => {
  server.use(http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })), http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [record], summaries: [], nextCursor: null })), http.put('*/api/v1/finance/records/:id', () => HttpResponse.json({ code: 'RevisionConflict', title: 'Changed' }, { status: 412 })), http.get('*/api/v1/finance/records/:id', () => HttpResponse.json({ code: 'ResourceUnavailable', title: 'Resource no longer available' }, { status: 404 })));
  const user = userEvent.setup(); mount('finance'); await screen.findByRole('heading', { name: '10.00 USD' });
  await user.click(within(screen.getByRole('heading', { name: '10.00 USD' }).closest('article')!).getByRole('button', { name: 'Sửa' }));
  await user.click(screen.getByRole('button', { name: 'Lưu record' })); await user.click(await screen.findByRole('button', { name: 'Tải bản hiện tại' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull()); expect(screen.queryByText(record.note)).toBeNull(); expect(screen.queryByRole('heading', { name: '10.00 USD' })).toBeNull(); expect(screen.queryByRole('region', { name: 'Finance record conflict' })).toBeNull(); expect(screen.getByRole('alert')).toHaveTextContent('Resource no longer available');
});
