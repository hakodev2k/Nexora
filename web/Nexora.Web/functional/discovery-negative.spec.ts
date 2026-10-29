import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test.use({ actionTimeout: 15000 });
async function openPalette(page: import('@playwright/test').Page) {
  await expect(page.getByRole('button', { name: 'Command Palette · Ctrl/Cmd+K', exact: true })).toBeVisible();
  // Native header click proves a mounted shell; unlike an early shortcut, it has
  // Playwright actionability readiness and cannot be lost before effect install.
  await page.getByRole('button', { name: 'Command Palette · Ctrl/Cmd+K', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Command Palette', exact: true })).toBeVisible();
}
// FX25 BR003/004/005/006, AC001/003; DISC4 authority and IME controls.
// Exclusive writer only: genuine SuperAdmin module-grant preview/commit, finally restored.
test('E25 negative: revoke listed source and saved-query access clears private projections under current authority', async ({ page, browser }) => {
  test.setTimeout(180000); await page.goto('/');
  const me = await api(page, 'getMe');
  const operatorContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const operator = await operatorContext.newPage(); let before: any, scenarioError: unknown;
  const codes = ['FX21', 'FX25'];
  const access = () => api(operator, 'getAdminUserAccess', [me.id]);
  async function setGrant(code: string, enabled: boolean) {
    const current = await access(), grant = current.moduleGrants.find((g: any) => g.code === code);
    expect(grant).toBeTruthy(); if (grant.enabled === enabled) return;
    const changes = [{ moduleId: grant.moduleId, enabled }];
    const preview = await api(operator, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(operator, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  }
  try {
    await login(operator, 'SuperAdmin'); before = await access();
    for (const code of codes) expect(before.moduleGrants.find((g: any) => g.code === code)?.enabled).toBe(true);
    const title = unique('Revoke palette source');
    const source = await api(page, 'createBookmark', ['https://example.invalid/' + unique('revoke'), title, 'Synthetic authority boundary']);
    const sourceBefore = sql('Bookmark', source.id);
    await page.goto('/search'); await openPalette(page);
    const palette = page.getByRole('dialog', { name: 'Command Palette', exact: true });
    const input = palette.getByRole('combobox', { name: 'Tìm lệnh hoặc nguồn', exact: true });
    await input.fill(title);
    const option = palette.getByRole('option').filter({ hasText: title });
    await expect(option).toBeVisible(); await expect(option).toHaveAttribute('aria-disabled', 'false');
    await setGrant('FX21', false);
    const executed = page.waitForResponse(r => r.url().endsWith('/api/v1/commands/execute') && r.request().method() === 'POST');
    await input.press('ArrowDown'); await expect(option).toHaveAttribute('aria-selected', 'true'); await input.press('Enter');
    const denied = await executed; expect(denied.status()).toBe(404); expect((await denied.json()).code).toBe('ResourceUnavailable');
    await expect(palette.getByRole('option')).toHaveCount(0); await expect(palette.getByText(title, { exact: true })).toHaveCount(0);
    await expect(palette.getByRole('alert')).toBeVisible(); await page.keyboard.press('Escape'); await expect(palette).toHaveCount(0);
    expect(sql('Bookmark', source.id)).toEqual(sourceBefore); await setGrant('FX21', true);
    const name = unique('Revoke saved definition');
    const definition = { text: title, resourceType: 'Bookmark', from: null, to: null, includeArchived: false, sort: 'relevance' };
    const saved = await api(page, 'saveSavedSearch', [null, null, name, definition]);
    const savedBefore = sql('SavedQuery', saved.id);
    await page.goto('/search/saved'); const region = page.getByRole('region', { name: 'Saved Searches', exact: true });
    await region.getByRole('button', { name: 'Đọc danh sách đã lưu', exact: true }).click(); await expect(region.getByText(name, { exact: true })).toBeVisible();
    await setGrant('FX25', false);
    const refreshed = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/saved-searches' && r.request().method() === 'GET');
    await region.getByRole('button', { name: 'Đọc danh sách đã lưu', exact: true }).click();
    // SavedQuery's concrete current module denial is opaque409 ModuleUnavailable
    // (SqlSavedSearchService.Denied); command-resource denial remains exact404 above.
    const moduleDenied = await refreshed; expect(moduleDenied.status()).toBe(409); expect((await moduleDenied.json()).code).toBe('ModuleUnavailable');
    await expect(region.getByText(name, { exact: true })).toHaveCount(0); await expect(region.locator('li.resource-card')).toHaveCount(0);
    await expect(region.getByLabel('Tên Saved Search', { exact: true })).toHaveValue('');
    expect(sql('SavedQuery', saved.id)).toEqual(savedBefore); await setGrant('FX25', true);
    await api(page, 'deleteSavedSearch', [saved]); expect(sql('SavedQuery', saved.id)).toEqual([]);
  } catch (error) { scenarioError = error; throw error; }
  finally {
    const cleanupErrors: unknown[] = [];
    if (before) {
      for (const code of [...codes].reverse()) try { await setGrant(code, before.moduleGrants.find((g: any) => g.code === code).enabled); } catch (error) { cleanupErrors.push(error); }
      try {
        const restored = await access(); expect(restored.user.role).toBe(before.user.role);
        expect(restored.moduleGrants).toEqual(before.moduleGrants); expect(restored.actionGrants).toEqual(before.actionGrants);
      } catch (error) { cleanupErrors.push(error); }
    }
    try { await operatorContext.close(); } catch (error) { cleanupErrors.push(error); }
    if (cleanupErrors.length) throw new AggregateError([...(scenarioError ? [scenarioError] : []), ...cleanupErrors], 'Discovery authority restoration failed');
  }
});

test('E25 negative: IME composition Enter/Escape never executes or closes the palette', async ({ page }) => {
  await page.goto('/search'); await openPalette(page);
  const palette = page.getByRole('dialog', { name: 'Command Palette', exact: true });
  const input = palette.getByRole('combobox', { name: 'Tìm lệnh hoặc nguồn', exact: true });
  await input.fill('clear-recents'); const option = palette.getByRole('option', { name: 'Xóa lịch sử gần đây…', exact: true });
  await expect(option).toHaveAttribute('aria-disabled', 'false'); await input.press('ArrowDown'); await expect(option).toHaveAttribute('aria-selected', 'true');
  let executed = 0;
  const observe = (request: import('@playwright/test').Request) => { if (request.method() === 'POST' && request.url().endsWith('/api/v1/commands/execute')) executed++; };
  page.on('request', observe);
  // Browser-generated trusted key events while the actual React composition state
  // is active. DOM composition events represent IME lifecycle, not injected authority.
  await input.dispatchEvent('compositionstart', { data: '' });
  await input.press('Enter'); await input.press('Escape');
  await expect(palette).toBeVisible(); expect(executed).toBe(0);
  await input.dispatchEvent('compositionend', { data: '' });
  await expect(palette.getByRole('option', { name: 'Xóa lịch sử gần đây…', exact: true })).toHaveAttribute('aria-disabled', 'false');
  await input.press('Escape'); await expect(palette).toHaveCount(0); expect(executed).toBe(0); page.off('request', observe);
});

test('E25 negative: persisted legacy schema stays non-runnable until native explicit version1 replacement', async ({ page }) => {
  test.setTimeout(120000); await page.goto('/'); const me = await api(page, 'getMe');
  // The same approved executable must refuse wrong ownership markers and unknown
  // users before its only fixture INSERT. Keep process diagnostics private.
  function refuses(user: string, env: NodeJS.ProcessEnv) {
    try { execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ['seed-legacy-saved-query', user], { env, stdio: ['ignore', 'pipe', 'pipe'] }); return false; }
    catch (error) { return typeof (error as { status?: number }).status === 'number' && (error as { status: number }).status !== 0; }
  }
  expect(refuses(me.id, { ...process.env, NEXORA_E2E_RUN_ID: randomUUID() })).toBe(true);
  expect(refuses(randomUUID(), process.env)).toBe(true);
  const fixture = JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ['seed-legacy-saved-query', me.id], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
  expect(sql('SavedQuery', fixture.id)[0].SchemaVersion).toBe(99);
  await page.goto('/search/saved'); const region = page.getByRole('region', { name: 'Saved Searches', exact: true });
  await region.getByRole('button', { name: 'Đọc danh sách đã lưu', exact: true }).click();
  const item = region.locator('li.resource-card').filter({ has: page.getByText(fixture.title, { exact: true }) });
  await expect(item.getByText('Cần định nghĩa tương thích; không tự nới bộ lọc.', { exact: true })).toBeVisible();
  await expect(item.getByRole('button', { name: 'Chạy trên dữ liệu hiện tại', exact: true })).toBeDisabled();
  const deniedRun = await response(page, '/api/v1/saved-searches/' + fixture.id + '/run', 'POST'); expect(deniedRun.status).toBe(409); expect(deniedRun.body.code).toBe('MigrationRequired');
  await page.reload(); await region.getByRole('button', { name: 'Đọc danh sách đã lưu', exact: true }).click(); await expect(item.getByRole('button', { name: 'Chạy trên dữ liệu hiện tại', exact: true })).toBeDisabled();
  await item.getByRole('button', { name: 'Sửa định nghĩa', exact: true }).click(); await page.locator('#global-search-query').fill('explicit compatible replacement'); await page.locator('#global-search-type').selectOption('Bookmark');
  const replaced = page.waitForResponse(r => r.url().endsWith('/api/v1/saved-searches/' + fixture.id) && r.request().method() === 'PUT');
  await region.getByRole('button', { name: 'Xác nhận thay bằng định nghĩa phiên bản 1', exact: true }).click(); expect((await replaced).status()).toBe(200);
  const persisted = sql('SavedQuery', fixture.id)[0]; expect(persisted.SchemaVersion).toBe(1); const definition = JSON.parse(persisted.QueryJson); expect(definition.Text ?? definition.text).toBe('explicit compatible replacement');
  await expect(item.getByRole('button', { name: 'Chạy trên dữ liệu hiện tại', exact: true })).toBeEnabled();
  await item.getByRole('button', { name: 'Xóa định nghĩa', exact: true }).click(); await page.getByRole('dialog', { name: 'Xóa Saved Search?', exact: true }).getByRole('button', { name: 'Xác nhận xóa định nghĩa', exact: true }).click();
  await expect.poll(() => sql('SavedQuery', fixture.id)).toEqual([]);
});
