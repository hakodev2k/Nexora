import { test, expect, sql, card, confirm, unique, sqlTimeReportSeries, api } from './fixtures';
import { moduleApi, response, enableForTest } from './time-focus-helpers';
import type { Page } from '@playwright/test';
async function showTimeCard(page: Page, name: string) {
  for (let n = 0; n < 100; n++) {
    const next = page.getByRole('button', { name: 'Load more entries', exact: true });
    await expect.poll(async () => await card(page, name).isVisible() || await next.isVisible()).toBe(true);
    if (await card(page, name).isVisible()) return;
    const count = await page.locator('.resource-card').count(); await next.click();
    await expect.poll(() => page.locator('.resource-card').count()).toBeGreaterThan(count);
  }
  throw new Error('Time entry not found through visible pagination');
}


test('FN-041 Time Tracking: SQL create/edit/history, overlap, ETag, timer race, resume and Trash/Restore', async ({ page, browser }) => {
  test.setTimeout(90000); const pageErrors: string[] = []; page.on('pageerror', e => pageErrors.push(e.message)); await page.goto('/'); const enabled = await enableForTest(browser, page, 'FX18');
  try {
    await page.goto('/modules/FX18'); await expect(page.getByRole('heading', { name: 'Time Tracking', exact: true })).toBeVisible();
    const name = unique('manual-time'); const stamp = new Date(2010 + Math.floor(Math.random() * 10), Math.floor(Math.random() * 12), 1 + Math.floor(Math.random() * 20), 1, 0).toISOString().slice(0,16);
    await page.getByRole('button', { name: 'Manual entry', exact: true }).click(); const modal = page.getByRole('dialog');
    await modal.getByLabel('Description', { exact: true }).fill(name); await modal.getByLabel('Category', { exact: true }).fill('Local SQL QA');
    await modal.getByLabel('Start (múi giờ trình duyệt)', { exact: true }).fill(stamp);
    const end = new Date(new Date(stamp + ':00Z').getTime() + 3600000).toISOString().slice(0,16);
    await modal.getByLabel('End (múi giờ trình duyệt)', { exact: true }).fill(end);
    await modal.getByRole('checkbox').check(); const createResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/time/entries') && r.request().method() === 'POST'); await confirm(page, 'Save');
    let entry = await (await createResponse).json(); const rows = sql('TimeEntry', entry.id); expect(rows).toHaveLength(1); expect(rows[0].OwnerId.toLowerCase()).toBe(enabled.me.personalSpaceId.toLowerCase()); expect(rows[0].Status).toBe('Stopped');
    await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await showTimeCard(page, name);
    await card(page, name).getByRole('button', { name: 'Edit entry', exact: true }).click(); expect(pageErrors).toEqual([]); await expect(page.getByRole('dialog')).toBeVisible(); await page.getByRole('dialog').getByLabel('Description', { exact: true }).fill(name + '-edited'); const updateResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/time/entries/' + entry.id) && r.request().method() === 'PUT'); await confirm(page, 'Save'); const old = entry; entry = await (await updateResponse).json();
    expect(sql('TimeEntry', entry.id)[0].Description).toBe(name + '-edited'); const history = await moduleApi(page, 'timeApi', 'timeHistory', [entry.id]); expect(history.items.some((h: any) => h.before.description === name)).toBe(true);
    const body = { startAt: entry.startAt, endAt: entry.endAt, description: entry.description, category: entry.category, confirmOverlap: false };
    expect((await response(page, '/api/v1/time/entries/' + entry.id, 'PUT', body, old.etag)).status).toBe(412);
    expect((await response(page, '/api/v1/time/entries/' + entry.id, 'PUT', body)).status).toBe(428);
    expect((await response(page, '/api/v1/time/entries', 'POST', body)).status).toBe(409);
    const replayKey = crypto.randomUUID(); const confirmed = { ...body, description: name + "-overlap-proof", confirmOverlap: true };
    const created = await response(page, '/api/v1/time/entries', 'POST', confirmed, undefined, replayKey); expect(created.status).toBe(201);
    expect((await response(page, '/api/v1/time/entries', 'POST', confirmed, undefined, replayKey)).body.id).toBe(created.body.id);
    expect((await response(page, '/api/v1/time/entries', 'POST', { ...confirmed, description: 'different' }, undefined, replayKey)).status).toBe(409);
    expect((await response(page, '/api/v1/time/entries', 'POST', { ...confirmed, ownerId: enabled.me.personalSpaceId })).status).toBe(400);
    const report = await moduleApi(page, 'timeApi', 'timeReport'); const series = sqlTimeReportSeries(enabled.me.personalSpaceId);
    const parseSqlUtc = (value: string) => Date.parse(value.endsWith('Z') ? value : value + 'Z');
    const gross = series.reduce((total, row) => total + parseSqlUtc(row.EndAt) - parseSqlUtc(row.StartAt), 0);
    expect(report.grossDurationMilliseconds).toBe(gross); expect(report.entryCount).toBe(series.length); expect(report.hasOverlaps).toBe(true);

    const running = await moduleApi(page, 'timeApi', 'timeTimer'); if (running) await moduleApi(page, 'timeApi', 'timeStop', [running, crypto.randomUUID()]);
    const starts = await Promise.all([response(page, '/api/v1/time/timer', 'POST', { description: 'race A', category: null }), response(page, '/api/v1/time/timer', 'POST', { description: 'race B', category: null })]); expect(starts.map(s => s.status).sort()).toEqual([201,409]);
    const timer = starts.find(s => s.status === 201)!.body; expect(sql('TimeEntry', timer.id)[0].Status).toBe('Running');
    const stopped = await moduleApi(page, 'timeApi', 'timeStop', [timer, crypto.randomUUID()]); const stoppedAgain = await moduleApi(page, 'timeApi', 'timeStop', [timer, crypto.randomUUID()]); expect(stoppedAgain.endAt).toBe(stopped.endAt);
    const resumed = await moduleApi(page, 'timeApi', 'timeStart', [stopped.description, stopped.category, crypto.randomUUID(), stopped.id]); expect(resumed.id).not.toBe(stopped.id); expect(sql('TimeEntry', stopped.id)[0].EndAt).toBe(sql('TimeEntry', timer.id)[0].EndAt); await moduleApi(page, 'timeApi', 'timeStop', [resumed, crypto.randomUUID()]);
    await page.reload(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await showTimeCard(page, name + '-edited'); const row = card(page, name + '-edited');
    await row.getByRole('button', { name: 'Trash', exact: true }).click(); await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click(); expect(sql('TimeEntry', entry.id)[0].Status).toBe('Stopped');
    await row.getByRole('button', { name: 'Trash', exact: true }).click(); await confirm(page, 'Trash'); await expect.poll(() => sql('TimeEntry', entry.id)[0].Status).toBe('Trash');
    await page.getByLabel('Thùng rác', { exact: true }).check(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await showTimeCard(page, name + '-edited'); await row.getByRole('button', { name: 'Restore', exact: true }).click(); await confirm(page, 'Restore'); expect(sql('TimeEntry', entry.id)[0].Status).toBe('Stopped');
    const snapshot = sql('TimeEntry', entry.id); const access = await api(enabled.admin, 'getAdminUserAccess', [enabled.me.id]); const focusGrant = access.moduleGrants.find((g: any) => g.code === 'FX19');
    const setFocusGrant = async (value: boolean) => { const changes = [{ moduleId: focusGrant.moduleId, enabled: value }]; const preview = await api(enabled.admin, 'previewAdminAccess', [enabled.me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]); await api(enabled.admin, 'commitAdminModuleGrant', [enabled.me.id, preview.etag, changes, preview.previewToken]); };
    if (!focusGrant.enabled) await setFocusGrant(true);
    await enabled.set(false); expect((await api(enabled.admin, 'getAdminUserAccess', [enabled.me.id])).moduleGrants.find((g: any) => g.code === 'FX19').enabled).toBe(false);
    expect((await response(page, '/api/v1/time/entries/' + entry.id, 'GET')).status).toBe(403); expect(sql('TimeEntry', entry.id)).toEqual(snapshot);
    await enabled.set(true); expect((await api(enabled.admin, 'getAdminUserAccess', [enabled.me.id])).moduleGrants.find((g: any) => g.code === 'FX19').enabled).toBe(true);
    if (!focusGrant.enabled) await setFocusGrant(false);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } finally { await enabled.close(); }
});
