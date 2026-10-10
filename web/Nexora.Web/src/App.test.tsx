import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterEach, test, expect, vi } from 'vitest';
import {
  App,
  SharedResourceScreen,
  ConfirmActionButton,
  RegisterScreen,
  SecurityScreen,
  Shell,
  type Screen,
  type LocationState,
  dateTime,
  isoToLocalDate,
  isoToLocalInput,
  localDateToIso,
  localInputToIso
} from './App';
import { registerUser, type ProfileResponse } from './api';
import { LocaleContext } from './i18n';

test('shared Project renders approved nullable Task details and literal ordered checklist without legacy Due', async () => {
  const token = 'synthetic-memory-only-sharing-component';
  server.use(http.get(`*/api/v1/sharing/resolve/${token}`, () => HttpResponse.json({
    resourceType: 'Project', resourceId: 'synthetic-project', mode: 'PublicLink', expiresAt: null, projectionVersion: 'v1', document: null,
    project: { id: 'synthetic-project', name: 'Approved shared projection', description: 'Approved source', status: 'InProgress', startAt: '2001-01-01T00:00:00Z', endAt: '2001-01-02T00:00:00Z', priority: 'P2', tagsJson: '[]',
      tasks: [{ id: 'synthetic-task', title: 'Approved Task', description: '<script>literal task text</script>', status: 'NotStarted', startAt: '2001-01-01T00:00:00Z', endAt: '2001-01-02T00:00:00Z', priority: null, tagsJson: '["Approved tag"]', isOverdue: true, acceptanceCriteriaJson: '["Literal <img src=x onerror=alert(1)>",{"text":"Ordered checked item","checked":true}]' }] }
  })));
  const { container } = render(<SharedResourceScreen token={token} />);
  expect(await screen.findByRole('heading', { name: 'Approved shared projection' })).toBeInTheDocument();
  expect(screen.getByRole('columnheader', { name: 'Start / End' })).toBeInTheDocument();
  expect(screen.getByRole('columnheader', { name: 'Acceptance criteria' })).toBeInTheDocument();
  expect(screen.getByText('Approved tag')).toBeInTheDocument();
  expect(screen.getByText(/☑ Ordered checked item/)).toBeInTheDocument();
  expect(screen.getByText('<script>literal task text</script>')).toBeInTheDocument();
  expect(container.querySelector('script, img')).toBeNull();
  expect(screen.queryByRole('columnheader', { name: 'Due' })).not.toBeInTheDocument();
});
import { server } from './test/server';

test('a denied Productivity reload clears the previously authorized Project editor and private draft', async () => {
  const user = userEvent.setup(); let denied = false;
  const project = { id: 'synthetic-owned-project', name: 'Previously authorized project', description: 'Private source description', status: 'NotStarted', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: 'synthetic-etag', startAt: '2026-10-01T00:00:00Z', endAt: '2026-10-02T00:00:00Z', priority: 'P3', tagsJson: '[]', notes: null };
  server.use(http.get('*/api/v1/projects', () => denied ? HttpResponse.json({ code: 'ModuleUnavailable', title: 'Unavailable' }, { status: 403 }) : HttpResponse.json({ items: [project], nextCursor: null })));
  renderShell(syntheticProfile({ modules: [{ code: 'FX11', enabled: true, unavailableReason: null }] }), { screen: 'module', moduleCode: 'FX11' });
  await user.click(await screen.findByRole('button', { name: 'Sửa' }));
  expect(screen.getByLabelText('Description')).toHaveValue(project.description);
  denied = true; await user.click(screen.getByRole('button', { name: 'Tải lại' }));
  await waitFor(() => expect(screen.getByLabelText('Description')).toHaveValue(''));
  expect(screen.getByLabelText('Title')).toHaveValue('');
  expect(screen.queryByText(project.name)).toBeNull();
  expect(screen.queryByRole('heading', { name: 'Sửa Project' })).toBeNull();
});

afterEach(() => {
  window.history.replaceState({}, '', '/');
});

function syntheticCsrfResponse() {
  return HttpResponse.json({
    requestToken: globalThis.crypto.randomUUID(),
    tokenType: 'csrf',
    expiresInSeconds: 1800
  });
}

function renderRegister(locale: 'vi' | 'en' = 'en', onRegistered = vi.fn()) {
  return {
    onRegistered,
    ...render(
      <LocaleContext.Provider value={locale}>
        <RegisterScreen navigate={vi.fn()} onRegistered={onRegistered} />
      </LocaleContext.Provider>
    )
  };
}

function syntheticProfile(overrides: Partial<ProfileResponse> = {}): ProfileResponse {
  return {
    id: '2a84968b-c0a5-4c0f-9cc0-0d7af395fc08',
    email: 'viewer@example.invalid',
    displayName: 'Synthetic viewer',
    timeZoneId: 'Etc/UTC',
    locale: 'en',
    state: 'Active',
    personalSpaceId: '1d4a7e4a-5ab9-4ba5-988d-a1bb3a30c74e',
    role: 'User',
    canViewAdminAccess: false,
    modules: [],
    ...overrides
  };
}

function renderShell(profile: ProfileResponse, location: LocationState) {
  const navigate = vi.fn();
  return {
    navigate,
    ...render(
      <LocaleContext.Provider value="en">
        <Shell
          profile={profile}
          location={location}
          navigate={navigate}
          onLogout={async () => undefined}
          onProfileUpdated={vi.fn()}
          onThemeChanged={vi.fn()}
          onAuthLost={async () => undefined}
        />
      </LocaleContext.Provider>
    )
  };
}

