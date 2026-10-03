import { test, expect, api, card, confirm, login, sql, unique, showPagedCard } from './fixtures';
import { response } from './time-focus-helpers';

test('Skill: explicit self assessment, SQL persistence, dirty conflict, pagination and archive cohort', async ({ page, browser }) => {
  test.setTimeout(180000);
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => console.error('Skill browser error:', error.message));
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  const module = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX40');
  const system = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const policy = async (change: typeof system) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.code === 'FX40');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  await page.goto('/'); const me = await api(page, 'getMe'); const original = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX40');
  const grant = async (enabled: boolean) => {
    const current = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX40'); if (current.enabled === enabled) return;
    const changes = [{ moduleId: original.moduleId, enabled }]; const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let workflowFailed = false;
  try {
    console.info('Skill verification: enabling the installed subset');
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX40', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Skills', exact: true })).toBeVisible();
    console.info('Skill verification: form cancel and browser Back');
    await page.getByRole('button', { name: 'New Skill', exact: true }).click(); const modal = page.getByRole('dialog');
    await modal.getByLabel('Title', { exact: true }).fill('Draft should not persist'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole('button', { name: 'New Skill', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Browser Back draft'); await page.goBack();
    await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal.getByLabel('Title', { exact: true })).toHaveValue('Browser Back draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    const title = unique('Skill');
    console.info('Skill verification: explicit assessment and SQL read');
    await page.getByRole('button', { name: 'New Skill', exact: true }).click();
    await modal.getByLabel('Title', { exact: true }).fill(title); await expect(modal.getByLabel('Self-assessed level', { exact: true })).toHaveValue(''); await modal.getByLabel('Self-assessed level', { exact: true }).selectOption('Intermediate'); await modal.getByLabel('Category', { exact: true }).fill('Engineering'); await modal.getByLabel('Last used', { exact: true }).fill('2026-09-30');
    const createdResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/learning/skills') && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal).toHaveCount(0); const created = await (await createdResponse).json(); const id = created.itemId;
    expect(sql('Skill', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    let item = (await response(page, `/api/v1/learning/skills/${id}`, 'GET')).body; expect(item.level).toBe('Intermediate'); expect(item.lastUsed).toBe('2026-09-30'); expect(sql('Skill', id)[0].Level).toBe('Intermediate');
    console.info('Skill verification: conflict recovery');
    await card(page, title).getByRole('button', { name: 'Edit Skill', exact: true }).click(); await modal.getByLabel('Description', { exact: true }).fill('Reapplied UI draft');
    expect((await response(page, `/api/v1/learning/skills/${id}`, 'PUT', { title, category: item.category, lastUsed: item.lastUsed, description: 'Concurrent current value' }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal.getByLabel('Current Skill version', { exact: true })).toContainText('Concurrent current value');
    await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Skill', id)[0].Description).toBe('Reapplied UI draft');
    const prefix = unique('page');
    console.info('Skill verification: pagination');
    for (let n = 0; n < 26; n++) expect((await response(page, '/api/v1/learning/skills', 'POST', { title: `${prefix}-${n}`, level: 'Beginner' })).status).toBe(201);
    console.info('Skill verification: fixtures created'); await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); console.info('Skill verification: list reloaded'); await showPagedCard(page, title, 'Load more Skill items'); console.info('Skill verification: target found through pagination');
    const row = card(page, title); await row.getByRole('button', { name: 'Edit Skill', exact: true }).click();
    await modal.getByLabel('Description', { exact: true }).fill('Synthetic UI note'); await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Skill', id)[0].Description).toBe('Synthetic UI note');
    console.info('Skill verification: lifecycle and revoke');
    expect(sql('Skill', id)[0].Level).toBe('Intermediate');
    await row.getByRole('button', { name: 'Assess proficiency', exact: true }).click(); await modal.getByLabel('Self-assessed level', { exact: true }).selectOption('Expert'); await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Skill', id)[0].Level).toBe('Expert');
    await row.getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive'); await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await expect(row).toBeVisible();
    await row.getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); expect(sql('Skill', id)[0].PreArchiveState).toBe('Active');
    await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('Skill', id)[0].Status).toBe('Archived');
    await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Unarchive', exact: true }).click(); await confirm(page, 'Unarchive'); expect(sql('Skill', id)[0].Status).toBe('Active');
    await page.getByLabel('Status', { exact: true }).selectOption('Active'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash');
    await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Delete permanently', exact: true }).click(); await confirm(page, 'Delete permanently'); expect(sql('Skill', id)).toEqual([]);
    await page.getByRole('button', { name: 'New Skill', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Revoked draft'); await modal.getByLabel('Self-assessed level', { exact: true }).selectOption('Beginner'); await grant(false);
    await modal.getByRole('button', { name: 'Save Skill', exact: true }).click(); await expect(modal).toHaveCount(0); await expect(page.locator('.resource-card')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (error) { workflowFailed = true; console.error(error instanceof Error ? error.message : 'Skill workflow failed'); throw error; }
  finally {
    try { await grant(original.enabled); await policy(system); }
    catch (cleanupError) { if (!workflowFailed) throw cleanupError; console.error('Skill test policy cleanup failed after the workflow failure.'); }
    finally { await context.close(); }
  }
});
