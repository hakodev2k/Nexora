import { test, expect, api, sql } from './fixtures';
import { randomUUID } from 'node:crypto';
import { startedAccess } from './started-flow-access';

// FX26-S02 / BR004,005; NXG-FX26-G03. Real UI/API/SQL, no provider mocks.
test('Dashboard layout: keyboard reorder, persistence, stale revision and native create handoff', async ({ page, browser }) => {
  test.setTimeout(120000);
  await page.goto('/');
  const access = await startedAccess(browser, page, ['FX26', 'FX11']);
  const original = (await api(page, 'getDashboardLayout')).layout;
  let failure: unknown;
  try {
    await page.reload();
    const editor = page.getByRole('region', { name: 'Bố cục Dashboard', exact: true });
    await expect(editor.getByRole('button', { name: 'Lưu bố cục', exact: true })).toBeEnabled();
    const beforeKeys = await editor.getByLabel('Nguồn', { exact: true }).evaluateAll(nodes => nodes.map(n => (n as HTMLSelectElement).value));
    expect(beforeKeys.length).toBeGreaterThan(1);
    const move = editor.getByRole('button', { name: 'Đưa widget 1 xuống', exact: true });
    await move.focus(); await page.keyboard.press('Enter');
    await editor.getByLabel('Kích thước', { exact: true }).first().selectOption('2');
    await editor.getByLabel('Hiển thị', { exact: true }).first().uncheck();
    await editor.getByRole('button', { name: 'Lưu bố cục', exact: true }).click();
    await expect.poll(async () => (await api(page, 'getDashboardLayout')).layout?.widgets[0]?.key).toBe(beforeKeys[1]);
    const saved = (await api(page, 'getDashboardLayout')).layout;
    expect(saved.widgets[0]).toMatchObject({ widthUnits: 2, enabled: false });
    expect(sql('Dashboard', saved.id)[0].OwnerId.toLowerCase()).toBe(access.me.personalSpaceId.toLowerCase());
    expect(sql('DashboardWidget', saved.widgets[0].id)[0].OwnerId.toLowerCase()).toBe(access.me.personalSpaceId.toLowerCase());
    await page.reload(); await expect(editor.getByLabel('Kích thước', { exact: true }).first()).toHaveValue('2');
    await expect(editor.getByLabel('Hiển thị', { exact: true }).first()).not.toBeChecked();
    // A separate legitimate command advances the revision behind the open editor.
    await api(page, 'saveDashboardLayout', [saved, saved.widgets.map((w: any) => ({ ...w, enabled: true })), randomUUID()]);
    await editor.getByLabel('Kích thước', { exact: true }).first().selectOption('1');
    const stale = page.waitForResponse(r => r.url().endsWith('/api/v1/dashboard/layout') && r.request().method() === 'PUT');
    await editor.getByRole('button', { name: 'Lưu bố cục', exact: true }).click();
    expect((await stale).status()).toBe(412);
    await expect(editor.getByLabel('Kích thước', { exact: true }).first()).toHaveValue('1');
    expect((await api(page, 'getDashboardLayout')).layout.widgets[0].widthUnits).toBe(2);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: test.info().outputPath('dashboard-stale-layout.png'), fullPage: true });
    // Reload explicitly discards only this test-owned stale draft.
    page.once('dialog', dialog => void dialog.accept()); await page.reload();
    const opener = page.getByRole('button', { name: 'Tạo Project', exact: true });
    await expect(opener).toBeEnabled();
    const handoff = page.waitForResponse(r => r.url().endsWith('/api/v1/dashboard/quick-create/Project'));
    await opener.click(); expect((await handoff).status()).toBe(200);
    await expect(page.getByRole('heading', { name: 'Projects', exact: true })).toBeVisible();
    // The native required form stays responsible for creation; handoff is read-only.
    await expect(page.getByRole('heading', { name: 'Tạo Project', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Tạo Project', exact: true })).toBeVisible();
    await page.screenshot({ path: test.info().outputPath('dashboard-native-project.png'), fullPage: true });
  } catch (error) { failure = error; }
  finally {
    const cleanupErrors: unknown[] = [];
    try {
      const current = (await api(page, 'getDashboardLayout')).layout;
      if (current) {
        const widgets = original?.widgets ?? current.widgets.map((w: any) => ({ ...w, widthUnits: 1, enabled: true }));
        await api(page, 'saveDashboardLayout', [current, widgets, randomUUID()]);
      }
    } catch (error) { cleanupErrors.push(error); }
    try { await access.close(); } catch (error) { cleanupErrors.push(error); }
    if (failure || cleanupErrors.length) throw new AggregateError([...(failure ? [failure] : []), ...cleanupErrors], 'Dashboard scenario or restoration failed.');
  }
});
