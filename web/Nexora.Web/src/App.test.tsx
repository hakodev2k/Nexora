import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterEach, test, expect, vi } from 'vitest';
import {
  App,
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
import { server } from './test/server';

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
  const onConfirm = vi.fn();

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
  expect(screen.getByRole('button', { name: 'Move to Trash' })).toHaveFocus();
  expect(onConfirm).not.toHaveBeenCalled();

  await user.keyboard('{Escape}');
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  expect(trigger).toHaveFocus();

  await user.click(trigger);
  await user.click(screen.getByRole('button', { name: 'Hủy' }));
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument();

  await user.click(trigger);
  await user.click(screen.getByRole('button', { name: 'Move to Trash' }));
  await waitFor(() => expect(onConfirm).toHaveBeenCalledTimes(1));
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

test('an Admin with the server-confirmed read grant gets a discoverable read-only admin UI', async () => {
  const target = {
    id: 'e9ed32e4-3a28-4eb9-a5d1-1f95c3868ef1',
    email: 'target@example.invalid',
    displayName: 'Operational target',
    state: 'Active',
    emailConfirmed: true,
    role: 'User',
    personalSpaceId: '3e065c11-7401-4d89-9305-d36bca2e7466',
    personalSpaceState: 'Active',
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-02T00:00:00.000Z',
    etag: '"AQIDBAUGBwg="'
  };
  server.use(
    http.get('*/api/v1/admin/users', () => HttpResponse.json({ items: [target], nextCursor: null })),
    http.get(`*/api/v1/admin/users/${target.id}/access`, () => HttpResponse.json({
      user: target,
      actionGrants: [{ actionKey: 'access.user.read', effect: 'Allow', status: 'Resolved', updatedAt: target.updatedAt }],
      moduleGrants: [{ code: 'FX02', enabled: true, state: 'Ready', systemEnabled: true }]
    }))
  );

  renderShell(syntheticProfile({ role: 'Admin', canViewAdminAccess: true }), { screen: 'admin' });

  expect(screen.getByRole('button', { name: /Admin access$/ })).toBeInTheDocument();
  expect(await screen.findByText('Read-only access')).toBeInTheDocument();
  expect(await screen.findByRole('heading', { name: 'Operational target' })).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Disable user' })).not.toBeInTheDocument();
  expect(screen.queryByRole('button', { name: 'Save role' })).not.toBeInTheDocument();
  expect(screen.getByRole('checkbox', { name: /FX02/ })).toBeDisabled();
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

test('Files removes a trashed file from the active UI only after the confirmed server mutation succeeds', async () => {
  const user = userEvent.setup();
  const file = {
    id: '352d0d14-1ac8-4dbf-94a7-27ad8a7d1e03',
    originalName: 'Quarterly-report.txt',
    mediaType: 'text/plain',
    byteLength: 42,
    scanState: 'Clean',
    lifecycle: 'Active',
    currentRevision: 1,
    createdAt: '2026-09-01T00:00:00.000Z',
    updatedAt: '2026-09-01T00:00:00.000Z',
    etag: '"AQIDBAUGBwg="'
  };
  let active = true;
  let trashCalls = 0;
  server.use(
    http.get('*/api/v1/files', () => HttpResponse.json({ items: active ? [file] : [], nextCursor: null })),
    http.get('*/api/v1/auth/csrf', () => syntheticCsrfResponse()),
    http.post(`*/api/v1/files/${file.id}/trash`, ({ request }) => {
      trashCalls += 1;
      expect(request.headers.get('If-Match')).toBe(file.etag);
      active = false;
      return new HttpResponse(null, { status: 204 });
    })
  );

  renderShell(
    syntheticProfile({ modules: [{ code: 'FX07', enabled: true, unavailableReason: null }] }),
    { screen: 'files' }
  );

  expect(await screen.findByRole('heading', { name: 'Files & attachments' })).toBeInTheDocument();
  expect(await screen.findByText(file.originalName)).toBeInTheDocument();
  const fileCard = screen.getByText(file.originalName).closest('article');
  expect(fileCard).not.toBeNull();
  await user.click(within(fileCard as HTMLElement).getByRole('button', { name: 'Trash' }));
  await user.click(screen.getByRole('button', { name: 'Đưa vào Trash' }));

  await waitFor(() => expect(trashCalls).toBe(1));
  expect(await screen.findByRole('heading', { name: 'Chưa có file' })).toBeInTheDocument();
  expect(screen.queryByText(file.originalName)).not.toBeInTheDocument();
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

test('global Trash exposes File batches, supports restore, and retains a deliberate PURGE gate', async () => {
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
  expect(screen.getByRole('button', { name: 'Purge' })).toBeDisabled();
  await user.type(screen.getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }), 'purge');
  expect(screen.getByRole('button', { name: 'Purge' })).toBeDisabled();
  await user.clear(screen.getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }));
  await user.type(screen.getByRole('textbox', { name: 'Nhập PURGE để xóa vĩnh viễn batch' }), 'PURGE');
  expect(screen.getByRole('button', { name: 'Purge' })).toBeEnabled();

  await user.click(screen.getByRole('button', { name: 'Restore batch' }));
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
  expect(screen.getByRole('button', { name: 'Revoke' })).toBeInTheDocument();
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
