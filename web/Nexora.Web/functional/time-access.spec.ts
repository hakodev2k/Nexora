import { test, expect, api, login, sql } from './fixtures';
import { enableForTest, moduleApi, response } from './time-focus-helpers';

test('FN-043 Time access: Admin SELF grants, read-only UI, timer separation and foreign-owner 404', async ({ page, browser }) => {
  test.setTimeout(180000); await page.goto('/'); const owner = await enableForTest(browser, page, 'FX18');
  const adminContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }), foreignContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const adminPage = await adminContext.newPage(), foreignPage = await foreignContext.newPage();
  const saved: { user: string; module: string; enabled: boolean }[] = []; let adminId: string | undefined; const original = new Map<string,string>();
  async function setModule(user: string, module: string, enabled: boolean) { const changes = [{ moduleId: module, enabled }]; const p = await api(owner.admin, 'previewAdminAccess', [user, { kind: 'modules', changes }]); expect(p.blockers).toEqual([]); await api(owner.admin, 'commitAdminModuleGrant', [user, p.etag, changes, p.previewToken]); }
  async function setGrant(actionKey: string, effect: string) { const current = await api(owner.admin, 'getAdminUserAccess', [adminId]); if ((current.actionGrants.find((g: any) => g.actionKey === actionKey)?.effect ?? 'Unset') === effect) return; const changes = [{ actionKey, effect }]; const p = await api(owner.admin, 'previewAdminAccess', [adminId, { kind: 'permissions', changes }]); expect(p.blockers).toEqual([]); await api(owner.admin, 'commitAdminActionGrant', [adminId, p.etag, changes, p.previewToken]); }
  try {
    await login(adminPage, 'Admin'); await login(foreignPage, 'UserB'); adminId = (await api(adminPage, 'getMe')).id;
    for (const p of [adminPage, foreignPage]) { const user = (await api(p, 'getMe')).id, access = await api(owner.admin, 'getAdminUserAccess', [user]); const g = access.moduleGrants.find((g: any) => g.code === 'FX18'); saved.push({ user, module: g.moduleId, enabled: g.enabled }); if (!g.enabled) await setModule(user, g.moduleId, true); }
    const access = await api(owner.admin, 'getAdminUserAccess', [adminId]);
    for (const action of ['time.entry.read','time.entry.create','time.timer.read','time.report.read']) { original.set(action, access.actionGrants.find((g:any) => g.actionKey === action)?.effect ?? 'Unset'); await setGrant(action, 'Unset'); }
    expect((await response(adminPage, '/api/v1/time/entries', 'GET')).status).toBe(403);
    await setGrant('time.entry.read', 'Allow'); expect((await response(adminPage, '/api/v1/time/entries', 'GET')).status).toBe(200);
    expect((await response(adminPage, '/api/v1/time/timer', 'GET')).status).toBe(403); expect((await response(adminPage, '/api/v1/time/report', 'GET')).status).toBe(403);
    await adminPage.goto('/modules/FX18'); await expect(adminPage.getByRole('heading', { name: 'Time Tracking', exact: true })).toBeVisible(); await expect(adminPage.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); await expect(adminPage.getByRole('button', { name: 'Manual entry', exact: true })).toBeDisabled(); await expect(adminPage.getByRole('heading', { name: 'Timer', exact: true })).toHaveCount(0); await expect(adminPage.getByRole('alert')).toHaveCount(0);
    const body = { startAt: '2001-01-01T00:00:00Z', endAt: '2001-01-01T01:00:00Z', description: 'Owner isolation fixture', category: null, confirmOverlap: true };
    expect((await response(adminPage, '/api/v1/time/entries', 'POST', body)).status).toBe(403);
    await setGrant('time.entry.read', 'Deny'); expect((await response(adminPage, '/api/v1/time/entries', 'GET')).status).toBe(403);
    await setGrant('time.entry.read', 'Unset'); expect((await response(adminPage, '/api/v1/time/entries', 'GET')).status).toBe(403);
    const entry = await moduleApi(page, 'timeApi', 'timeSave', [body, crypto.randomUUID()]); const before = sql('TimeEntry', entry.id);
    expect((await response(foreignPage, '/api/v1/time/entries/' + entry.id, 'GET')).status).toBe(404);
    expect((await response(foreignPage, '/api/v1/time/entries/' + entry.id, 'PUT', body, entry.etag)).status).toBe(404);
    expect((await response(foreignPage, '/api/v1/time/entries/' + entry.id + '/history', 'GET')).status).toBe(404); expect(sql('TimeEntry', entry.id)).toEqual(before);
  } finally {
    if (adminId) for (const [action, effect] of original) await setGrant(action, effect);
    for (const grant of saved.reverse()) if (!grant.enabled) await setModule(grant.user, grant.module, false);
    await adminContext.close(); await foreignContext.close(); await owner.close();
  }
});
