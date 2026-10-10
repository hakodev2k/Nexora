import { test, expect, api, card, confirm, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('Digital Assets: typed SQL records, exact values, dirty conflicts, private paged history and lifecycle', async ({ page, browser }) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  const module = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX38');
  const system = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof system) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX38');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe');
  const original = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX38');
  const grant = async (enabled: boolean) => {
    const current = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX38');
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: original.moduleId, enabled }];
    const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let failed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX38', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Digital Assets', exact: true })).toBeVisible();
    const root = '/api/v1/assets/digital', modal = page.getByRole('dialog'), title = unique('Digital');
    const search = async (query: string, state = 'Active') => {
      await page.getByLabel('Digital state', { exact: true }).selectOption(state);
      await page.getByLabel('Name or domain search', { exact: true }).fill(query);
      await page.getByRole('button', { name: 'Search Digital Assets', exact: true }).click();
      await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
    };
    await page.getByRole('button', { name: 'New Digital Asset', exact: true }).click();
    await expect(modal.getByLabel('Digital type', { exact: true })).toHaveValue('');
    await expect(modal.getByLabel('Initial Digital state', { exact: true })).toHaveValue('');
    await modal.getByLabel('Digital title', { exact: true }).fill('Unsaved digital draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click();
    await modal.getByRole('button', { name: 'Tiếp tục sửa', exact: true }).click();
    await expect(modal.getByLabel('Digital title', { exact: true })).toHaveValue('Unsaved digital draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    await page.getByRole('button', { name: 'New Digital Asset', exact: true }).click();
    await modal.getByLabel('Digital title', { exact: true }).fill(title);
    await modal.getByLabel('Digital type', { exact: true }).selectOption('Domain');
    await modal.getByLabel('Initial Digital state', { exact: true }).selectOption('Active');
    await modal.getByLabel('Domain name', { exact: true }).fill('bücher.example');
    await modal.getByLabel('Recorded cost (exact decimal)', { exact: true }).fill('9007199254740993.12345678');
    await modal.getByLabel('Cost currency', { exact: true }).fill('USD');
    await modal.getByLabel('Digital notes', { exact: true }).fill('Initial private digital note');
    const creation = page.waitForResponse(r => r.url().endsWith(root) && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Save Digital Asset', exact: true }).click(); await expect(modal).toHaveCount(0);
    const id = (await (await creation).json()).itemId;
    expect(sql('DigitalAsset', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    await search(title); await expect(card(page, title)).toContainText('xn--bcher-kva.example'); await expect(card(page, title)).toContainText('bücher.example');
    await expect(card(page, title)).toContainText('9007199254740993.12345678 USD');
    let item = (await response(page, root+'/'+id, 'GET')).body;
    await card(page, title).getByRole('button', { name: 'Edit Digital Asset', exact: true }).click();
    await expect(modal.getByLabel('Saved domain names')).toContainText('bücher.example'); await expect(modal.getByLabel('Saved domain names')).toContainText('xn--bcher-kva.example');
    await modal.getByLabel('Digital notes', { exact: true }).fill('Reapplied private digital note');
    expect((await response(page, root+'/'+id, 'PUT', { metadata: { ...item.metadata, notes: 'Concurrent digital note' }, confirmTypeChange: false }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Digital Asset', exact: true }).click(); await expect(modal).toContainText('Concurrent digital note');
    await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Digital Asset', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('DigitalAsset', id)[0].Notes).toBe('Reapplied private digital note');
    await card(page, title).getByRole('button', { name: 'Record renewal', exact: true }).click();
    await modal.getByLabel('Renewed on', { exact: true }).fill('2026-10-03'); await modal.getByLabel('New entered expiry', { exact: true }).fill('2028-01-01');
    await modal.getByLabel('Renewal note', { exact: true }).fill('Private renewal reason');
    await modal.getByLabel('Confirm Nexora record only, no provider renewal or charge', { exact: true }).check();
    await modal.getByRole('button', { name: 'Save renewal record', exact: true }).click(); await expect(modal).toHaveCount(0);
    await expect(card(page, title)).toContainText('9007199254740993.12345678 USD');
    await card(page, title).getByRole('button', { name: 'Digital history', exact: true }).click(); const history = page.getByRole('region', { name: 'Digital history', exact: true });
    await expect(history).toContainText('Initial private digital note'); await expect(history).toContainText('Private renewal reason');
    await history.getByLabel('History version', { exact: true }).fill('1'); await history.getByRole('button', { name: 'Filter history', exact: true }).click();
    await expect(history.getByRole('heading', { name: 'Version 1', exact: true })).toBeVisible(); await expect(history.locator('h3').filter({ hasText: /^Version / })).toHaveCount(1);
    await history.getByRole('button', { name: 'Close history', exact: true }).click();
    // Actual renewed records make both cursors exceed 25, without altering production limits.
    item = (await response(page, root+'/'+id, 'GET')).body;
    for (let n=0; n<27; n++) {
      const changed = await response(page, root+'/'+id+'/record-renewal', 'POST', { renewedOn: '2026-10-03', newExpiry: '2028-01-01', confirm: true, notes: 'Renewal iteration '+n }, item.etag);
      expect(changed.status).toBe(200); item = { ...item, etag: changed.body.etag };
    }
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await card(page, title).getByRole('button', { name: 'Digital history', exact: true }).click();
    await expect(history.locator('h3').filter({ hasText: /^Version / })).toHaveCount(25);
    await history.getByRole('button', { name: 'Load more Digital history', exact: true }).click(); await expect(history.locator('h3').filter({ hasText: /^Version / })).toHaveCount(31);
    await history.getByRole('button', { name: 'Load more Digital renewals', exact: true }).click(); await expect(history).toContainText('Private renewal reason');
    await history.getByRole('button', { name: 'Close history', exact: true }).click();
    const prefix = unique('DigitalPage');
    for (let n=0; n<26; n++) expect((await response(page, root, 'POST', { metadata: { title: prefix+'-'+n, kind: 'OnlineService', details: { onlineService: { plan: 'Recorded', serviceUrl: 'https://example.invalid/inert' } } }, state: 'Active' })).status).toBe(201);
    await search(prefix); await expect(page.locator('.resource-cards article')).toHaveCount(25);
    await page.getByRole('button', { name: 'Load more Digital Assets', exact: true }).click(); await expect(page.locator('.resource-cards article')).toHaveCount(26);
    const certificateTitle = unique('Certificate');
    const cert = await response(page, root, 'POST', { metadata: { title: certificateTitle, kind: 'Certificate', expiresOn: '2028-01-01', notes: 'x'.repeat(20000), details: { certificate: { subject: 'Synthetic public certificate', notAfter: '2029-01-01T12:00:00Z', subjectAlternativeNames: ['s'.repeat(253)] } } }, state: 'Active' }); expect(cert.status).toBe(201);
    await search(certificateTitle); await expect(card(page, certificateTitle)).toContainText('Expiry discrepancy:');
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await card(page, certificateTitle).getByRole('button', { name: 'Edit Digital Asset', exact: true }).click();
    await modal.getByLabel('Certificate NotAfter (offset or UTC instant)', { exact: true }).fill('invalid draft instant');
    await expect(modal.getByRole('button', { name: 'Save Digital Asset', exact: true })).toBeVisible();
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    await search(title); await card(page, title).getByRole('button', { name: 'Mark canceled', exact: true }).click(); await confirm(page, 'Mark canceled');
    await search(title, 'Canceled'); await card(page, title).getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive');
    await search(title, 'Archived'); await card(page, title).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash');
    await search(title, 'Trash'); await card(page, title).getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('DigitalAsset', id)[0].State).toBe('Archived');
    await search(title, 'Archived'); await card(page, title).getByRole('button', { name: 'Unarchive', exact: true }).click(); await confirm(page, 'Unarchive'); expect(sql('DigitalAsset', id)[0].State).toBe('Canceled');
    await search(title, 'Canceled'); await card(page, title).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash');
    await search(title, 'Trash'); await card(page, title).getByRole('button', { name: 'Delete permanently', exact: true }).click(); await confirm(page, 'Delete permanently'); expect(sql('DigitalAsset', id)).toEqual([]);
    await search(prefix); await grant(false); await card(page, prefix+'-0').getByRole('button', { name: 'Digital history', exact: true }).click();
    await expect(page.locator('.resource-cards article')).toHaveCount(0); await expect(history).toHaveCount(0); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(original.enabled); await policy(system); } catch (e) { if (!failed) throw e; } finally { await context.close(); } }
});
