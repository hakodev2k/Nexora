import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { expect, it, vi } from 'vitest';
import { Shell, SharedResourceScreen, type Screen } from './App';
import { DashboardLayoutEditor } from './DashboardLayoutEditor';
import { DashboardQuickCreate } from './DashboardQuickCreate';
import { type DashboardLayout, type ProfileResponse } from './api';
import { LocaleContext } from './i18n';
import { server } from './test/server';

const profile: ProfileResponse = {
  id: 'synthetic-ui-user', personalSpaceId: 'synthetic-ui-owner',
  email: 'long-synthetic-display-name-for-responsive-review@example.invalid',
  displayName: 'Synthetic UI owner', role: 'User', state: 'Active', locale: 'vi', timeZoneId: 'UTC',
  modules: ['FX25', 'FX26', 'FX27'].map(code => ({ code, enabled: true, unavailableReason: null }))
};
const onAuthLost = async () => undefined;

it('exposes a main landmark and safe error heading for an unavailable public share', async () => {
  server.use(http.get('*/api/v1/sharing/resolve/*', () => HttpResponse.json({ code: 'ResourceUnavailable', title: 'Unavailable' }, { status: 404 })));
  render(<SharedResourceScreen token="synthetic-invalid-share" />);
  const main = screen.getByRole('main');
  expect(await within(main).findByRole('heading', { name: 'Không thể mở nội dung' })).toBeVisible();
  expect(main).toHaveTextContent('đã bị thu hồi');
});
function shell(page: Screen, currentProfile = profile) {
  return <LocaleContext.Provider value="vi"><Shell profile={currentProfile} location={{ screen: page }}
    navigate={vi.fn()} onLogout={async () => undefined} onProfileUpdated={vi.fn()}
    onThemeChanged={vi.fn()} onAuthLost={onAuthLost} /></LocaleContext.Provider>;
}
const category = { id: 'b2048918-a582-4487-8c10-63251632a74b', title: 'Synthetic category',
  usageCount: 0, etag: '"category-1"', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z' };
function financeReads() {
  server.use(
    http.get('*/api/v1/finance/categories', () => HttpResponse.json({ items: [category], nextCursor: null })),
    http.get('*/api/v1/finance/records', () => HttpResponse.json({ items: [], summaries: [], nextCursor: null }))
  );
}

it('keeps one Quick Create and one layout editor across Dashboard refresh and context changes without sibling-key collisions', async () => {
  const errors = vi.spyOn(console, 'error');
  let reads = 0;
  const snapshot = { generatedAt: '2026-10-21T00:00:00Z', timeZoneId: 'UTC', widgets: [] };
  server.use(
    http.get('*/api/v1/dashboard', () => { reads++; return HttpResponse.json(snapshot); }),
    http.get('*/api/v1/dashboard/refresh', () => { reads++; return HttpResponse.json(snapshot); }),
    http.get('*/api/v1/dashboard/layout', () => HttpResponse.json({ layout: null }))
  );
  const user = userEvent.setup();
  const view = render(shell('home'));
  await screen.findByRole('heading', { name: 'Attention' });
  await user.click(screen.getByRole('button', { name: 'Tải lại Dashboard' }));
  await waitFor(() => expect(reads).toBe(2));
  await screen.findByRole('heading', { name: 'Attention' });
  view.rerender(shell('home', { ...profile, locale: 'en', modules: [...profile.modules] }));
  await waitFor(() => expect(reads).toBe(3));
  await screen.findByRole('heading', { name: 'Attention' });
  expect(screen.getAllByRole('region', { name: 'Tạo mới' })).toHaveLength(1);
  expect(screen.getAllByRole('region', { name: 'Bố cục Dashboard' })).toHaveLength(1);
  expect(screen.getAllByRole('button', { name: 'Tạo Finance record' })).toHaveLength(1);
  expect(errors.mock.calls.filter(args => /same key|unique.*key/i.test(args.map(String).join(' ')))).toEqual([]);
});

it('reorders repeated providers by keyboard and saves their stable identities and sizes', async () => {
  const layout: DashboardLayout = {
    id: '72f46988-1c91-4d72-8c42-64c7afce292b', schemaVersion: 1, layoutVersion: 2, etag: '"layout-2"',
    widgets: [
      { id: 'widget-one', key: 'tasks-due', providerVersion: '1', configVersion: 1, widthUnits: 2, enabled: true, options: {} },
      { id: 'widget-two', key: 'tasks-due', providerVersion: '1', configVersion: 1, widthUnits: 1, enabled: false, options: {} }
    ]
  };
  let saved: unknown;
  server.use(
    http.get('*/api/v1/dashboard/layout', () => HttpResponse.json({ layout })),
    http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 1800 })),
    http.put('*/api/v1/dashboard/layout', async ({ request }) => {
      expect(request.headers.get('If-Match')).toBe(layout.etag);
      saved = await request.json();
      return HttpResponse.json({ ...layout, layoutVersion: 3, widgets: [...layout.widgets].reverse(), etag: '"layout-3"' });
    })
  );
  const onCommit = vi.fn();
  render(<DashboardLayoutEditor onLayout={vi.fn()} onCommit={onCommit} onUnavailable={vi.fn()} onAuthLost={onAuthLost} refreshToken={0} />);
  const user = userEvent.setup();
  const down = await screen.findByRole('button', { name: 'Đưa widget 1 xuống' });
  await waitFor(() => expect(down).toBeEnabled());
  down.focus(); await user.keyboard('{Enter}');
  await user.click(screen.getByRole('button', { name: 'Lưu bố cục' }));
  await waitFor(() => expect(onCommit).toHaveBeenCalledWith(true));
  expect(saved).toEqual({ schemaVersion: 1, widgets: [...layout.widgets].reverse() });
  expect(screen.getAllByRole('listitem')).toHaveLength(2);
});

