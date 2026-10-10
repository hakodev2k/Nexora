import { test, expect, api, sqlSession, login, unique, confirm } from './fixtures';

test('FN-033 Sessions: cancel/confirm revoke other, foreign owner denial, current revoke logs out', async ({ page, browser }, worker) => {
  const label = unique('session-other');
  const other = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: worker.project.use.viewport, userAgent: label });
  const foreignLabel = unique('session-user-b');
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: worker.project.use.viewport, userAgent: foreignLabel });
  let otherId: string | undefined, foreignId: string | undefined;
  try {
    const otherPage = await other.newPage(); await login(otherPage);
    otherId = (await api(otherPage, 'listSessions')).items.find((s: any) => s.isCurrent).id;
    const me = await api(otherPage, 'getMe'); const before = sqlSession(otherId!)[0];
    expect(before.UserId.toLowerCase()).toBe(me.id.toLowerCase()); expect(before.RevokedAt).toBeNull();
    await page.goto('/settings/security'); const row = page.getByRole('row').filter({ hasText: label });
    await expect(row).toBeVisible(); await row.getByRole('button', { name: 'Thu hồi', exact: true }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click();
    expect(sqlSession(otherId!)[0]).toEqual(before); expect((await api(otherPage, 'getMe')).id).toBe(me.id);
    await row.getByRole('button', { name: 'Thu hồi', exact: true }).click(); await confirm(page, 'Thu hồi');
    await expect.poll(() => Boolean(sqlSession(otherId!)[0].RevokedAt)).toBe(true); await expect(row).toHaveCount(0);
    expect(await otherPage.evaluate(async () => (await fetch('/api/v1/me')).status)).toBe(401);
    expect((await api(page, 'getMe')).id).toBe(me.id);
    const foreignPage = await foreign.newPage(); await login(foreignPage, 'UserB');
    foreignId = (await api(foreignPage, 'listSessions')).items.find((s: any) => s.isCurrent).id;
    const foreignBefore = sqlSession(foreignId!)[0];
    expect((await api(page, 'listSessions')).items.some((s: any) => s.id === foreignId)).toBe(false);
    const denied = await page.evaluate(async id => { const path = '/src/api.ts'; const a = await import(path); try { await a.revokeSession(id); return 204; } catch (e: any) { return e.status; } }, foreignId!);
    expect(denied).toBe(404); expect(sqlSession(foreignId!)[0]).toEqual(foreignBefore);
    await foreignPage.goto('/settings/security'); const ownRow = foreignPage.getByRole('row').filter({ hasText: foreignLabel });
    await ownRow.getByRole('button', { name: 'Thu hồi', exact: true }).click(); await confirm(foreignPage, 'Thu hồi');
    await expect(foreignPage).toHaveURL(/\/login/); expect(sqlSession(foreignId!)[0].RevokedAt).not.toBeNull();
    expect(await foreignPage.evaluate(async () => (await fetch('/api/v1/me')).status)).toBe(401);
    expect((await api(page, 'getMe')).id).toBe(me.id);
  } finally {
    // Cleanup only sessions created by this workflow; no revoke-all of the shared fixture.
    if (otherId && !sqlSession(otherId)[0].RevokedAt) await api(page, 'revokeSession', [otherId]);
    if (foreignId && !sqlSession(foreignId)[0].RevokedAt) { const p = foreign.pages()[0]; await api(p, 'revokeSession', [foreignId]); }
    await other.close(); await foreign.close();
  }
});
