import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { test, expect, vi } from 'vitest';
import {
  RegisterScreen,
  SecurityScreen,
  dateTime,
  isoToLocalDate,
  isoToLocalInput,
  localDateToIso,
  localInputToIso
} from './App';
import { registerUser } from './api';
import { LocaleContext } from './i18n';
import { server } from './test/server';

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
