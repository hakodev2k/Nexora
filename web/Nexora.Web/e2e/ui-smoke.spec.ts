import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
// Real React/browser rendering with controlled HTTP fixtures; no SQL/API acceptance claim.
const bookmarkId = '57e37922-1633-425b-b819-3f20266ce0e2';
const bookmark = { id: bookmarkId, url: 'https://example.invalid/synthetic', canonicalUrl: 'https://example.invalid/synthetic', title: 'Synthetic Bookmark', description: 'Synthetic private draft boundary', health: 'Unknown', status: 'Active', createdAt: '2026-10-01T00:00:00Z', updatedAt: '2026-10-01T00:00:00Z', etag: '"old"' };
const modules = ['FX04', 'FX05', 'FX07', 'FX10', 'FX11', 'FX12', 'FX13', 'FX14', 'FX15', 'FX16', 'FX17', 'FX18', 'FX19', 'FX20', 'FX21', 'FX22', 'FX23', 'FX24', 'FX25', 'FX27', 'FX29', 'FX31', 'FX32', 'FX35', 'FX36', 'FX37', 'FX38', 'FX39', 'FX40'];
async function fixtures(page: Page, authenticated = false) {
  await page.route('**/api/v1/**', route => {
    const path = new URL(route.request().url()).pathname;
    const payload = path === '/api/v1/auth/csrf' ? { requestToken: 'synthetic-ui-csrf', tokenType: 'csrf', expiresInSeconds: 1800 }
      : path === '/api/v1/me' ? { id: 'synthetic-user', email: 'ui@example.invalid', displayName: 'UI smoke', personalSpaceId: 'synthetic-owner', role: 'User', locale: 'vi', timeZoneId: 'UTC', state: 'Active', modules: modules.map(code => ({ code, enabled: code !== 'FX35', unavailableReason: code === 'FX35' ? 'PO_PAUSED' : null })) }
      : path === '/api/v1/dashboard' ? { timeZoneId: 'UTC', generatedAt: '2026-10-01T00:00:00Z', widgets: [] }
      : path === '/api/v1/finance/records' ? { items: [], summaries: [], nextCursor: null }
      : path === '/api/v1/bookmarks' ? { items: [bookmark], nextCursor: null }
      : path === '/api/v1/planner' ? { from: '2026-10-01', to: '2026-10-07', pins: [], etag: '"synthetic"' }
      : path === '/api/v1/notifications' ? { items: [], unreadCount: 0, nextCursor: null }
      : path === '/api/v1/bookmarks/capabilities' ? Object.fromEntries(['read', 'create', 'update', 'archive', 'unarchive'].map(action => ['bookmarks.bookmark.' + action, true]))
      : path.endsWith('/capabilities') ? {} : { items: [], nextCursor: null };
    return route.fulfill({ status: path === '/api/v1/me' && !authenticated ? 401 : 200, contentType: 'application/json', body: JSON.stringify(path === '/api/v1/me' && !authenticated ? { code: 'Unauthorized' } : payload), headers: path === '/api/v1/me' ? { ETag: '"synthetic-profile"' } : {} });
  });
}
async function reflow(page: Page) { expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), `Whole-page overflow at ${new URL(page.url()).pathname}`).toBe(true); }
test('public responsive forms, keyboard shortcut, validation and API unavailable state', async ({ page }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message)); await fixtures(page);
  for (const path of ['/login', '/register', '/password/forgot', '/password/reset', '/verify-email']) {
    await page.goto(path); await expect(page.locator('main h1')).toBeVisible(); await reflow(page);
    const skip = page.getByRole('link', { name: 'Bỏ qua điều hướng, tới nội dung' }); await skip.focus(); await expect(skip).toBeVisible(); await page.keyboard.press('Enter'); await expect(page.getByRole('main')).toBeFocused(); expect(new URL(page.url()).hash).toBe('');
  }
  await page.goto('/register'); expect(await page.locator('.skip-link').evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThan(0); await page.getByLabel('Email', { exact: true }).fill('invalid'); await page.getByRole('button', { name: 'Tạo tài khoản', exact: true }).click();
  await expect(page.getByRole('alert')).toBeVisible(); await expect(page.getByLabel('Email', { exact: true })).toHaveValue('invalid'); await reflow(page);
  expect((await new AxeBuilder({ page }).include('main').analyze()).violations).toEqual([]); await page.screenshot({ path: info.outputPath('public-validation.png'), fullPage: true });
  await page.route('**/api/v1/me', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Synthetic persistence unavailable', status: 503 }) }));
  await page.goto('/login'); await expect(page.getByRole('heading', { name: 'Local API chưa sẵn sàng' })).toBeVisible(); await expect(page.getByRole('button', { name: 'Thử lại' })).toBeVisible(); await reflow(page); expect(errors).toEqual([]);
});
test('authenticated modules, mobile reflow, dirty Back, dialog focus and committed ACK', async ({ page }, info) => {
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message)); await fixtures(page, true);
  await page.goto('/bookmarks'); await expect(page.getByRole('heading', { name: 'Bookmarks', exact: true })).toBeVisible();
  if (info.project.name.includes('mobile')) await page.getByRole('button', { name: 'Mở menu' }).click();
  for (const code of ['FX10', 'FX37', 'FX38', 'FX39']) await expect(page.getByRole('button', { name: new RegExp(code) })).toBeVisible(); await expect(page.getByRole('button', { name: /FX35/ })).toBeDisabled();
  for (const path of ['/finance', '/organize/tags', '/read-later', '/modules/FX10', '/modules/FX20', '/modules/FX37', '/modules/FX38', '/modules/FX39', '/modules/FX40']) { await page.goto(path); await expect(page.locator('main h1')).toBeVisible(); await reflow(page); }
  await page.goto('/organize/tags'); const documentTags = page.waitForResponse(response => new URL(response.url()).pathname === '/api/v1/documents/tags'); await page.getByLabel('Namespace hiển thị').selectOption('documents'); expect((await documentTags).status()).toBe(200);
  await page.goto('/finance'); if (info.project.name.includes('mobile')) await page.getByRole('button', { name: 'Mở menu' }).click(); await page.locator('#nexora-primary-navigation').getByRole('button', { name: 'Bookmarks', exact: true }).click(); await expect(page).toHaveURL(/\/bookmarks$/); await page.getByRole('button', { name: 'Thêm bookmark' }).click(); const dialog = page.getByRole('dialog'); await expect(dialog).toBeVisible();
  await page.getByLabel('URL', { exact: true }).fill('https://example.invalid/new'); await page.getByLabel('Tiêu đề', { exact: true }).fill('Synthetic unsaved draft'); await reflow(page); expect((await new AxeBuilder({ page }).include('[role="dialog"]').analyze()).violations).toEqual([]);
  await page.goBack(); await expect(page.getByText('Bỏ thay đổi chưa lưu?')).toBeVisible(); await expect(page).toHaveURL(/\/bookmarks$/); await page.getByRole('button', { name: 'Tiếp tục sửa' }).click(); await expect(page.getByLabel('Tiêu đề', { exact: true })).toHaveValue('Synthetic unsaved draft');
  await page.keyboard.press('Escape'); await page.getByRole('button', { name: 'Bỏ bản nháp' }).click(); await expect(dialog).toHaveCount(0); await expect(page.getByRole('button', { name: 'Thêm bookmark' })).toBeFocused();
  await page.route(`**/api/v1/bookmarks/${bookmarkId}`, route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id: bookmarkId, committed: true, detailAvailable: false }) })); await page.getByRole('button', { name: 'Sửa', exact: true }).click(); await page.getByRole('button', { name: 'Lưu bookmark' }).click();
  await expect(page.getByText(/Thao tác đã được lưu/)).toBeVisible(); await expect(page.getByText(bookmark.title, { exact: true })).toHaveCount(0); await reflow(page); expect(await page.locator('.skip-link').evaluate(element => element.getBoundingClientRect().bottom)).toBeLessThan(0); await page.screenshot({ path: info.outputPath('committed-ack.png'), fullPage: true }); let inboxReads = 0;
  await page.route('**/api/v1/notifications?**', route => { inboxReads++; return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [{ id: bookmarkId, kind: 'System', title: 'Synthetic private notification', body: 'Synthetic private notification body', sourceRef: null, createdAt: '2026-10-01T00:00:00Z', readAt: null, etag: '\"AQIDBAUGBwg=\"', deliveries: [] }], unreadCount: 1, nextCursor: null }) }); });
  await page.route(`**/api/v1/notifications/${bookmarkId}`, route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id: bookmarkId, committed: true, detailAvailable: false }) }));
  await page.goto('/notifications'); await expect(page.getByRole('heading', { name: 'Synthetic private notification', exact: true })).toBeVisible();
  const initialInboxReads = inboxReads; expect(initialInboxReads).toBeGreaterThan(0);
  await page.getByRole('button', { name: 'Đánh dấu đã đọc', exact: true }).click(); await expect(page.getByText(/Thao tác đã được lưu/)).toBeVisible();
  await expect(page.getByText('Synthetic private notification', { exact: true })).toHaveCount(0); await expect(page.getByRole('heading', { name: 'Inbox trống' })).toHaveCount(0); expect(inboxReads).toBe(initialInboxReads); await reflow(page);
  expect(errors).toEqual([]);
});
