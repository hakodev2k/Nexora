import { test, expect, sql, confirm, sqlFocusCompletion } from './fixtures';
import { moduleApi, response, enableForTest } from './time-focus-helpers';

test('FN-042 Focus: SQL preferences, one active slot, pause/reload, cancel, real offline completion', async ({ page, browser, context }) => {
  test.setTimeout(180000); await page.goto('/'); const enabled = await enableForTest(browser, page, 'FX19');
  let original: any;
  try {
    original = await moduleApi(page, 'focusApi', 'focusPreferences'); const existing = await moduleApi(page, 'focusApi', 'focusList', [true]);
    if (existing.items.length) await moduleApi(page, 'focusApi', 'focusTransition', [existing.items[0], 'cancel', crypto.randomUUID()]);
    await page.goto('/modules/FX19'); await expect(page.getByRole('heading', { name: 'Focus', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Edit focus preferences', exact: true }).click(); await page.getByRole('dialog').getByLabel('Focus minutes', { exact: true }).fill('1'); await confirm(page, 'Save preferences');
    await page.getByRole('button', { name: 'Start phase', exact: true }).click(); await expect(page.getByRole('button', { name: 'Pause', exact: true })).toBeEnabled();
    let session = (await moduleApi(page, 'focusApi', 'focusList', [true])).items[0]; expect(sql('FocusSession', session.id)[0].PlannedSeconds).toBe(60); expect(sql('FocusSession', session.id)[0].OwnerId.toLowerCase()).toBe(enabled.me.personalSpaceId.toLowerCase());
    expect((await response(page, '/api/v1/focus/sessions', 'POST', { phase: 'Focus' })).status).toBe(409);
    await page.getByRole('button', { name: 'Pause', exact: true }).click(); await expect(page.getByRole('button', { name: 'Resume', exact: true })).toBeEnabled(); session = (await moduleApi(page, 'focusApi', 'focusList', [true])).items[0]; const elapsed = session.elapsedMilliseconds;
    await page.reload(); await expect(page.getByRole('button', { name: 'Resume', exact: true })).toBeEnabled(); expect((await moduleApi(page, 'focusApi', 'focusList', [true])).items[0].elapsedMilliseconds).toBe(elapsed);
    await page.getByRole('button', { name: 'Resume', exact: true }).click(); await expect(page.getByRole('button', { name: 'Pause', exact: true })).toBeEnabled();
    await page.getByRole('button', { name: 'Cancel phase', exact: true }).click(); await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click(); expect(sql('FocusSession', session.id)[0].State).toBe('Running');
    await page.getByRole('button', { name: 'Cancel phase', exact: true }).click(); await confirm(page, 'Confirm cancel'); expect(sql('FocusSession', session.id)[0].State).toBe('Cancelled');
    await page.getByRole('button', { name: 'Start phase', exact: true }).click(); await expect(page.getByRole('button', { name: 'Pause', exact: true })).toBeEnabled(); session = (await moduleApi(page, 'focusApi', 'focusList', [true])).items[0];
    await context.setOffline(true); await expect.poll(() => sql('FocusSession', session.id)[0].State, { timeout: 80000, intervals: [1000] }).toBe('Completed');
    expect(sql('FocusSession', session.id)[0].ElapsedMilliseconds).toBe(60000); const notifications = sqlFocusCompletion(session.id); expect(notifications).toHaveLength(1); expect(notifications[0].OwnerUserId.toLowerCase()).toBe(enabled.me.id.toLowerCase()); expect(notifications[0].deliveries.map((d: any) => [d.Channel, d.State])).toEqual([['BrowserPush', 'PermissionUnavailable'], ['Email', 'NotApplicable'], ['InApp', 'Delivered']]); await context.setOffline(false); await page.reload(); await expect(page.getByRole('button', { name: 'Start phase', exact: true })).toBeEnabled(); expect((await moduleApi(page, 'focusApi', 'focusList', [true])).items).toEqual([]);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } finally {
    await context.setOffline(false);
    if (original) { const current = await moduleApi(page, 'focusApi', 'focusPreferences'); await moduleApi(page, 'focusApi', 'focusSavePreferences', [{ ...original, etag: current.etag }, crypto.randomUUID()]); }
    await enabled.close();
  }
});