it('renders one category dialog title and preserves its dirty-cancel guard and focus return', async () => {
  financeReads(); const user = userEvent.setup(); render(shell('finance'));
  await screen.findByRole('heading', { name: category.title });
  const trigger = screen.getByRole('button', { name: 'Thêm category' });
  await user.click(trigger);
  const dialog = screen.getByRole('dialog', { name: 'Category mới' });
  expect(within(dialog).getAllByRole('heading', { name: 'Category mới' })).toHaveLength(1);
  expect(within(dialog).queryByRole('button', { name: 'Làm mới' })).toBeNull();
  expect(within(dialog).getByLabelText('Tên category')).toHaveFocus();
  await user.type(within(dialog).getByLabelText('Tên category'), 'Unsaved category');
  await user.click(within(dialog).getByRole('button', { name: /^Hủy$/ }));
  expect(within(dialog).getByText('Bỏ thay đổi chưa lưu?')).toBeVisible();
  await user.click(within(dialog).getByRole('button', { name: 'Tiếp tục sửa' }));
  expect(within(dialog).getByLabelText('Tên category')).toHaveValue('Unsaved category');
  await user.keyboard('{Escape}');
  await user.click(within(dialog).getByRole('button', { name: 'Bỏ bản nháp' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).toBeNull());
  await waitFor(() => expect(trigger).toHaveFocus());
});

it('renders one record title and retains explicit currency validation and the draft after Escape', async () => {
  financeReads(); let writes = 0;
  server.use(http.post('*/api/v1/finance/records', () => { writes++; return HttpResponse.json({}, { status: 201 }); }));
  const user = userEvent.setup(); render(shell('finance'));
  await screen.findByRole('heading', { name: category.title });
  await user.click(screen.getByRole('button', { name: 'Thêm record' }));
  const dialog = screen.getByRole('dialog', { name: 'Record mới' });
  expect(within(dialog).getAllByRole('heading', { name: 'Record mới' })).toHaveLength(1);
  expect(within(dialog).queryByRole('button', { name: 'Làm mới' })).toBeNull();
  expect(within(dialog).getByLabelText('Currency')).toHaveValue('');
  await user.type(within(dialog).getByLabelText('Amount'), '12.50');
  await user.click(within(dialog).getByRole('button', { name: 'Tạo record' }));
  expect(await within(dialog).findByRole('alert')).toBeVisible();
  expect(writes).toBe(0);
  await user.keyboard('{Escape}');
  await user.click(within(dialog).getByRole('button', { name: 'Tiếp tục sửa' }));
  expect(within(dialog).getByLabelText('Amount')).toHaveValue('12.50');
  expect(within(dialog).getByLabelText('Currency')).toHaveValue('');
});

it('Quick Create only opens an enabled source after the server approves the exact handoff', async () => {
  const onOpen = vi.fn(), onDenied = vi.fn();
  server.use(http.get('*/api/v1/dashboard/quick-create/FinanceRecord', () => HttpResponse.json({ title: 'Unavailable', code: 'ModuleUnavailable' }, { status: 403 })));
  const user = userEvent.setup();
  render(<DashboardQuickCreate enabledModules={['FX27']} onOpen={onOpen} onDenied={onDenied} onAuthLost={onAuthLost} />);
  expect(screen.queryByRole('button', { name: 'Tạo Task' })).toBeNull();
  await user.click(screen.getByRole('button', { name: 'Tạo Finance record' }));
  await waitFor(() => expect(onDenied).toHaveBeenCalledOnce());
  expect(onOpen).not.toHaveBeenCalled();
  expect(screen.getByRole('alert')).toBeVisible();
});
