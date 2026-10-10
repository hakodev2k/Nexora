import { test, expect, api, confirm, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test('Calendar ICS: real scanned upload, paged preview, explicit partial import, exact dates, duplicate and cancel reports', async ({ page, browser }) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const superPage = await context.newPage(); await login(superPage, 'SuperAdmin');
  await page.goto('/'); const me = await api(page, 'getMe');
  const modules = (await api(superPage, 'listAdminModules')).items.filter((m: any) => ['FX07', 'FX10', 'FX13'].includes(m.code));
  const originalGrants = (await api(superPage, 'getAdminUserAccess', [me.id])).moduleGrants.filter((m: any) => ['FX07', 'FX10', 'FX13'].includes(m.code));
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
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX10', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Calendar ICS Import', exact: true })).toBeVisible();
    const prefix = unique('ICS'); const entry = (i: string, extra = '', time = 'DTSTART:20261003T100000Z\r\nDTEND:20261003T110000Z') => `BEGIN:VEVENT\r\nUID:${prefix}-${i}\r\nSUMMARY:${prefix} ${i}\r\nDESCRIPTION:Private literal <script>never executed</script>\\nSecond line\r\n${time}\r\n${extra}END:VEVENT\r\n`;
    const content = 'BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Synthetic browser test//EN\r\n' + Array.from({ length: 29 }, (_, i) => entry(String(i), i === 0 ? 'STATUS:CANCELLED\r\nBEGIN:VALARM\r\nTRIGGER:-PT15M\r\nEND:VALARM\r\n' : '')).join('') + entry('all-day', '', 'DTSTART;VALUE=DATE:20111230\r\nDTEND;VALUE=DATE:20111231') + entry('0') + entry('recurring', 'RRULE:FREQ=DAILY\r\n') + entry('invalid').replace('DESCRIPTION:Private literal <script>never executed</script>\\nSecond line\r\n', '') + 'END:VCALENDAR\r\n';
    await page.getByRole('button', { name: 'Chọn file ICS', exact: true }).click(); const modal = page.locator('.resource-form-dialog');
    await modal.getByLabel('ICS file', { exact: true }).setInputFiles({ name: prefix + '.ics', mimeType: 'text/calendar', buffer: Buffer.from(content) });
    await expect.poll(() => modal.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await modal.getByRole('button', { name: 'Validate / Preview', exact: true }).click(); await expect(modal).toHaveCount(0);
    const report = page.getByRole('article', { name: 'ICS report' }); await expect(report).toContainText('PreviewReady'); await expect(report).toContainText('Total 33'); await expect(report).toContainText('Initially valid 30');
    await expect(report.getByRole('heading', { name: /^Row / })).toHaveCount(25); await expect(report).toContainText('ReminderIgnored'); await report.getByRole('button', { name: 'Tải thêm rows', exact: true }).click(); await expect(report.getByRole('heading', { name: /^Row / })).toHaveCount(33);
    await report.getByLabel('Row outcome', { exact: true }).selectOption('Invalid'); await expect(report.getByRole('heading', { name: /^Row / })).toHaveCount(2); await expect(report).toContainText('RecurringUnsupported');
    await report.getByLabel('Row outcome', { exact: true }).selectOption(''); await expect(report.getByRole('heading', { name: /^Row / })).toHaveCount(25);
    const batch = (await response(page, '/api/v1/transfer/calendar/imports', 'GET')).body.items[0]; const before = sql('ImportBatch', batch.id); expect(before).toHaveLength(1); expect(before[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase()); expect(before[0].AppliedCount).toBe(0);
    await report.getByRole('button', { name: 'Import Valid', exact: true }).click(); await confirm(page, 'Xác nhận Import'); await expect(report).toContainText('Completed'); await expect(report).toContainText('Applied 30'); expect(sql('ImportBatch', batch.id)[0].AppliedCount).toBe(30);
    await report.getByLabel('Row outcome', { exact: true }).selectOption('Applied'); await report.getByRole('button', { name: 'Tải thêm rows', exact: true }).click(); await expect(report.getByRole('heading', { name: /^Row / })).toHaveCount(30);
    const applied = (await response(page, `/api/v1/transfer/calendar/imports/${batch.id}/rows?outcome=Applied&cursor=25`, 'GET')).body.items;
    const allDay = applied.find((r: any) => r.candidate?.isAllDay); expect(allDay.candidate.startDate).toBe('2011-12-30'); expect(sql('Event', allDay.resultResourceId)[0].StartDate.slice(0, 10)).toBe('2011-12-30');
    await report.getByRole('button', { name: 'Tạo preview mới từ file', exact: true }).click(); await expect(report).toContainText('PreviewReady'); await expect(report).toContainText('Initially valid 0'); await expect(report).toContainText('Skipped 33');
    await report.getByRole('button', { name: 'Hủy preview', exact: true }).click(); await confirm(page, 'Xác nhận hủy'); await expect(report).toContainText('Canceled');
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    const fx10 = originalGrants.find((g: any) => g.code === 'FX10'); await grant([{ moduleId: fx10.moduleId, enabled: false }]); await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('article', { name: 'ICS report' })).toHaveCount(0); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(originalGrants.map((g: any) => ({ moduleId: g.moduleId, enabled: g.enabled }))); for (const module of modules) await policy(module, module.systemEnabled, module.registrationEnabled); } catch (e) { if (!failed) throw e; } finally { await context.close(); } }
});
