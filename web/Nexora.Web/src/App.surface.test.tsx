import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterEach, expect, test, vi } from 'vitest';
import { Shell, type LocationState } from './App';
import { type ProfileResponse } from './api';
import { LocaleContext } from './i18n';
import { server } from './test/server';

const enabledModuleCodes = [
  'FX04', 'FX05', 'FX07', 'FX11', 'FX12', 'FX13', 'FX14', 'FX15', 'FX16',
  'FX17', 'FX20', 'FX21', 'FX22', 'FX23', 'FX24', 'FX25', 'FX27', 'FX32'
];

function enabledProfile(overrides: Partial<ProfileResponse> = {}): ProfileResponse {
  return {
    id: 'af5ee6ab-8f1c-4014-a674-96eec3bd4e0e',
    email: 'coverage-user@example.invalid',
    displayName: 'Coverage user',
    timeZoneId: 'Etc/UTC',
    locale: 'en',
    state: 'Active',
    personalSpaceId: 'bdf7401c-75a3-4b7d-9b57-d5b9f9898ad5',
    role: 'User',
    canViewAdminAccess: false,
    modules: enabledModuleCodes.map((code) => ({ code, enabled: true, unavailableReason: null })),
    ...overrides
  };
}

function renderEnabledShell(location: LocationState, profile = enabledProfile()) {
  return render(
    <LocaleContext.Provider value="en">
      <Shell
        profile={profile}
        location={location}
        navigate={vi.fn()}
        onLogout={async () => undefined}
        onProfileUpdated={vi.fn()}
        onThemeChanged={vi.fn()}
        onAuthLost={async () => undefined}
      />
    </LocaleContext.Provider>
  );
}

function emptyReadModel({ request }: { request: Request }) {
  const url = new URL(request.url);
  switch (url.pathname) {
    case '/api/v1/dashboard':
      return HttpResponse.json({ timeZoneId: 'Etc/UTC', generatedAt: '2026-09-29T00:00:00.000Z', widgets: [] });
    case '/api/v1/search':
      return HttpResponse.json({
        query: url.searchParams.get('q') ?? '',
        resourceType: url.searchParams.get('resourceType'),
        includeArchived: url.searchParams.get('includeArchived') === 'true',
        items: [],
        providers: [],
        nextCursor: null
      });
    case '/api/v1/notifications':
      return HttpResponse.json({ items: [], nextCursor: null, unreadCount: 0 });
    case '/api/v1/finance/records':
      return HttpResponse.json({ items: [], summaries: [], nextCursor: null });
    case '/api/v1/developer/tools':
      return HttpResponse.json({ items: [] });
    case '/api/v1/planner':
      return HttpResponse.json({ from: '2026-09-29', to: '2026-10-05', pins: [], etag: '"AQIDBAUGBwg="' });
    default:
      return HttpResponse.json({ items: [], nextCursor: null });
  }
}

function visibleInteractiveControls(root: HTMLElement) {
  return Array.from(root.querySelectorAll<HTMLElement>('button, a[href], input:not([type="hidden"]), select, textarea'))
    .filter((element) => !element.closest('[hidden], [aria-hidden="true"]'));
}

function toolCatalog(code: string) {
  server.use(
    http.get('*/api/v1/developer/tools', () => HttpResponse.json({ items: [{ code, name: code, category: 'Local', description: 'Synthetic component test', actionKey: 'toolbox.' + code + '.run', executionMode: 'Local' }] })),
    http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-component-csrf', tokenType: 'csrf', expiresInSeconds: 60 }))
  );
}

test('Toolbox removes the previous success when changed options fail and retains correction input', async () => {
  const user = userEvent.setup(); toolCatalog('uuid');
  server.use(http.post('*/api/v1/developer/tools/run', async ({ request }) => {
    const body = await request.json() as { options: { count: string } };
    return body.options.count === '21'
      ? HttpResponse.json({ code: 'ValidationFailed', title: 'count must be between 1 and 20.' }, { status: 422 })
      : HttpResponse.json({ toolCode: 'uuid', output: 'synthetic-prior-success', durationMilliseconds: 1 });
  }));
  renderEnabledShell({ screen: 'module', moduleCode: 'FX32' }); await screen.findByLabelText('Số UUID (1–20)');
  await user.click(screen.getByRole('button', { name: 'Chạy tool' })); expect(await screen.findByLabelText('Tool output')).toHaveTextContent('synthetic-prior-success');
  await user.clear(screen.getByLabelText('Số UUID (1–20)')); await user.type(screen.getByLabelText('Số UUID (1–20)'), '21');
  await user.click(screen.getByRole('button', { name: 'Chạy tool' })); expect(await screen.findByRole('alert')).toHaveTextContent('count must be between 1 and 20.');
  expect(screen.queryByLabelText('Tool output')).not.toBeInTheDocument(); expect(screen.getByLabelText('Số UUID (1–20)')).toHaveValue('21');
});

