import { test, expect, api, card, confirm, login, sql, unique, showPagedCard } from './fixtures';
import { response } from './time-focus-helpers';

test('Wishlist: real form, decimal SQL persistence, dirty cancel, pagination and preserved archive cohort', async ({ page, browser }) => {
  test.setTimeout(180000);
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => console.error('Wishlist browser error:', error.message));
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  const module = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX31');
  const system = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof system) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX31');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe'); const original = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX31');
  const grant = async (enabled: boolean) => {
    const current = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX31'); if (current.enabled === enabled) return;
    const changes = [{ moduleId: original.moduleId, enabled }]; const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let workflowFailed = false;
  try {
    console.info('Wishlist verification: enabling the installed subset');
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX31', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Wishlist', exact: true })).toBeVisible();
    console.info('Wishlist verification: form cancel and browser Back');
    await page.getByRole('button', { name: 'New Wishlist item', exact: true }).click(); const modal = page.getByRole('dialog');
    await modal.getByLabel('Title', { exact: true }).fill('Draft should not persist'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole('button', { name: 'New Wishlist item', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Browser Back draft'); await page.goBack();
    await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal.getByLabel('Title', { exact: true })).toHaveValue('Browser Back draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    const title = unique('Wishlist');
    console.info('Wishlist verification: real create and SQL read');
    await page.getByRole('button', { name: 'New Wishlist item', exact: true }).click();
    await modal.getByLabel('Title', { exact: true }).fill(title); await modal.getByLabel('Quantity', { exact: true }).fill('1.12345678'); await modal.getByLabel('Desired amount', { exact: true }).fill('12345678901234567890.12345678'); await modal.getByLabel('Currency', { exact: true }).fill('USD');
    const createdResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/shopping/wishlist') && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Save Wishlist item', exact: true }).click(); await expect(modal).toHaveCount(0); const created = await (await createdResponse).json(); const id = created.itemId;
    expect(sql('WishlistItem', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    // Decimal strings are verified from the API; SQL JSON numeric parsing cannot preserve 28 significant digits.
    let item = (await response(page, `/api/v1/shopping/wishlist/${id}`, 'GET')).body; expect(item.targetAmount).toBe('12345678901234567890.12345678'); expect(item.quantity).toBe('1.12345678');
    console.info('Wishlist verification: conflict recovery');
    await card(page, title).getByRole('button', { name: 'Edit Wishlist item', exact: true }).click(); await modal.getByLabel('Notes', { exact: true }).fill('Reapplied UI draft');
    expect((await response(page, `/api/v1/shopping/wishlist/${id}`, 'PUT', { title, quantity: item.quantity, targetAmount: item.targetAmount, currency: item.currency, notes: 'Concurrent current value' }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Wishlist item', exact: true }).click(); await expect(modal.getByLabel('Current Wishlist version', { exact: true })).toContainText('Concurrent current value');
    await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Wishlist item', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WishlistItem', id)[0].Notes).toBe('Reapplied UI draft');
    const prefix = unique('page');
    console.info('Wishlist verification: pagination');
    for (let n = 0; n < 26; n++) expect((await response(page, '/api/v1/shopping/wishlist', 'POST', { title: `${prefix}-${n}`, quantity: '1' })).status).toBe(201);
    console.info('Wishlist verification: fixtures created'); await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); console.info('Wishlist verification: list reloaded'); await showPagedCard(page, title, 'Load more Wishlist items'); console.info('Wishlist verification: target found through pagination');
    const row = card(page, title); await row.getByRole('button', { name: 'Edit Wishlist item', exact: true }).click();
    await modal.getByLabel('Notes', { exact: true }).fill('Synthetic UI note'); await modal.getByRole('button', { name: 'Save Wishlist item', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WishlistItem', id)[0].Notes).toBe('Synthetic UI note');
    console.info('Wishlist verification: lifecycle and revoke');
    await row.getByRole('button', { name: 'Mark purchased', exact: true }).click(); await confirm(page, 'Mark purchased'); expect(sql('WishlistItem', id)[0].Status).toBe('Purchased');
    await page.getByLabel('Status', { exact: true }).selectOption('Purchased'); await expect(row).toBeVisible();
    await row.getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive'); await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await expect(row).toBeVisible();
    await row.getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); expect(sql('WishlistItem', id)[0].PreArchiveState).toBe('Purchased');
    await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('WishlistItem', id)[0].Status).toBe('Archived');
    await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Unarchive', exact: true }).click(); await confirm(page, 'Unarchive'); expect(sql('WishlistItem', id)[0].Status).toBe('Purchased');
    await page.getByLabel('Status', { exact: true }).selectOption('Purchased'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash');
    await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Delete permanently', exact: true }).click(); await confirm(page, 'Delete permanently'); expect(sql('WishlistItem', id)).toEqual([]);
    await page.getByRole('button', { name: 'New Wishlist item', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Revoked draft'); await grant(false);
    await modal.getByRole('button', { name: 'Save Wishlist item', exact: true }).click(); await expect(modal).toHaveCount(0); await expect(page.locator('.resource-card')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (error) { workflowFailed = true; console.error(error instanceof Error ? error.message : 'Wishlist workflow failed'); throw error; }
  finally {
    try { await grant(original.enabled); await policy(system); }
    catch (cleanupError) { if (!workflowFailed) throw cleanupError; console.error('Wishlist test policy cleanup failed after the workflow failure.'); }
    finally { await context.close(); }
  }
});

