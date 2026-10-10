import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('News categories: private name containers, dirty cancellation, explicit conflict recovery and actual paging', async ({ page: fixturePage, browser }) => {
  test.setTimeout(180000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const ownerContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: fixturePage.viewportSize() ?? undefined });
  const page = await ownerContext.newPage();
  const module = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX29');
  const originalPolicy = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof originalPolicy) => {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX29');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(admin, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  // Retained isolated owner: defaults verify migration backfill and category reads.
  await login(page, 'SuperAdmin'); await page.goto('/'); const me = await api(page, 'getMe');
  const originalGrant = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX29');
  const grant = async (enabled: boolean) => {
    const current = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX29');
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: originalGrant.moduleId, enabled }];
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let failed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX29', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'News categories', exact: true })).toBeVisible();
    const modal = page.locator('.resource-form-dialog'), root = '/api/v1/news/categories', name = unique('Category_%[x]');
    for (const defaultName of ['AI News', 'Tech News']) {
      const defaults = await response(page, root + '/?query=' + encodeURIComponent(defaultName), 'GET');
      expect(defaults.status).toBe(200); expect(defaults.body.items.some((item: any) => item.metadata.name === defaultName)).toBe(true);
    }
    await page.getByText('New category', { exact: true }).click(); await modal.getByLabel('Category name', { exact: true }).fill('Unsaved');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByText('New category', { exact: true }).click(); await modal.getByLabel('Category name', { exact: true }).fill(name);
    const creation = page.waitForResponse(r => r.url().endsWith(root + '/') && r.request().method() === 'POST');
    await modal.getByText('Save category', { exact: true }).click(); const id = (await (await creation).json()).id; await expect(modal).toHaveCount(0);
    const search = async (query: string) => { await page.getByLabel('Category name search').fill(query); await page.getByText('Search categories', { exact: true }).click(); await expect(page.getByText('Tải lại', { exact: true })).toBeEnabled(); };
    await search(name); const card = page.locator('.resource-card').filter({ has: page.getByRole('heading', { name, exact: true }) });
    expect(sql('NewsCategory', id)[0].Name).toBe(name); expect(sql('NewsCategory', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    await card.getByText('Edit category', { exact: true }).click(); await modal.getByLabel('Category name').fill(name + ' draft');
    const current = (await response(page, root + '/' + id, 'GET')).body;
    expect((await response(page, root + '/' + id, 'PUT', { metadata: { schemaVersion: 1, name: name + ' concurrent' } }, current.etag)).status).toBe(200);
    await modal.getByText('Save category', { exact: true }).click(); await expect(modal.getByLabel('Current category version')).toContainText(name + ' concurrent');
    await modal.getByText('Reapply draft', { exact: true }).click(); await modal.getByText('Save category', { exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('NewsCategory', id)[0].Name).toBe(name + ' draft');
    const paging = unique('CategoryPaging_%[');
    for (let n = 0; n < 29; n++) expect((await response(page, root + '/', 'POST', { metadata: { schemaVersion: 1, name: paging + '-' + String(n).padStart(2, '0') } })).status).toBe(201);
    await search(paging); await expect(page.locator('.resource-card')).toHaveCount(25);
    await page.getByText('Load more categories', { exact: true }).click(); await expect(page.locator('.resource-card')).toHaveCount(29); await expect(page.getByText('Load more categories', { exact: true })).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await grant(false); await page.getByText('Tải lại', { exact: true }).click(); await expect(page.locator('.resource-card')).toHaveCount(0);
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(originalGrant.enabled); await policy(originalPolicy); } catch (e) { if (!failed) throw e; } finally { await ownerContext.close(); await context.close(); } }
});
