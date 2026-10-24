import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { expect, test, vi } from 'vitest';
import { Shell } from './App';
import { type ProfileResponse } from './api';
import { LocaleContext } from './i18n';
import { server } from './test/server';

const tag = (id: string, name: string) => ({ id, name, namespace: 'projects', color: null,
  usageCount: 0, createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"AQIDBAUGBwg="' });

function showTags() {
  server.use(http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf-token', tokenType: 'csrf', expiresInSeconds: 60 })));
  const profile: ProfileResponse = { id: 'synthetic-user', email: 'synthetic@example.invalid',
    displayName: 'Synthetic owner', timeZoneId: 'Etc/UTC', locale: 'en', state: 'Active',
    personalSpaceId: 'synthetic-owner', role: 'User', canViewAdminAccess: false,
    modules: [{ code: 'FX24', enabled: true, unavailableReason: null }] };
  render(<LocaleContext.Provider value="en"><Shell profile={profile}
    location={{ screen: 'module', moduleCode: 'FX24' }} navigate={vi.fn()}
    onLogout={async () => undefined} onProfileUpdated={vi.fn()} onThemeChanged={vi.fn()}
    onAuthLost={async () => undefined} /></LocaleContext.Provider>);
}

test('Tag pagination requests 25 rows, appends without duplicates and resets filters to a fresh page', async () => {
  const requests: URL[] = [];
  server.use(http.get('*/api/v1/documents/tags', ({ request }) => { const url = new URL(request.url); requests.push(url); return HttpResponse.json({ items: [{ ...tag('document-tag', 'Document label'), namespace: 'documents' }], nextCursor: null }); }), http.get('*/api/v1/organization/tags', ({ request }) => {
    const url = new URL(request.url); requests.push(url);
    if (url.searchParams.get('namespace') === 'documents')
      return HttpResponse.json({ items: [{ ...tag('document-tag', 'Document label'), namespace: 'documents' }], nextCursor: null });
    return HttpResponse.json(url.searchParams.has('cursor')
      ? { items: [tag('first', 'First label'), tag('second', 'Second label')], nextCursor: null }
      : { items: [tag('first', 'First label')], nextCursor: 'opaque-next-page' });
  }));
  showTags(); const user = userEvent.setup();
  await screen.findByRole('heading', { name: 'First label' });
  await user.click(screen.getByRole('button', { name: 'Tải thêm tags' }));
  await screen.findByRole('heading', { name: 'Second label' });
  expect(screen.getAllByRole('heading', { name: 'First label' })).toHaveLength(1);
  expect(requests[0].searchParams.get('limit')).toBe('25');
  expect(requests[1].searchParams.get('cursor')).toBe('opaque-next-page');
  await user.selectOptions(screen.getByLabelText('Namespace hiển thị'), 'documents');
  await screen.findByRole('heading', { name: 'Document label' });
  expect(screen.queryByRole('heading', { name: 'First label' })).not.toBeInTheDocument();
  expect(requests.at(-1)!.searchParams.has('cursor')).toBe(false);
});

test('A denied continuation clears private Tags and shows a failed fetch instead of an empty catalog', async () => {
  server.use(http.get('*/api/v1/organization/tags', ({ request }) => new URL(request.url).searchParams.has('cursor')
    ? HttpResponse.json({ title: 'Permission removed', code: 'PermissionDenied' }, { status: 403 })
    : HttpResponse.json({ items: [tag('private-tag', 'Private label')], nextCursor: 'opaque-next-page' })));
  showTags(); const user = userEvent.setup();
  await screen.findByRole('heading', { name: 'Private label' });
  await user.click(screen.getByRole('button', { name: 'Tải thêm tags' }));
  await screen.findByText('Danh sách chưa tải được. Hãy tải lại hoặc sửa bộ lọc.');
  expect(screen.queryByRole('heading', { name: 'Private label' })).not.toBeInTheDocument();
  expect(screen.queryByRole('heading', { name: 'Chưa có tag' })).not.toBeInTheDocument();
});

test('Conflict recovery fetches the exact later-page Tag and preserves the edited draft', async () => {
  const writes: { etag: string | null; name: string }[] = [];
  let detailReads = 0;
  server.use(
    http.get('*/api/v1/organization/tags', ({ request }) => HttpResponse.json(new URL(request.url).searchParams.has('cursor')
      ? { items: [tag('later-tag', 'Later page label')], nextCursor: null }
      : { items: [], nextCursor: 'opaque-next-page' })),
    http.get('*/api/v1/organization/tags/later-tag', () => {
      detailReads++; return HttpResponse.json({ ...tag('later-tag', 'Other session title'), etag: '"AgIDBAUGBwg="' });
    }),
    http.put('*/api/v1/organization/tags/later-tag', async ({ request }) => {
      const body = await request.json() as { name: string };
      writes.push({ etag: request.headers.get('If-Match'), name: body.name });
      return writes.length === 1 ? HttpResponse.json({ title: 'Tag changed', code: 'RevisionConflict' }, { status: 412 })
        : HttpResponse.json({ ...tag('later-tag', body.name), etag: '"AwIDBAUGBwg="' });
    })
  );
  showTags(); const user = userEvent.setup();
  await user.click(await screen.findByRole('button', { name: 'Tải thêm tags' }));
  await screen.findByRole('heading', { name: 'Later page label' });
  await user.click(screen.getByRole('button', { name: 'Đổi tên' }));
  const input = screen.getByLabelText('Tên tag');
  await user.clear(input); await user.type(input, 'My preserved draft');
  await user.click(screen.getByRole('button', { name: 'Lưu tag' }));
  await user.click(await screen.findByRole('button', { name: 'Dùng revision mới' }));
  await waitFor(() => expect(detailReads).toBe(1));
  expect(input).toHaveValue('My preserved draft');
  await user.click(screen.getByRole('button', { name: 'Lưu tag' }));
  await waitFor(() => expect(writes).toHaveLength(2));
  expect(writes[1]).toEqual({ etag: '"AgIDBAUGBwg="', name: 'My preserved draft' });
});