async function fillValidRegistration(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Email'), 'synthetic-user@example.invalid');
  await user.type(screen.getByLabelText('Password'), 'x'.repeat(15));
  await user.clear(screen.getByLabelText('IANA time zone'));
  await user.type(screen.getByLabelText('IANA time zone'), 'Asia/Ho_Chi_Minh');
}

test('registration renders English copy and exposes inline validation through accessible controls', async () => {
  const user = userEvent.setup();
  renderRegister('en');

  expect(screen.getByRole('heading', { name: 'Create account' })).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Create account' }));

  expect(screen.getByRole('alert')).toHaveTextContent('Enter a valid email address.');
  expect(screen.getByLabelText('Email')).toBeInTheDocument();
});

test('registration keeps one idempotency request while submit is pending', async () => {
  let registrationCalls = 0;
  let releaseRequest: (() => void) | undefined;
  const requestFinished = new Promise<void>((resolve) => { releaseRequest = resolve; });
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.post('*/api/v1/auth/registrations', async () => {
      registrationCalls += 1;
      await requestFinished;
      return HttpResponse.json({ status: 'Accepted', messageCode: 'RegistrationAccepted' }, { status: 202 });
    })
  );

  const user = userEvent.setup();
  const { onRegistered } = renderRegister();
  await fillValidRegistration(user);
  const submit = screen.getByRole('button', { name: 'Create account' });

  await user.click(submit);
  await waitFor(() => expect(registrationCalls).toBe(1));
  expect(submit).toBeDisabled();
  await user.click(submit);
  expect(registrationCalls).toBe(1);

  releaseRequest?.();
  await waitFor(() => expect(onRegistered).toHaveBeenCalledTimes(1));
});

