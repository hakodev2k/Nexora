import { test, expect, api, card, confirm, login, sql, unique, showPagedCard } from './fixtures';
import { response } from './time-focus-helpers';

test('Assets: explicit states, SQL persistence, dirty conflict, paging, private history and lifecycle', async ({ page, browser }) => {
  test.setTimeout(180000);
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => console.error('Asset browser error:', error.message));
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  const module = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX37');
  const system = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof system) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX37');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe'); const original = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX37');
  const grant = async (enabled: boolean) => {
    const current = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX37'); if (current.enabled === enabled) return;
    const changes = [{ moduleId: original.moduleId, enabled }]; const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let workflowFailed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX37', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Assets', exact: true })).toBeVisible();
    const root = '/api/v1/assets/personal'; const modal = page.getByRole('dialog'); const title = unique('Asset');
    await page.getByRole('button', { name: 'New Asset', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Uncommitted asset');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole('button', { name: 'New Asset', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Asset Back draft'); await page.goBack();
    await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal.getByLabel('Title', { exact: true })).toHaveValue('Asset Back draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    await page.getByRole('button', { name: 'New Asset', exact: true }).click(); await expect(modal.getByLabel('Asset state', { exact: true })).toHaveValue(''); await expect(modal.getByLabel('Asset kind', { exact: true })).toHaveValue('');
    await modal.getByLabel('Title', { exact: true }).fill(title); await modal.getByLabel('Asset state', { exact: true }).selectOption('Active'); await modal.getByLabel('Asset kind', { exact: true }).selectOption('Device');
    await modal.getByLabel('Model', { exact: true }).fill('Model_%['); await modal.getByLabel('Category', { exact: true }).fill('Personal'); await modal.getByLabel('Notes', { exact: true }).fill('Initial owner note');
    const creation = page.waitForResponse(r => r.url().endsWith(root) && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Save Asset', exact: true }).click(); await expect(modal).toHaveCount(0);
    const id = (await (await creation).json()).itemId; expect(sql('PersonalAsset', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    await showPagedCard(page, title, 'Load more Asset items');
    let item = (await response(page, root+'/'+id, 'GET')).body;
    await card(page, title).getByRole('button', { name: 'Edit Asset', exact: true }).click(); await modal.getByLabel('Notes', { exact: true }).fill('Reapplied private note');
    expect((await response(page, root+'/'+id, 'PUT', { title, kind: 'Device', model: item.model, category: item.category, notes: 'Concurrent current note' }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Asset', exact: true }).click(); await expect(modal.getByLabel('Current Asset version')).toContainText('Concurrent current note');
    await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Asset', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('PersonalAsset', id)[0].Notes).toBe('Reapplied private note');
    const prefix = unique('A-asset-page'); for (let n=0; n<26; n++) expect((await response(page, root, 'POST', { title: prefix+'-'+n, kind: 'Other', state: 'Active' })).status).toBe(201);
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await showPagedCard(page, title, 'Load more Asset items');
    await page.getByLabel('Title or model search', { exact: true }).fill('_%['); await page.getByRole('button', { name: 'Search Asset', exact: true }).click(); await expect(card(page, title)).toBeVisible();
    await page.getByLabel('Kind filter', { exact: true }).selectOption('Electronics'); await expect(page.locator('.resource-cards article')).toHaveCount(0); await page.getByLabel('Kind filter', { exact: true }).selectOption('Device');
    await card(page, title).getByRole('button', { name: 'Change state', exact: true }).click(); await modal.getByLabel('Asset state', { exact: true }).selectOption('Sold'); await modal.getByLabel('Private reason').fill('Private sold reason');
    await modal.getByRole('button', { name: 'Save Asset', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('PersonalAsset', id)[0].State).toBe('Sold'); await page.getByLabel('State', { exact: true }).selectOption('Sold');
    await card(page, title).getByRole('button', { name: 'Asset history', exact: true }).click(); const history = page.getByRole('region', { name: 'Asset history', exact: true });
    await expect(history).toContainText('Private sold reason'); await expect(history).toContainText('Initial owner note'); await history.getByLabel('History version', { exact: true }).fill('1');
    await history.getByRole('button', { name: 'Filter history', exact: true }).click(); await expect(history.locator('article')).toHaveCount(1); await expect(history).toContainText('Version 1');
    await history.getByLabel('History version', { exact: true }).fill(''); await history.getByLabel('History action', { exact: true }).selectOption('assets.asset.transition'); await history.getByRole('button', { name: 'Filter history', exact: true }).click(); await expect(history.locator('article')).toHaveCount(1); await expect(history).toContainText('Private sold reason');
    await history.getByLabel('History from (UTC instant)', { exact: true }).fill('2000-01-01T00:00:00Z'); await history.getByLabel('History to (UTC, exclusive)', { exact: true }).fill('2001-01-01T00:00:00Z'); await history.getByRole('button', { name: 'Filter history', exact: true }).click(); await expect(history.locator('article')).toHaveCount(0); await history.getByRole('button', { name: 'Close history', exact: true }).click();
    item = (await response(page, root+'/'+id, 'GET')).body; for (let n=0;n<27;n++) { const edited = await response(page, root+'/'+id, 'PUT', { title, kind: 'Device', model: item.model, category: item.category, notes: 'History iteration '+n }, item.etag); expect(edited.status).toBe(200); item = { ...item, etag: edited.body.etag }; }
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await card(page, title).getByRole('button', { name: 'Asset history', exact: true }).click(); await expect(history.locator('article')).toHaveCount(25);
    await history.getByRole('button', { name: 'Load more Asset history', exact: true }).click(); await expect(history.locator('article')).toHaveCount(31); await history.getByRole('button', { name: 'Close history', exact: true }).click();
    await card(page, title).getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive'); await page.getByLabel('State', { exact: true }).selectOption('Archived');
    await card(page, title).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); await page.getByLabel('State', { exact: true }).selectOption('Trash');
    await card(page, title).getByRole('button', { name: 'Asset history', exact: true }).click(); await expect(history).toContainText('preArchiveState'); await expect(history).toContainText('Sold'); await history.getByRole('button', { name: 'Close history', exact: true }).click();
    await card(page, title).getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('PersonalAsset', id)[0].State).toBe('Archived'); await page.getByLabel('State', { exact: true }).selectOption('Archived');
    await card(page, title).getByRole('button', { name: 'Unarchive', exact: true }).click(); await confirm(page, 'Unarchive'); expect(sql('PersonalAsset', id)[0].State).toBe('Sold'); await page.getByLabel('State', { exact: true }).selectOption('Sold');
    await card(page, title).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); await page.getByLabel('State', { exact: true }).selectOption('Trash');
    await card(page, title).getByRole('button', { name: 'Delete permanently', exact: true }).click(); await confirm(page, 'Delete permanently'); expect(sql('PersonalAsset', id)).toEqual([]);
    // Revocation while reading history clears private data and leaves Reload usable.
    await page.getByRole('button', { name: 'Clear filters', exact: true }).click(); await showPagedCard(page, prefix+'-0', 'Load more Asset items'); await grant(false); await card(page, prefix+'-0').getByRole('button', { name: 'Asset history', exact: true }).click();
    await expect(page.locator('.resource-cards article')).toHaveCount(0); await expect(page.getByRole('region', { name: 'Asset history', exact: true })).toHaveCount(0); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (error) { workflowFailed = true; throw error; }
  finally { try { await grant(original.enabled); await policy(system); } catch (error) { if (!workflowFailed) throw error; } finally { await context.close(); } }
});
