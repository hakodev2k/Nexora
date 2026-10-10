import { test, expect, api, confirm, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('Monitoring: private HTTP configuration, explicit preferences, conflicts and real paging without observations', async ({ page, browser }) => {
  test.setTimeout(180000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const module = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX36');
  const originalPolicy = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof originalPolicy) => {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX36');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(admin, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe');
  const originalGrant = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX36');
  const grant = async (enabled: boolean) => {
    const current = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX36');
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: originalGrant.moduleId, enabled }];
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let failed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX36', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Monitoring', exact: true })).toBeVisible();
    const modal = page.locator('.resource-form-dialog'); const root = '/api/v1/monitoring/monitors'; const title = unique('Monitor');
    await page.getByText('New Monitor', { exact: true }).click();
    await expect(modal.getByLabel('Initial preference')).toHaveValue('');
    await modal.getByLabel('Title', { exact: true }).fill('Unsaved'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click();
    await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByText('New Monitor', { exact: true }).click();
    await modal.getByLabel('Title', { exact: true }).fill(title); await modal.getByLabel('Target URL').fill('https://monitor.example/health');
    await modal.getByLabel('Interval seconds').fill('60'); await modal.getByLabel('Initial preference').selectOption('true');
    const creation = page.waitForResponse(r => r.url().endsWith(root + '/') && r.request().method() === 'POST');
    await modal.getByText('Save Monitor', { exact: true }).click(); const id = (await (await creation).json()).id;
    await expect(modal).toHaveCount(0);
    const search = async (query: string) => { await page.getByLabel('Title search').fill(query); await page.getByText('Search Monitor', { exact: true }).click(); await expect(page.getByText('Tải lại', { exact: true })).toBeEnabled(); };
    await search(title);
    const card = page.locator('.resource-card').filter({ has: page.getByRole('heading', { name: title, exact: true }) });
    await expect(card).toContainText('Unknown'); expect(sql('Monitor', id)[0].LastObservedAt).toBeNull();
    expect(sql('Monitor', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    await card.getByText('Pause Monitor', { exact: true }).click(); await confirm(page, 'Pause Monitor'); await expect(card).toContainText('Paused');
    await card.getByText('Resume Monitor', { exact: true }).click(); await confirm(page, 'Resume Monitor'); await expect(card).toContainText('Unknown');
    await card.getByText('Edit Monitor', { exact: true }).click(); await modal.getByLabel('Interval seconds').fill('75');
    const current = (await response(page, root + '/' + id, 'GET')).body;
    expect((await response(page, root + '/' + id, 'PUT', { metadata: { ...current.metadata, intervalSeconds: 90 } }, current.etag)).status).toBe(200);
    await modal.getByText('Save Monitor', { exact: true }).click(); await expect(modal.getByLabel('Current Monitor version')).toContainText('90');
    await modal.getByText('Reapply draft', { exact: true }).click(); await modal.getByText('Save Monitor', { exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('Monitor', id)[0].IntervalSeconds).toBe(75); expect(sql('Monitor', id)[0].LastObservedAt).toBeNull();
    const paging = unique('MonitorPaging');
    for (let n = 0; n < 29; n++) expect((await response(page, root + '/', 'POST', { metadata: { ...current.metadata, title: paging + '-' + String(n).padStart(2, '0') }, enabled: false })).status).toBe(201);
    await search(paging); await expect(page.locator('.resource-card')).toHaveCount(25);
    await page.getByText('Load more Monitor items', { exact: true }).click(); await expect(page.locator('.resource-card')).toHaveCount(29);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await grant(false); await page.getByText('Tải lại', { exact: true }).click(); await expect(page.locator('.resource-card')).toHaveCount(0);
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(originalGrant.enabled); await policy(originalPolicy); } catch (e) { if (!failed) throw e; } finally { await context.close(); } }
});