test('destructive controls require an accessible in-app confirmation before acting', async () => {
  const user = userEvent.setup();
  const onConfirm = vi.fn(async () => true);

  render(
    <ConfirmActionButton
      confirmationTitle="Move file to Trash?"
      confirmationDescription="The file will no longer appear in the active list."
      confirmLabel="Move to Trash"
      onConfirm={onConfirm}
    >
      Trash
    </ConfirmActionButton>
  );

  const trigger = screen.getByRole('button', { name: 'Trash' });
  await user.click(trigger);
  expect(screen.getByRole('dialog', { name: 'Move file to Trash?' })).toBeInTheDocument();
  const cancel = screen.getByRole('button', { name: 'Hủy' });
  const confirm = screen.getByRole('button', { name: 'Move to Trash' });
  expect(cancel).toHaveFocus();
  expect(trigger.parentElement).toHaveAttribute('inert', '');
  expect(trigger.parentElement).toHaveAttribute('aria-hidden', 'true');
  expect(onConfirm).not.toHaveBeenCalled();

  await user.tab();
  expect(confirm).toHaveFocus();
  await user.tab({ shift: true });
  expect(cancel).toHaveFocus();

  await user.keyboard('{Escape}');
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(trigger.parentElement).not.toHaveAttribute('inert');
  expect(trigger.parentElement).not.toHaveAttribute('aria-hidden');
  expect(trigger).toHaveFocus();

  await user.click(trigger);
  await user.click(screen.getByRole('button', { name: 'Hủy' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

  await user.click(trigger);
  await user.click(screen.getByRole('button', { name: 'Move to Trash' }));
  await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(1));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
});

test('a failed destructive confirmation stays open, explains the failure, and returns focus safely on cancel', async () => {
  const user = userEvent.setup();
  const onConfirm = vi.fn(async () => ({ error: 'The file is still referenced by a protected record.' }));

  render(
    <ConfirmActionButton
      confirmationTitle="Move file to Trash?"
      confirmationDescription="The file will no longer appear in the active list."
      confirmLabel="Move to Trash"
      onConfirm={onConfirm}
    >
      Trash
    </ConfirmActionButton>
  );

  const trigger = screen.getByRole('button', { name: 'Trash' });
  await user.click(trigger);
  await user.click(screen.getByRole('button', { name: 'Move to Trash' }));

  await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(1));
  expect(screen.getByRole('dialog', { name: 'Move file to Trash?' })).toBeInTheDocument();
  expect(screen.getByRole('alert')).toHaveTextContent('The file is still referenced by a protected record.');

  await user.click(screen.getByRole('button', { name: 'Hủy' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(trigger).toHaveFocus();
});

const specialistRoutes: Array<[Screen, string]> = [
  ['search', 'FX25'],
  ['favorites', 'FX25'],
  ['sharing', 'FX04'],
  ['support', 'FX05'],
  ['files', 'FX07'],
  ['finance', 'FX27'],
  ['bookmarks', 'FX21'],
  ['snippets', 'FX22'],
  ['readLater', 'FX23'],
  ['tags', 'FX24'],
  ['tools', 'FX32'],
  ['goals', 'FX16'],
  ['planner', 'FX15'],
  ['habits', 'FX17']
];

test.each(specialistRoutes)('direct %s routes render the %s unavailable state without mounting a protected screen', (screenName, moduleCode) => {
  renderShell(
    syntheticProfile({
      modules: [{ code: moduleCode, enabled: false, unavailableReason: 'NotGranted' }]
    }),
    { screen: screenName }
  );

  expect(screen.getByRole('heading', { name: 'Module unavailable' })).toBeInTheDocument();
  expect(screen.getByText(moduleCode)).toBeInTheDocument();
});

test('the shell hides an unavailable admin route and provides an explicit permission state', () => {
  renderShell(syntheticProfile({ role: 'Admin', canViewAdminAccess: false }), { screen: 'admin' });

  expect(screen.getByRole('heading', { name: 'Admin access is unavailable' })).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Admin access' })).not.toBeInTheDocument();
});

test('an Admin cannot reveal a SUPER-only access route even if a stale client profile contains a capability flag', () => {
  renderShell(syntheticProfile({ role: 'Admin', canViewAdminAccess: true }), { screen: 'admin' });

  expect(screen.getByRole('heading', { name: 'Admin access is unavailable' })).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Admin access' })).not.toBeInTheDocument();
});

test('a SuperAdmin reviews a signed access preview before committing a role mutation', async () => {
  const user = userEvent.setup();
  const target = {
    id: 'ae17ba5b-43a1-47d8-b5b7-2c87c8b72ba1',
    email: 'managed@example.invalid',
    displayName: 'Managed account',
    state: 'Active',
    emailConfirmed: true,
    role: 'User',
    personalSpaceId: '6c6877f0-f90a-43e4-a0e4-fd14f2a48ffb',
    personalSpaceState: 'Active',
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-02T00:00:00.000Z',
    etag: '"AQIDBAUGBwg="'
  };
  const updated = { ...target, role: 'Admin', etag: '"AgMEBQYHCAk="' };
  let previewCalls = 0;
  let commitCalls = 0;
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.get('*/api/v1/admin/users', () => HttpResponse.json({ items: [target], nextCursor: null })),
    http.get(`*/api/v1/admin/users/${target.id}/access`, () => HttpResponse.json({
      user: target,
      actionGrants: [],
      moduleGrants: [{ moduleId: '4eb98548-0e0e-48b2-a4c5-3d607e6b08ce', code: 'FX02', enabled: true, state: 'Ready', systemEnabled: true }]
    })),
    http.post(`*/api/v1/admin/users/${target.id}/access/preview`, async ({ request }) => {
      previewCalls += 1;
      expect(await request.json()).toEqual({ kind: 'role', role: 'Admin' });
      return HttpResponse.json({ previewToken: 'signed-preview', expiresAt: '2026-09-30T00:02:00.000Z', etag: target.etag, changes: [{ field: 'role', before: 'User', after: 'Admin' }], blockers: [] });
    }),
    http.put(`*/api/v1/admin/users/${target.id}/access/role`, async ({ request }) => {
      commitCalls += 1;
      expect(request.headers.get('If-Match')).toBe(target.etag);
      expect(await request.json()).toEqual({ kind: 'role', role: 'Admin', previewToken: 'signed-preview' });
      return HttpResponse.json({ user: updated, actionGrants: [], moduleGrants: [{ moduleId: '4eb98548-0e0e-48b2-a4c5-3d607e6b08ce', code: 'FX02', enabled: true, state: 'Ready', systemEnabled: true }] });
    })
  );

  renderShell(syntheticProfile({ role: 'SuperAdmin', canViewAdminAccess: true }), { screen: 'admin' });

  await screen.findByRole('heading', { name: 'Managed account' });
  await user.selectOptions(screen.getByLabelText('Role'), 'Admin');
  await user.click(screen.getByRole('button', { name: 'Xem preview role' }));

  await waitFor(() => expect(previewCalls).toBe(1));
  const dialog = screen.getByRole('dialog', { name: 'Xem lại thay đổi role' });
  expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus();
  expect(within(dialog).getByText('User → Admin')).toBeInTheDocument();
  await user.click(within(dialog).getByRole('button', { name: 'Xác nhận commit' }));

  await waitFor(() => expect(commitCalls).toBe(1));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(screen.getByRole('heading', { name: 'Managed account' })).toBeInTheDocument();
});

test.each([409, 412])('a stale access preview (%s) keeps the review dialog open and prevents a blind retry', async (status) => {
  const user = userEvent.setup();
  const target = {
    id: '3d890f25-0f66-4e96-9d13-2ec71627b5d2', email: 'stale@example.invalid', displayName: 'Stale target', state: 'Active', emailConfirmed: true,
    role: 'User', personalSpaceId: 'e0667fcf-3f0e-4d2d-9ad9-12ad209ea7f1', personalSpaceState: 'Active',
    createdAt: '2026-01-01T00:00:00.000Z', updatedAt: '2026-01-02T00:00:00.000Z', etag: '"AQIDBAUGBwg="'
  };
  let commits = 0;
  let reads = 0;
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.get('*/api/v1/admin/users', () => HttpResponse.json({ items: [target], nextCursor: null })),
    http.get(`*/api/v1/admin/users/${target.id}/access`, () => {
      reads += 1;
      return HttpResponse.json({ user: { ...target, etag: reads > 1 ? '"CAcGBQQDAgE="' : target.etag }, actionGrants: [], moduleGrants: [] });
    }),
    http.post(`*/api/v1/admin/users/${target.id}/access/preview`, async ({ request }) => {
      expect(await request.json()).toEqual({ kind: 'permissions', changes: [{ actionKey: 'access.user.read', effect: 'Allow' }] });
      return HttpResponse.json({ previewToken: 'will-stale', expiresAt: '2026-09-30T00:02:00.000Z', etag: target.etag, changes: [{ field: 'permission:access.user.read', before: 'Unset', after: 'Allow' }], blockers: [] });
    }),
    http.put(`*/api/v1/admin/users/${target.id}/access/permissions`, () => {
      commits += 1;
      return HttpResponse.json({ code: status === 412 ? 'RevisionConflict' : 'PreviewStale', title: 'The access preview is stale.' }, { status });
    })
  );

  renderShell(syntheticProfile({ role: 'SuperAdmin', canViewAdminAccess: true }), { screen: 'admin' });
  await screen.findByRole('heading', { name: 'Stale target' });
  await user.type(screen.getByLabelText('Action key'), 'access.user.read');
  await user.click(screen.getByRole('button', { name: 'Xem preview quyền' }));
  const dialog = await screen.findByRole('dialog', { name: 'Xem lại cập nhật quyền chi tiết' });
  await user.click(within(dialog).getByRole('button', { name: 'Xác nhận commit' }));

  await waitFor(() => expect(commits).toBe(1));
  expect(screen.getByRole('dialog', { name: 'Xem lại cập nhật quyền chi tiết' })).toBeInTheDocument();
  expect(within(dialog).getByRole('alert')).toHaveTextContent('The access preview is stale.');
  expect(within(dialog).getByRole('button', { name: 'Xác nhận commit' })).toBeDisabled();
  expect(screen.getByLabelText('Action key')).toHaveValue('access.user.read');
  expect(reads).toBe(status === 412 ? 2 : 1);
});

test('a recent-auth challenge reauthenticates without losing the intended access change, then requests a fresh preview', async () => {
  const user = userEvent.setup();
  const target = {
    id: '44d9cd84-b510-43db-91d9-15a7f7355ecd', email: 'reauth@example.invalid', displayName: 'Reauth target', state: 'Active', emailConfirmed: true,
    role: 'User', personalSpaceId: 'd6c257aa-0ef4-4f41-a0e2-56b1878815c3', personalSpaceState: 'Active',
    createdAt: '2026-01-01T00:00:00.000Z', updatedAt: '2026-01-02T00:00:00.000Z', etag: '"AQIDBAUGBwg="'
  };
  let previewCalls = 0;
  let reauthCalls = 0;
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.get('*/api/v1/admin/users', () => HttpResponse.json({ items: [target], nextCursor: null })),
    http.get(`*/api/v1/admin/users/${target.id}/access`, () => HttpResponse.json({ user: target, actionGrants: [], moduleGrants: [] })),
    http.post(`*/api/v1/admin/users/${target.id}/access/preview`, () => {
      previewCalls += 1;
      return previewCalls === 1
        ? HttpResponse.json({ code: 'RecentAuthenticationRequired', title: 'Reauthenticate before reviewing an access change.' }, { status: 428 })
        : HttpResponse.json({ previewToken: 'fresh-after-reauth', expiresAt: '2026-09-30T00:02:00.000Z', etag: target.etag, changes: [{ field: 'role', before: 'User', after: 'Admin' }], blockers: [] });
    }),
    http.post('*/api/v1/auth/reauth', async ({ request }) => {
      reauthCalls += 1;
      expect(await request.json()).toEqual({ password: 'current-password' });
      return new HttpResponse(null, { status: 204 });
    })
  );

  renderShell(syntheticProfile({ role: 'SuperAdmin', canViewAdminAccess: true }), { screen: 'admin' });
  await screen.findByRole('heading', { name: 'Reauth target' });
  await user.selectOptions(screen.getByLabelText('Role'), 'Admin');
  await user.click(screen.getByRole('button', { name: 'Xem preview role' }));

  const reauthDialog = await screen.findByRole('dialog', { name: 'Xác minh lại danh tính' });
  await user.type(within(reauthDialog).getByLabelText('Mật khẩu hiện tại'), 'current-password');
  await user.click(within(reauthDialog).getByRole('button', { name: 'Xác minh và tiếp tục' }));

  await waitFor(() => expect(reauthCalls).toBe(1));
  expect(await screen.findByRole('dialog', { name: 'Xem lại thay đổi role' })).toBeInTheDocument();
  expect(previewCalls).toBe(2);
});

test('module policy exposes dependency context and commits only after a policy preview', async () => {
  const user = userEvent.setup();
  const module = {
    id: '8bb0f094-6c1a-47ea-bf22-24e5f53d52b8', code: 'FX99', name: 'Policy test module', state: 'Ready', systemEnabled: true,
    registrationEnabled: true, sharingEnabled: true, policyRevision: '4', etag: '"BAAAAAAAAAA="', requiredDependencies: ['FX01'], requiredBy: ['FX100'], unavailableReason: null
  };
  let commitCalls = 0;
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.get('*/api/v1/admin/modules/', () => HttpResponse.json({ items: [module], nextCursor: null })),
    http.post(`*/api/v1/admin/modules/${module.id}/preview`, async ({ request }) => {
      expect(await request.json()).toEqual({ systemEnabled: false });
      return HttpResponse.json({ previewToken: 'module-preview', expiresAt: '2026-09-30T00:02:00.000Z', etag: module.etag, changes: [{ field: 'systemEnabled', before: 'True', after: 'False' }], blockers: [] });
    }),
    http.put(`*/api/v1/admin/modules/${module.id}/policy`, async ({ request }) => {
      commitCalls += 1;
      expect(request.headers.get('If-Match')).toBe(module.etag);
      expect(await request.json()).toEqual({ systemEnabled: false, previewToken: 'module-preview' });
      return HttpResponse.json({ ...module, systemEnabled: false, policyRevision: '5', etag: '"BQAAAAAAAAA="' });
    })
  );

  renderShell(syntheticProfile({ role: 'SuperAdmin', canViewModuleCatalog: true, canManageModulePolicy: true }), { screen: 'adminModules' });
  await screen.findByRole('heading', { name: 'Module catalog' });
  expect(screen.getByText('FX01')).toBeInTheDocument();
  expect(screen.getByText('FX100')).toBeInTheDocument();
  await user.click(screen.getByRole('checkbox', { name: 'System enabled' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Xem lại thay đổi FX99' }));

  const dialog = await screen.findByRole('dialog', { name: 'Xem lại policy FX99' });
  expect(within(dialog).getByText('True → False')).toBeInTheDocument();
  await user.click(within(dialog).getByRole('button', { name: 'Xác nhận policy' }));
  await waitFor(() => expect(commitCalls).toBe(1));
  expect(screen.getByText(/System disabled/)).toBeInTheDocument();
});

test('mobile navigation exposes state through an accessible toggle', async () => {
  const user = userEvent.setup();
  const { navigate } = renderShell(syntheticProfile(), { screen: 'module', moduleCode: 'FX99' });

  const toggle = screen.getByRole('button', { name: 'Open menu' });
  const navigation = screen.getByRole('navigation', { name: 'Primary navigation' });
  expect(toggle).toHaveAttribute('aria-expanded', 'false');
  expect(navigation).not.toHaveClass('mobile-open');

  await user.click(toggle);
  expect(toggle).toHaveAttribute('aria-expanded', 'true');
  expect(navigation).toHaveClass('mobile-open');

  await user.click(screen.getByRole('button', { name: 'Home' }));
  expect(navigate).toHaveBeenCalledWith('home');
  expect(toggle).toHaveAttribute('aria-expanded', 'false');
  expect(navigation).not.toHaveClass('mobile-open');
});

test('the profile dirty-state dialog traps focus, defaults to keep editing, and restores focus on Escape', async () => {
  const user = userEvent.setup();
  window.history.replaceState({}, '', '/settings/profile');
  server.use(
    http.get('*/api/v1/me', () => HttpResponse.json(syntheticProfile())),
    http.get('*/api/v1/settings/preferences', () => HttpResponse.json({ items: [], nextCursor: null }))
  );
  render(<App />);

  const displayName = await screen.findByLabelText('Display name');
  await user.clear(displayName);
  await user.type(displayName, 'Edited display name');
  const home = screen.getByRole('button', { name: 'Home' });
  await user.click(home);

  const dialog = screen.getByRole('dialog', { name: 'You have unsaved changes' });
  const keepEditing = within(dialog).getByRole('button', { name: 'Keep editing' });
  expect(keepEditing).toHaveFocus();
  const appContainer = home.closest('[inert]');
  expect(appContainer).not.toBeNull();
  expect(appContainer as HTMLElement).toHaveAttribute('aria-hidden', 'true');
  expect(screen.queryByRole('heading', { name: 'Profile' })).not.toBeInTheDocument();

  await user.tab({ shift: true });
  expect(within(dialog).getByRole('button', { name: 'Save and continue' })).toHaveFocus();
  await user.keyboard('{Escape}');

  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(appContainer as HTMLElement).not.toHaveAttribute('inert');
  expect(home).toHaveFocus();
  expect(screen.getByRole('heading', { name: 'Profile' })).toBeInTheDocument();
});

test('navigation labels are concise for assistive technology and unavailable modules stay hidden', () => {
  server.use(
    http.get('*/api/v1/files', () => HttpResponse.json({ items: [], nextCursor: null }))
  );
  renderShell(
    syntheticProfile({
      modules: [
        { code: 'FX07', enabled: true, unavailableReason: null },
        { code: 'FX27', enabled: false, unavailableReason: 'NotGranted' }
      ]
    }),
    { screen: 'files' }
  );

  const files = screen.getByRole('button', { name: 'Files' });
  expect(files).toHaveAccessibleName('Files');
  expect(files).toHaveAttribute('aria-current', 'page');
  expect(screen.queryByRole('button', { name: 'Finance' })).not.toBeInTheDocument();
});

test('Files exposes the installed private subset and hides dormant lifecycle controls', async () => {
  const file = { id: crypto.randomUUID(), originalName: 'Private-contract.txt', mediaType: 'text/plain', byteLength: 42, scanState: 'Clean', lifecycle: 'Active', currentRevision: 1, createdAt: '2026-09-01T00:00:00Z', updatedAt: '2026-09-01T00:00:00Z', etag: '"AQIDBAUGBwg="' };
  server.use(http.get('*/api/v1/files', () => HttpResponse.json({ items: [file], nextCursor: null })));
  renderShell(syntheticProfile({ modules: [{ code: 'FX07', enabled: true, unavailableReason: null }] }), { screen: 'files' });
  await screen.findByText(file.originalName);
  expect(screen.getByRole('link', { name: 'Tải xuống' })).toBeInTheDocument();
  expect(within(screen.getByText(file.originalName).closest('article')!).queryByRole('button', { name: 'Trash' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Đổi tên' })).not.toBeInTheDocument();
});

test('Files binds each action capability and clears protected rows on authority loss', async () => {
  const user = userEvent.setup(); let revoked = false;
  const file = { id: crypto.randomUUID(), originalName: 'Read-only-private.txt', mediaType: 'text/plain', byteLength: 42, scanState: 'Clean', lifecycle: 'Active', currentRevision: 1, createdAt: '2026-09-01T00:00:00Z', updatedAt: '2026-09-01T00:00:00Z', etag: '"AQIDBAUGBwg="' };
  server.use(
    http.get('*/api/v1/files/capabilities', () => revoked ? HttpResponse.json({ code: 'PermissionDenied', title: 'Current permission revoked.' }, { status: 403 }) : HttpResponse.json({ 'files.file.read': true, 'files.file.upload': false, 'files.file.download': false })),
    http.get('*/api/v1/files', () => HttpResponse.json({ items: [file], nextCursor: null }))
  );
  renderShell(syntheticProfile({ modules: [{ code: 'FX07', enabled: true, unavailableReason: null }] }), { screen: 'files' });
  await screen.findByText(file.originalName);
  expect(screen.getByLabelText('Chọn file')).toBeDisabled();
  expect(screen.queryByRole('link', { name: 'Tải xuống' })).not.toBeInTheDocument();
  revoked = true; await user.click(screen.getByRole('button', { name: 'Tải lại' }));
  await screen.findByRole('alert'); expect(screen.queryByText(file.originalName)).not.toBeInTheDocument();
});

test('Files clears the native file input after upload so the same file can be selected again', async () => {
  const user = userEvent.setup();
  const upload = new File(['reusable fixture'], 'repeatable.txt', { type: 'text/plain' });
  const sessionId = 'baf89e70-a1a0-4388-a124-9e83b7866767';
  let completedUploads = 0;
  server.use(
    http.get('*/api/v1/files', () => HttpResponse.json({ items: [], nextCursor: null })),
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.post('*/api/v1/files/upload-sessions', async ({ request }) => {
      expect(await request.json()).toEqual({
        originalName: upload.name,
        mediaType: upload.type,
        expectedBytes: upload.size
      });
      return HttpResponse.json({
        id: sessionId,
        expectedBytes: upload.size,
        receivedBytes: 0,
        state: 'Initiated',
        expiresAt: '2026-09-30T00:00:00.000Z',
        uploadHandle: 'test-upload-handle',
        etag: '"AQIDBAUGBwg="'
      }, { status: 201 });
    }),
    http.put(`*/api/v1/files/upload-sessions/${sessionId}/content`, ({ request }) => {
      completedUploads += 1;
      expect(request.headers.get('X-Upload-Handle')).toBe('test-upload-handle');
      return HttpResponse.json({
        id: 'bd551960-21e4-4c7f-b28c-ab20dcdd2f63',
        originalName: upload.name,
        mediaType: upload.type,
        byteLength: upload.size,
        scanState: 'Clean',
        lifecycle: 'Active',
        currentRevision: 1,
        createdAt: '2026-09-01T00:00:00.000Z',
        updatedAt: '2026-09-01T00:00:00.000Z',
        etag: '"AQIDBAUGBwg="'
      });
    })
  );

  renderShell(
    syntheticProfile({ modules: [{ code: 'FX07', enabled: true, unavailableReason: null }] }),
    { screen: 'files' }
  );

  await screen.findByRole('heading', { name: 'Files & attachments' });
  await waitFor(() => expect(screen.getByLabelText('Chọn file')).toBeEnabled());
  const input = screen.getByLabelText('Chọn file') as HTMLInputElement;
  await user.upload(input, upload);
  expect(screen.getByText(/Đã chọn: repeatable\.txt/)).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Upload và scan' }));

  await waitFor(() => {
    expect(completedUploads).toBe(1);
    expect(input.value).toBe('');
    expect(screen.queryByText(/Đã chọn: repeatable\.txt/)).not.toBeInTheDocument();
  });

  await user.upload(input, upload);
  expect(screen.getByText(/Đã chọn: repeatable\.txt/)).toBeInTheDocument();
});

test('global Trash previews restore and permanent deletion, with a deliberate in-dialog PURGE gate', async () => {
  const user = userEvent.setup();
  const batchId = 'ab01dd92-7a90-454f-b849-486b92a4d998';
  const item = {
    id: 'd43dfc64-9290-49d4-a436-331a7c5aa11e',
    resourceType: 'File',
    resourceId: '0aa8f560-f9a2-4c11-99a3-82b85804c9bf',
    deletionBatchId: batchId,
    priorStatus: 'Active',
    deletedAt: '2026-09-01T00:00:00.000Z',
    restoredAt: null,
    purgedAt: null
  };
  let active = true;
  let restoreCalls = 0;
  server.use(
    http.get('*/api/v1/trash', () => HttpResponse.json({ items: active ? [item] : [], nextCursor: null })),
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.post(`*/api/v1/trash/batches/${batchId}/restore`, () => {
      restoreCalls += 1;
      active = false;
      return HttpResponse.json({ deletionBatchId: batchId, restoredCount: 1, remainingCount: 0 });
    })
  );

  renderShell(syntheticProfile(), { screen: 'trash' });

  expect(await screen.findByText(`File · ${item.resourceId} · trạng thái trước: Active`)).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Delete permanently' }));
  const purgeDialog = screen.getByRole('dialog', { name: 'Delete permanently?' });
  expect(purgeDialog).toBeInTheDocument();
  expect(within(purgeDialog).getByRole('button', { name: 'Cancel' })).toHaveFocus();
  expect(within(purgeDialog).getByRole('button', { name: 'Delete permanently' })).toBeDisabled();
  await user.type(within(purgeDialog).getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }), 'purge');
  expect(within(purgeDialog).getByRole('button', { name: 'Delete permanently' })).toBeDisabled();
  await user.clear(within(purgeDialog).getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }));
  await user.type(within(purgeDialog).getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }), 'PURGE');
  expect(within(purgeDialog).getByRole('button', { name: 'Delete permanently' })).toBeEnabled();
  expect(restoreCalls).toBe(0);
  await user.click(within(purgeDialog).getByRole('button', { name: 'Cancel' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

  await user.click(screen.getByRole('button', { name: 'Restore batch' }));
  const restoreDialog = screen.getByRole('dialog', { name: 'Restore these items?' });
  expect(restoreDialog).toBeInTheDocument();
  await user.click(within(restoreDialog).getByRole('button', { name: 'Restore batch' }));
  await waitFor(() => expect(restoreCalls).toBe(1));
  expect(await screen.findByRole('heading', { name: 'Trash trống' })).toBeInTheDocument();
  expect(screen.getByText('Không có resource nào đang chờ restore hoặc purge.')).toBeInTheDocument();
});

test('Notifications keeps ETags from the list projection for read state and selected-item deletion', async () => {
  const user = userEvent.setup();
  const unread = {
    id: '08fb2ed5-13a0-42ef-b775-8720e397d298',
    kind: 'System',
    title: 'Unread synthetic notice',
    body: 'A safe summary for a UI state transition.',
    sourceRef: null,
    createdAt: '2026-09-01T00:00:00.000Z',
    readAt: null as string | null,
    etag: '"AQIDBAUGBwg="',
    deliveries: [{ channel: 'InApp', state: 'Pending', attempts: 0, lastErrorCode: null, updatedAt: '2026-09-01T00:00:00.000Z' }]
  };
  const read = {
    id: '155315ba-8c2b-4b51-bc99-72ba12331a97',
    kind: 'System',
    title: 'Read synthetic notice',
    body: 'This item can be selected for deletion.',
    sourceRef: null,
    createdAt: '2026-08-31T00:00:00.000Z',
    readAt: '2026-09-01T01:00:00.000Z' as string | null,
    etag: '"CQoLDA0ODxA="',
    deliveries: [{ channel: 'Email', state: 'Pending', attempts: 0, lastErrorCode: null, updatedAt: '2026-08-31T00:00:00.000Z' }]
  };
  let items = [unread, read];
  let markReadCalls = 0;
  let deleteCalls = 0;
  server.use(
    http.get('*/api/v1/notifications', ({ request }) => {
      const unreadOnly = new URL(request.url).searchParams.get('unreadOnly') === 'true';
      const visible = unreadOnly ? items.filter((item) => item.readAt === null) : items;
      return HttpResponse.json({
        items: visible,
        nextCursor: null,
        unreadCount: items.filter((item) => item.readAt === null).length
      });
    }),
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.patch(`*/api/v1/notifications/${unread.id}`, async ({ request }) => {
      markReadCalls += 1;
      expect(request.headers.get('If-Match')).toBe(unread.etag);
      expect(await request.json()).toEqual({ read: true });
      unread.readAt = '2026-09-01T02:00:00.000Z';
      unread.etag = '"ERITFBUWFxg="';
      return HttpResponse.json(unread);
    }),
    http.post('*/api/v1/notifications/delete', async ({ request }) => {
      deleteCalls += 1;
      expect(await request.json()).toEqual({ notificationIds: [read.id] });
      items = items.filter((item) => item.id !== read.id);
      return new HttpResponse(null, { status: 204 });
    })
  );

  renderShell(syntheticProfile(), { screen: 'notifications' });

  expect(await screen.findByRole('heading', { name: 'Notifications' })).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Đánh dấu đã đọc' }));
  await waitFor(() => expect(markReadCalls).toBe(1));
  expect(screen.getAllByRole('button', { name: 'Đánh dấu chưa đọc' })).toHaveLength(2);
  expect(screen.getByText('Chỉ chưa đọc (0)')).toBeInTheDocument();

  await user.click(screen.getByRole('checkbox', { name: 'Chọn Read synthetic notice' }));
  const removeSelected = screen.getByRole('button', { name: 'Xóa mục đã chọn' });
  expect(removeSelected).toBeEnabled();
  await user.click(removeSelected);
  const deleteDialog = screen.getByRole('dialog', { name: 'Xóa 1 mục khỏi Inbox?' });
  await user.click(within(deleteDialog).getByRole('button', { name: 'Xóa mục đã chọn' }));
  await waitFor(() => expect(deleteCalls).toBe(1));
  expect(screen.queryByText(read.title)).not.toBeInTheDocument();
  expect(screen.getByText(unread.title)).toBeInTheDocument();
});

test('an authenticated share link resumes after sign-in without placing its capability token in a login URL or browser storage', async () => {
  const user = userEvent.setup();
  const token = 'synthetic-share-capability-token';
  const resourceId = '8f3712de-25d9-4150-a131-a313b4e61c91';
  let signedIn = false;
  let resolveCalls = 0;
  window.history.replaceState({}, '', `/share/${token}`);
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.get('*/api/v1/me', () => signedIn
      ? HttpResponse.json(syntheticProfile())
      : HttpResponse.json({ code: 'SessionUnavailable', title: 'Session unavailable' }, { status: 401 })),
    http.get(`*/api/v1/sharing/resolve/${token}`, () => {
      resolveCalls += 1;
      if (!signedIn) return HttpResponse.json({ code: 'ResourceUnavailable', title: 'Unavailable' }, { status: 404 });
      return HttpResponse.json({
        resourceType: 'Project',
        resourceId,
        mode: 'AuthenticatedLink',
        expiresAt: null,
        projectionVersion: 'v1',
        project: {
          id: resourceId,
          name: 'Returned shared project',
          description: 'Safe synthetic projection',
          status: 'InProgress',
          startAt: '2026-09-01T00:00:00.000Z',
          endAt: '2026-09-02T00:00:00.000Z',
          priority: 'P2',
          tagsJson: '[]',
          tasks: []
        },
        document: null
      });
    }),
    http.post('*/api/v1/auth/login', () => {
      signedIn = true;
      return HttpResponse.json({ profile: syntheticProfile(), expiresAt: '2026-10-01T00:00:00.000Z' });
    }),
    http.get('*/api/v1/settings/preferences', () => HttpResponse.json({ items: [] }))
  );

  render(<App />);

  const signIn = await screen.findByRole('button', { name: 'Đăng nhập để kiểm tra quyền' });
  expect(resolveCalls).toBe(1);
  await user.click(signIn);
  expect(window.location.pathname).toBe('/login');
  expect(window.location.search).toBe('');

  await user.type(screen.getByLabelText('Email'), 'shared-viewer@example.invalid');
  await user.type(screen.getByLabelText('Mật khẩu'), 'x'.repeat(18));
  await user.click(screen.getByRole('button', { name: 'Đăng nhập' }));

  expect(await screen.findByRole('heading', { name: 'Returned shared project' })).toBeInTheDocument();
  expect(window.location.pathname).toBe(`/share/${token}`);
  expect(window.location.search).toBe('');
  expect(resolveCalls).toBe(2);
});

test('the HTTP boundary refreshes CSRF once and retains a single idempotency key after a pre-handler rejection', async () => {
  let mutationCalls = 0;
  const observedKeys = new Set<string | null>();
  server.use(
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.post('*/api/v1/auth/registrations', ({ request }) => {
      mutationCalls += 1;
      observedKeys.add(request.headers.get('Idempotency-Key'));
      if (mutationCalls === 1) {
        return HttpResponse.json({ code: 'CsrfInvalid', title: 'Rejected before handler' }, { status: 403 });
      }

      return HttpResponse.json({ status: 'Accepted', messageCode: 'RegistrationAccepted' }, { status: 202 });
    })
  );

  await expect(registerUser(
    'synthetic-user@example.invalid',
    'x'.repeat(15),
    'Asia/Ho_Chi_Minh',
    'Synthetic user'
  )).resolves.toEqual({ status: 'Accepted', messageCode: 'RegistrationAccepted' });

  expect(mutationCalls).toBe(2);
  expect(observedKeys.size).toBe(1);
  expect(observedKeys.has(null)).toBe(false);
});

test('session metadata uses the profile IANA zone rather than the browser default', async () => {
  const user = userEvent.setup();
  const createdAt = '2026-01-01T00:00:00.000Z';
  const expectedProfileTime = dateTime(createdAt, 'Asia/Ho_Chi_Minh', 'en');
  const browserUtcTime = dateTime(createdAt, 'UTC', 'en');
  server.use(
    http.get('*/api/v1/me/sessions', () => HttpResponse.json({
      items: [{
        id: globalThis.crypto.randomUUID(),
        deviceLabel: 'Synthetic browser',
        createdAt,
        lastSeenAt: '2026-01-01T01:00:00.000Z',
        expiresAt: '2026-01-01T02:00:00.000Z',
        isCurrent: false
      }],
      nextCursor: null
    }))
  );

  render(
    <LocaleContext.Provider value="en">
      <SecurityScreen timeZoneId="Asia/Ho_Chi_Minh" onAuthLost={async () => undefined} />
    </LocaleContext.Provider>
  );

  expect(expectedProfileTime).not.toEqual(browserUtcTime);
  expect(await screen.findByText((_, element) =>
    element?.tagName === 'SPAN' && element.textContent === 'Created ' + expectedProfileTime
  )).toBeInTheDocument();
  const revoke = screen.getByRole('button', { name: 'Revoke' });
  await user.click(revoke);
  const dialog = screen.getByRole('dialog', { name: 'Revoke this session?' });
  expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus();
  expect(within(dialog).getByText(/Synthetic browser/)).toBeInTheDocument();
});

test('time helpers preserve a normal IANA instant and all-day date boundary', () => {
  const instant = localInputToIso('2026-03-01T07:30', 'Asia/Ho_Chi_Minh');

  expect(instant).toBe('2026-03-01T00:30:00.000Z');
  expect(isoToLocalInput(instant, 'Asia/Ho_Chi_Minh')).toBe('2026-03-01T07:30');
  expect(localDateToIso('2026-03-01', 'Asia/Ho_Chi_Minh')).toBe('2026-02-28T17:00:00.000Z');
  expect(isoToLocalDate('2026-02-28T17:00:00.000Z', 'Asia/Ho_Chi_Minh')).toBe('2026-03-01');
});

test.skip('R2-10: DST valid-after-gap and fold disambiguation require the approved post-M01 time policy', () => {
  expect(localInputToIso('2027-03-14T03:30', 'America/New_York')).toBe('2027-03-14T07:30:00.000Z');
});
