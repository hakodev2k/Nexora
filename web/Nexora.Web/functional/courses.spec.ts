import { test, expect, api, card, confirm, login, sql, unique, showPagedCard } from './fixtures';
import { response } from './time-focus-helpers';

test('Course: exact progress, SQL persistence, dirty conflict, pagination and owned milestone cohort', async ({ page, browser }) => {
  test.setTimeout(180000);
  page.setDefaultTimeout(15000);
  page.on('pageerror', error => console.error('Course browser error:', error.message));
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
    console.info('Course verification: enabling the installed subset');
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX40', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click(); await page.getByRole('button', { name: 'Courses', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Courses', exact: true })).toBeVisible();

    console.info('Course verification: dirty cancellation and browser Back');
    await page.getByRole('button', { name: 'New Course', exact: true }).click(); const modal = page.getByRole('dialog');
    await modal.getByLabel('Title', { exact: true }).fill('Uncommitted draft'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole('button', { name: 'New Course', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Back draft'); await page.goBack();
    await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal.getByLabel('Title', { exact: true })).toHaveValue('Back draft');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click();
    const title = unique('Course'); await page.getByRole('button', { name: 'New Course', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill(title);
    await expect(modal.getByLabel('Progress mode', { exact: true })).toHaveValue(''); await modal.getByLabel('Progress mode', { exact: true }).selectOption('ManualPercent'); await modal.getByLabel('Provider', { exact: true }).fill('Manual provider');
    const createdResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/learning/courses') && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0);
    const id = (await (await createdResponse).json()).itemId; expect(sql('Course', id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    let item = (await response(page, `/api/v1/learning/courses/${id}`, 'GET')).body;
    await card(page, title).getByRole('button', { name: 'Edit Course', exact: true }).click(); await modal.getByLabel('Notes', { exact: true }).fill('Reapplied note');
    expect((await response(page, `/api/v1/learning/courses/${id}`, 'PUT', { title, provider: item.provider, progressMode: item.progressMode, notes: 'Concurrent current note' }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal.getByLabel('Current Course version', { exact: true })).toContainText('Concurrent current note'); await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Course', id)[0].Notes).toBe('Reapplied note');
    console.info('Course verification: actual visible pagination'); const prefix = unique('course-page');
    for (let n = 0; n < 26; n++) expect((await response(page, '/api/v1/learning/courses', 'POST', { title: `${prefix}-${n}`, progressMode: 'ManualPercent' })).status).toBe(201);
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await showPagedCard(page, title, 'Load more Course items');
    console.info('Course verification: exact percent, explicit completion and preserved dates');
    await card(page, title).getByRole('button', { name: 'Record progress', exact: true }).click(); await modal.getByLabel('Progress percent', { exact: true }).fill('12.345678'); await modal.getByLabel('Start course explicitly', { exact: true }).check(); await modal.getByLabel('Actual start date', { exact: true }).fill('2026-10-01'); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('Course', id)[0].ManualProgress).toBe(0.12345678); expect(sql('Course', id)[0].Status).toBe('InProgress'); await page.getByLabel('Status', { exact: true }).selectOption('InProgress');
    await card(page, title).getByRole('button', { name: 'Record progress', exact: true }).click(); await modal.getByLabel('Progress percent', { exact: true }).fill('100'); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Course', id)[0].Status).toBe('InProgress');
    await card(page, title).getByRole('button', { name: 'Record progress', exact: true }).click(); await modal.getByLabel('Progress percent', { exact: true }).fill('90');
    item = (await response(page, `/api/v1/learning/courses/${id}`, 'GET')).body; expect((await response(page, `/api/v1/learning/courses/${id}/progress`, 'POST', { manualProgress: '0.5' }, item.etag)).status).toBe(200);
    await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal.getByLabel('Current Course version', { exact: true })).toContainText('Current progress percent'); await expect(modal.getByLabel('Current Course version', { exact: true }).locator('dd').filter({ hasText: /^50$/ })).toHaveCount(1);
    await modal.getByRole('button', { name: 'Reapply draft', exact: true }).click(); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('Course', id)[0].ManualProgress).toBe(0.9);
    await card(page, title).getByRole('button', { name: 'Complete Course', exact: true }).click(); await modal.getByLabel('Actual completion date', { exact: true }).fill('2026-10-02'); await confirm(page, 'Complete Course'); expect(sql('Course', id)[0].Status).toBe('Completed');
    await page.getByLabel('Status', { exact: true }).selectOption('Completed'); await card(page, title).getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive'); await page.getByLabel('Status', { exact: true }).selectOption('Archived');
    await card(page, title).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await card(page, title).getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('Course', id)[0].Status).toBe('Archived');
    await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await card(page, title).getByRole('button', { name: 'Unarchive', exact: true }).click(); await confirm(page, 'Unarchive'); expect(sql('Course', id)[0].Status).toBe('Completed');
    console.info('Course verification: owned milestones and root cohort'); await page.getByLabel('Status', { exact: true }).selectOption('Planned');
    const milestoneTitle = unique('Milestone course'); await page.getByRole('button', { name: 'New Course', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill(milestoneTitle); await modal.getByLabel('Progress mode', { exact: true }).selectOption('Milestones');
    const milestoneCreate = page.waitForResponse(r => r.url().endsWith('/api/v1/learning/courses') && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); const milestoneId = (await (await milestoneCreate).json()).itemId;
    await expect(card(page, milestoneTitle)).toContainText('No Milestones'); await card(page, milestoneTitle).getByRole('button', { name: 'Milestones', exact: true }).click(); const pane = page.getByRole('region', { name: 'Course milestones', exact: true });
    let firstChild = '';
    for (const unit of ['First unit', 'Second unit']) { await pane.getByRole('button', { name: 'Add milestone', exact: true }).click(); await modal.getByLabel('Milestone title', { exact: true }).fill(unit); const created = page.waitForResponse(r => r.url().endsWith(`/api/v1/learning/courses/${milestoneId}/milestones`) && r.request().method() === 'POST'); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); const ack = await (await created).json(); if (unit === 'First unit') firstChild = ack.milestoneId; }
    const unit = pane.locator('article').filter({ has: page.getByRole('heading', { name: 'First unit', exact: true }) }); await unit.getByRole('button', { name: 'Complete milestone', exact: true }).click(); await expect(unit).toContainText('Done'); expect(sql('CourseMilestone', firstChild)[0].Completed).toBe(true); await expect(card(page, milestoneTitle)).toContainText('1/2 milestones'); expect(sql('Course', milestoneId)[0].Status).toBe('Planned');
    await unit.getByRole('button', { name: 'Move down', exact: true }).click(); await expect(pane.locator('article').first()).toContainText('Second unit');
    await unit.getByRole('button', { name: 'Undo milestone', exact: true }).click(); await expect(unit).toContainText('Pending');
    await card(page, milestoneTitle).getByRole('button', { name: 'Edit Course', exact: true }).click(); await expect(modal.getByLabel('Progress mode', { exact: true })).toBeDisabled(); await modal.getByRole('button', { name: 'Hủy', exact: true }).click();
    await card(page, milestoneTitle).getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive'); await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await card(page, milestoneTitle).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); expect(sql('CourseMilestone', firstChild)).toHaveLength(1);
    await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await card(page, milestoneTitle).getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('Course', milestoneId)[0].Status).toBe('Archived');
    await page.getByLabel('Status', { exact: true }).selectOption('Archived'); await card(page, milestoneTitle).getByRole('button', { name: 'Move to Trash', exact: true }).click(); await confirm(page, 'Move to Trash'); await page.getByLabel('Status', { exact: true }).selectOption('Trash'); await card(page, milestoneTitle).getByRole('button', { name: 'Delete permanently', exact: true }).click(); await confirm(page, 'Delete permanently'); expect(sql('Course', milestoneId)).toEqual([]); expect(sql('CourseMilestone', firstChild)).toEqual([]);
    await page.getByRole('button', { name: 'New Course', exact: true }).click(); await modal.getByLabel('Title', { exact: true }).fill('Revoked draft'); await modal.getByLabel('Progress mode', { exact: true }).selectOption('ManualPercent'); await grant(false); await modal.getByRole('button', { name: 'Save Course', exact: true }).click(); await expect(modal).toHaveCount(0); await expect(page.locator('.resource-card')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (error) { workflowFailed = true; throw error; }
  finally { try { await grant(original.enabled); await policy(system); } catch (error) { if (!workflowFailed) throw error; } finally { await context.close(); } }
});
