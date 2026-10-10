import { test, expect, api, confirm, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('Calendar ICS export: explicit source/status/range, confirm, private bytes and authority clearing', async ({ page, browser }) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  await page.goto('/'); const me = await api(page, 'getMe');
  const modules = (await api(superPage, 'listAdminModules')).items.filter((m: any) => ['FX10', 'FX11', 'FX12', 'FX13'].includes(m.code));
  const originalGrants = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.filter((m: any) => ['FX10', 'FX11', 'FX12', 'FX13'].includes(m.code));
  const policy = async (module: any, systemEnabled: boolean, registrationEnabled: boolean) => {
    const current = (await api(superPage, 'listAdminModules')).items.find((m: any) => m.id === module.id);
    if (current.systemEnabled === systemEnabled && current.registrationEnabled === registrationEnabled) return;
    const change = { systemEnabled, registrationEnabled }; const preview = await api(superPage, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(superPage, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  const grant = async (changes: any[]) => { const current = await api(superPage, 'getAdminUserAccess', [me.id]); const different = changes.filter(change => current.moduleGrants.find((g: any) => g.moduleId === change.moduleId)?.enabled !== change.enabled); if (!different.length) return; const preview = await api(superPage, 'previewAdminAccess', [me.id, { kind: 'modules', changes: different }]); expect(preview.blockers).toEqual([]); await api(superPage, 'commitAdminModuleGrant', [me.id, preview.etag, different, preview.previewToken]); };
  let failed = false;
  try {
    for (const module of modules) await policy(module, true, true); await grant(originalGrants.map((g: any) => ({ moduleId: g.moduleId, enabled: true })));
    const prefix = unique('ICS-export');
    const start = '2040-10-04T10:00:00Z', end = '2040-10-04T11:00:00Z';
    const manual = await api(page, 'createCalendarEvent', [prefix + ' manual', 'Unicode tiếng Việt; literal <script>safe</script>', start, end, 'UTC']);
    const project = await api(page, 'createProject', [prefix + ' project', 'Synthetic export source project', start, end]);
    const task = await api(page, 'createTask', [project.id, prefix + ' task', null, 'NotStarted', end, start, end]);
    const manualBefore = sql('Event', manual.id)[0], taskBefore = sql('Task', task.id)[0];
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX10', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await page.getByRole('button', { name: 'ICS Export', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Calendar ICS Export', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Chọn phạm vi export', exact: true }).click();
    const modal = page.locator('.resource-form-dialog');
    await expect(modal.getByLabel('Manual Events', { exact: true })).toBeFocused();
    await modal.getByLabel('Task Events', { exact: true }).check();
    await modal.getByRole('combobox', { name: /^Range/ }).selectOption('Custom');
    await modal.getByLabel('Start date', { exact: true }).fill('2040-10-04');
    await modal.getByLabel('End date (exclusive)', { exact: true }).fill('2040-10-05');
    await page.keyboard.press('Escape'); await expect(page.getByText('Bỏ thay đổi chưa lưu?', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Tiếp tục sửa', exact: true }).click();
    await expect(modal.getByLabel('Start date', { exact: true })).toHaveValue('2040-10-04');
    await expect.poll(() => modal.evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    await modal.getByRole('button', { name: 'Preview export', exact: true }).click();
    const preview = page.getByRole('article', { name: 'Export preview' }); await expect(preview).toBeVisible();
    await page.getByRole('button', { name: 'ICS Import', exact: true }).click();
    await expect(page.getByText('Rời export đang chuẩn bị?', { exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Tiếp tục export', exact: true }).click(); await expect(preview).toBeVisible();
    await preview.getByRole('button', { name: 'Generate export', exact: true }).click(); await confirm(page, 'Xác nhận Generate');
    const report = page.getByRole('article', { name: 'Export report' }); await expect(report).toContainText('Ready');
    const filter = { schemaVersion: 1, sourceKinds: ['Manual', 'Task'], manualStatuses: ['Scheduled'], taskStatuses: ['NotStarted'], range: { start: '2040-10-04', end: '2040-10-05' }, containmentMode: 'FullyContained' };
    const jobs = (await response(page, '/api/v1/transfer/calendar/exports', 'GET')).body.items;
    const job = jobs.find((j: any) => j.filter.range?.start === filter.range.start); expect(job).toBeTruthy();
    const saved = sql('ExportJob', job.id)[0]; expect(saved.OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    expect(saved.FileObjectId).toBeNull(); expect(saved.EventCount).toBe(job.count); expect(saved.SourceCount).toBe(job.count);
    const downloadPromise = page.waitForEvent('download'); await report.getByRole('button', { name: 'Download ICS', exact: true }).click();
    const download = await downloadPromise; expect(download.suggestedFilename()).toBe('calendar.ics');
    const file = await download.createReadStream(); expect(file).toBeTruthy(); const parts: Buffer[] = []; for await (const chunk of file!) parts.push(Buffer.from(chunk));
    const bytes = Buffer.concat(parts), text = bytes.toString('utf8'); expect(bytes.length).toBe(saved.ArtifactBytes);
    expect(text).toContain(prefix + ' manual'); expect(text).toContain(prefix + ' task'); expect(text).toContain('X-NEXORA-PROJECT:');
    expect(text).not.toContain(manual.id); expect(text).not.toContain(task.id); expect(text).not.toContain(project.id); expect(text).not.toContain('VALARM');
    expect(text.match(/BEGIN:VEVENT/g)?.length).toBe(job.count);
    for (const line of text.split('\r\n')) expect(Buffer.byteLength(line, 'utf8')).toBeLessThanOrEqual(75);
    expect(sql('Event', manual.id)[0]).toEqual(manualBefore); expect(sql('Task', task.id)[0]).toEqual(taskBefore);
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(report).toHaveCount(0);
    const fx10 = originalGrants.find((g: any) => g.code === 'FX10'); await grant([{ moduleId: fx10.moduleId, enabled: false }]);
    await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Chọn phạm vi export', exact: true })).toBeDisabled();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(originalGrants.map((g: any) => ({ moduleId: g.moduleId, enabled: g.enabled }))); for (const module of modules) await policy(module, module.systemEnabled, module.registrationEnabled); } catch (e) { if (!failed) throw e; } finally { await context.close(); } }
});
