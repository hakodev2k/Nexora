import { mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect, api } from './fixtures';
import { response } from './time-focus-helpers';

// Negative runtime gate only: never enable FX05, inject grants or simulate a
// successful consent. Positive consent/recovery remains separately BLOCKED.
test('Support gate: owner sees unavailable route and cannot create consent', async ({ page }, info) => {
  await page.goto('/');
  await expect(page.locator('.app-shell')).toBeVisible();
  const before = await api(page, 'getMe');
  const module = before.modules.find((m: any) => m.code === 'FX05');
  expect(module, 'The unavailable module stays explicit in owner capabilities').toBeTruthy();
  expect(module.enabled).toBe(false);
  await expect(page.locator('.sidebar').getByRole('button', { name: 'Support access', exact: true })).toHaveCount(0);
  await page.goto('/support');
  await expect(page.locator('#module-unavailable-title')).toBeVisible();
  await expect(page.locator('main')).toContainText('FX05');
  await expect(page.getByRole('button', { name: 'Cấp support consent', exact: true })).toHaveCount(0);
  await expect(page.locator('main form')).toHaveCount(0);
  const key = crypto.randomUUID();
  const command = { moduleCode: 'FX11', durationMode: '24Hours', expiresAt: null };
  for (let attempt = 0; attempt < 2; attempt++) {
    const denied = await response(page, '/api/v1/support/grants', 'POST', command, undefined, key);
    expect(denied.status).toBe(409); expect(denied.body.code).toBe('ModuleUnavailable');
    expect(denied.body).not.toHaveProperty('id'); expect(denied.body).not.toHaveProperty('committed');
  }
  for (const kind of ['grants', 'sessions']) {
    const denied = await response(page, `/api/v1/support/${kind}`, 'GET');
    expect(denied.status).toBe(409); expect(denied.body.code).toBe('ModuleUnavailable');
    expect(denied.body).not.toHaveProperty('items');
  }
  await expect(page.locator('#module-unavailable-title')).toBeVisible();
  await expect(page.getByText('Support consent đã được cấp.', { exact: true })).toHaveCount(0);
  expect((await api(page, 'getMe')).modules.find((m: any) => m.code === 'FX05').enabled).toBe(false);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  const folder = join('artifacts', 'support-gate-20261027'); mkdirSync(folder, { recursive: true });
  await page.screenshot({ path: join(folder, `${info.project.name}-unavailable.png`), fullPage: false });
});
