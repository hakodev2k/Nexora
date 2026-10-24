import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { expect, it, vi } from 'vitest';
import { App, Shell, type LocationState } from './App';
import { type ProfileResponse } from './api';
import { LocaleContext } from './i18n';
import { server } from './test/server';
const profile: ProfileResponse = { id: 'synthetic-user', email: 'keyboard@example.invalid', displayName: 'Keyboard', timeZoneId: 'UTC', locale: 'en', state: 'Active', personalSpaceId: 'synthetic-owner', role: 'User', modules: [] };
function view(location: LocationState) { return <LocaleContext.Provider value="en"><Shell profile={profile} location={location} navigate={vi.fn()} onLogout={async () => undefined} onProfileUpdated={vi.fn()} onThemeChanged={vi.fn()} onAuthLost={async () => undefined} /></LocaleContext.Provider>; }
it('offers the first keyboard shortcut and moves focus after changing a route', async () => {
  server.use(http.get('*/api/v1/me/sessions', () => HttpResponse.json({ items: [] })));
  const user = userEvent.setup(); const rendered = render(view({ screen: 'module', moduleCode: 'FX99' })); await user.tab();
  expect(screen.getByRole('link', { name: 'Skip to content' })).toHaveFocus();
  await user.click(screen.getByRole('button', { name: 'Open menu' }));
  expect(screen.getByRole('button', { name: 'Close menu' })).toHaveAttribute('aria-expanded', 'true');
  rendered.rerender(view({ screen: 'security' })); expect(screen.getByRole('main')).toHaveFocus();
  expect(screen.getByRole('button', { name: 'Open menu' })).toHaveAttribute('aria-expanded', 'false');
});
it('public navigation has a focusable content destination', async () => {
  window.history.replaceState({}, '', '/login');
  server.use(http.get('*/api/v1/auth/csrf', () => HttpResponse.json({ requestToken: 'synthetic-csrf', tokenType: 'csrf', expiresInSeconds: 1800 })), http.get('*/api/v1/me', () => HttpResponse.json({ code: 'Unauthorized' }, { status: 401 })));
  const user = userEvent.setup(); render(<App />); await screen.findByRole('heading', { name: 'Đăng nhập' });
  expect(screen.getByRole('link', { name: 'Bỏ qua điều hướng, tới nội dung' })).toBeInTheDocument();
  await user.click(screen.getByRole('button', { name: 'Tạo tài khoản' }));
  await waitFor(() => expect(screen.getByRole('main')).toHaveFocus());
  window.history.replaceState({}, '', '/');
});
it('shows existing module entries with disabled modules unavailable', () => {
  const modules = ['FX10', 'FX35', 'FX37', 'FX38', 'FX39'].map(code => ({ code, enabled: code !== 'FX35', unavailableReason: code === 'FX35' ? 'PO_PAUSED' : null }));
  render(<LocaleContext.Provider value="en"><Shell profile={{ ...profile, modules }} location={{ screen: 'module', moduleCode: 'FX99' }} navigate={vi.fn()} onLogout={async () => undefined} onProfileUpdated={vi.fn()} onThemeChanged={vi.fn()} onAuthLost={async () => undefined} /></LocaleContext.Provider>);
  for (const code of ['FX10', 'FX37', 'FX38', 'FX39']) expect(screen.getByRole('button', { name: new RegExp(code) })).toBeEnabled();
  expect(screen.getByRole('button', { name: /FX35/ })).toBeDisabled();
});