test('Toolbox clear requires confirmation and cancellation preserves pasted input', async () => {
  const user = userEvent.setup(); toolCatalog('base64'); renderEnabledShell({ screen: 'module', moduleCode: 'FX32' });
  const input = await screen.findByLabelText(/Input.*tối đa 1 MiB/); await user.type(input, 'synthetic unsaved input');
  await user.click(screen.getByRole('button', { name: 'Xóa' })); expect(screen.getByRole('dialog')).toBeVisible();
  await user.click(screen.getByRole('button', { name: 'Cancel' })); expect(input).toHaveValue('synthetic unsaved input');
  await user.click(screen.getByRole('button', { name: 'Xóa' })); await user.click(screen.getByRole('button', { name: 'Xóa input/output' }));
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument()); expect(input).toHaveValue('');
});

const surfaceMatrix: Array<{ label: string; location: LocationState }> = [
  { label: 'home', location: { screen: 'home' } },
  { label: 'search', location: { screen: 'search' } },
  { label: 'favorites', location: { screen: 'favorites' } },
  { label: 'profile', location: { screen: 'profile' } },
  { label: 'security', location: { screen: 'security' } },
  { label: 'notifications', location: { screen: 'notifications' } },
  { label: 'trash', location: { screen: 'trash' } },
  { label: 'sharing', location: { screen: 'sharing' } },
  { label: 'support', location: { screen: 'support' } },
  { label: 'files', location: { screen: 'files' } },
  { label: 'projects', location: { screen: 'module', moduleCode: 'FX11' } },
  { label: 'tasks', location: { screen: 'module', moduleCode: 'FX12' } },
  { label: 'calendar', location: { screen: 'module', moduleCode: 'FX13' } },
  { label: 'documents', location: { screen: 'module', moduleCode: 'FX20' } },
  { label: 'reminders', location: { screen: 'module', moduleCode: 'FX14' } },
  { label: 'bookmarks', location: { screen: 'module', moduleCode: 'FX21' } },
  { label: 'snippets', location: { screen: 'module', moduleCode: 'FX22' } },
  { label: 'read later', location: { screen: 'module', moduleCode: 'FX23' } },
  { label: 'tags', location: { screen: 'module', moduleCode: 'FX24' } },
  { label: 'developer tools', location: { screen: 'module', moduleCode: 'FX32' } },
  { label: 'goals', location: { screen: 'module', moduleCode: 'FX16' } },
  { label: 'planner', location: { screen: 'module', moduleCode: 'FX15' } },
  { label: 'habits', location: { screen: 'module', moduleCode: 'FX17' } },
  { label: 'finance', location: { screen: 'module', moduleCode: 'FX27' } }
];

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

test.each(surfaceMatrix)('the enabled $label surface has a settled empty state and named interactive controls', async ({ label, location }) => {
  server.use(http.get(/\/api\/v1\/.*/, emptyReadModel));
  const { container } = renderEnabledShell(location);

  expect(await screen.findByRole('heading', { level: 1 })).toBeInTheDocument();
  await waitFor(() => expect(container.querySelector('.loading-state')).not.toBeInTheDocument());
  expect(screen.queryByRole('alert')).not.toBeInTheDocument();

  const controls = visibleInteractiveControls(container);
  expect(controls.length, `${label} should expose at least one interactive control`).toBeGreaterThan(0);
  for (const control of controls) {
    expect(control, `${label} control ${control.outerHTML}`).toHaveAccessibleName();
  }
});

test('Files keeps a functional form visible alongside its API error state', async () => {
  server.use(
    http.get('*/api/v1/files', () => HttpResponse.json({ code: 'StorageUnavailable', title: 'Storage is temporarily unavailable.' }, { status: 503 }))
  );

  renderEnabledShell({ screen: 'files' });

  expect(await screen.findByRole('alert')).toHaveTextContent('Storage is temporarily unavailable.');
  expect(screen.getByLabelText('Chọn file')).toBeEnabled();
  expect(screen.getByRole('button', { name: 'Upload và scan' })).toBeEnabled();
});

test('Files exposes a clear loading state before its empty state resolves', async () => {
  let release: (() => void) | undefined;
  const pending = new Promise<void>((resolve) => { release = resolve; });
  server.use(
    http.get('*/api/v1/files', async () => {
      await pending;
      return HttpResponse.json({ items: [], nextCursor: null });
    })
  );

  renderEnabledShell({ screen: 'files' });

  expect(await screen.findByRole('status')).toHaveTextContent('Đang tải file…');
  release?.();
  expect(await screen.findByRole('heading', { name: 'Chưa có file' })).toBeInTheDocument();
});

test('a revoked module entry reports unavailability instead of claiming a grant', async () => {
  renderEnabledShell({ screen: 'module', moduleCode: 'FX11' }, enabledProfile({
    modules: [{ code: 'FX11', enabled: false, unavailableReason: 'UserGrantDisabled' }]
  }));
  expect(await screen.findByRole('heading', { name: 'Module unavailable' })).toBeVisible();
  expect(screen.queryByText(/Server đã cấp module này/)).not.toBeInTheDocument();
});
