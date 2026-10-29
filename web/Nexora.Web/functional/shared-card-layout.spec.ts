import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect, api, login, sql, unique, card, showPagedCard } from './fixtures';
import type { Browser, Locator, Page } from '@playwright/test';

async function enableLayoutModules(browser: Browser, page: Page) {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const operator = await context.newPage(); await login(operator, 'SuperAdmin');
  const me = await api(page, 'getMe'), before = await api(operator, 'getAdminUserAccess', [me.id]);
  const codes = ['FX18', 'FX40'];
  const modules = (await api(operator, 'listAdminModules')).items.filter((m: any) => codes.includes(m.code));
  expect(modules).toHaveLength(2); for (const module of modules) expect(module.state).toBe('Ready');
  const grants = before.moduleGrants.filter((g: any) => codes.includes(g.code)); expect(grants).toHaveLength(2);
  async function policy(module: any, enabled: boolean) {
    const current = (await api(operator, 'listAdminModules')).items.find((m: any) => m.id === module.id);
    if (current.systemEnabled === enabled && current.registrationEnabled === module.registrationEnabled) return;
    const change = { systemEnabled: enabled, registrationEnabled: module.registrationEnabled };
    const preview = await api(operator, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(operator, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  }
  async function grant(restore: boolean) {
    const current = await api(operator, 'getAdminUserAccess', [me.id]);
    const changes = grants.map((g: any) => ({ moduleId: g.moduleId, enabled: restore ? g.enabled : true }))
      .filter((g: any) => current.moduleGrants.find((m: any) => m.moduleId === g.moduleId)?.enabled !== g.enabled);
    if (!changes.length) return;
    const preview = await api(operator, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(operator, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  }
  const close = async () => {
    const errors: unknown[] = [];
    const attempt = async (action: () => Promise<void>) => { try { await action(); } catch (e) { errors.push(e); } };
    await attempt(() => grant(true));
    for (const module of [...modules].reverse()) await attempt(() => policy(module, module.systemEnabled));
    await attempt(async () => {
      const restored = await api(operator, 'getAdminUserAccess', [me.id]); expect(restored.user.role).toBe(before.user.role);
      for (const g of grants) expect(restored.moduleGrants.find((m: any) => m.moduleId === g.moduleId)?.enabled).toBe(g.enabled);
      const current = (await api(operator, 'listAdminModules')).items;
      for (const module of modules) {
        const row = current.find((m: any) => m.id === module.id);
        expect(row.state).toBe(module.state); expect(row.systemEnabled).toBe(module.systemEnabled); expect(row.registrationEnabled).toBe(module.registrationEnabled);
      }
    });
    await context.close(); if (errors.length) throw new AggregateError(errors, 'Layout fixture authority restoration failed.');
  };
  try { for (const module of modules) await policy(module, true); await grant(false); }
  catch (error) { try { await close(); } catch (restore) { throw new AggregateError([error, restore], 'Layout fixture setup and restoration failed.'); } throw error; }
  return { me, close };
}

async function measureCard(target: Locator) {
  await expect(target).toBeVisible(); await target.scrollIntoViewIfNeeded();
  const result = await target.evaluate(el => {
    const heading = el.querySelector('h2,h3')!, body = el.querySelector('p')!;
    const box = el.getBoundingClientRect(), h = heading.getBoundingClientRect(), p = body.getBoundingClientRect();
    const hit = document.elementFromPoint(h.left + h.width / 2, h.top + h.height / 2);
    return { width: box.width, headingWidth: h.width, bodyWidth: p.width, overflow: el.scrollWidth > el.clientWidth + 1,
      headingUnobscured: hit === heading || (hit !== null && heading.contains(hit)),
      headingHeight: h.height, lineHeight: parseFloat(getComputedStyle(heading).lineHeight) };
  });
  expect(result.overflow).toBe(false);
  expect(result.headingUnobscured, 'The visible title must not be covered by a sticky filter or action').toBe(true);
  expect(result.headingWidth / result.width, 'Title receives a readable content column, not a squeezed flex sliver').toBeGreaterThan(.75);
  expect(result.bodyWidth / result.width, 'Metadata receives a readable content column').toBeGreaterThan(.75);
  expect(result.headingHeight, 'Representative title must not wrap one character per line').toBeLessThanOrEqual(result.lineHeight * 3 + 1);
  const actions = await target.getByRole('button').evaluateAll(nodes => nodes.map(el => el.getBoundingClientRect().height));
  expect(actions.length).toBeGreaterThan(0); for (const height of actions) expect(height, 'Visible card actions retain comfortable touch targets').toBeGreaterThanOrEqual(44);
  return result;
}

test('Shared card layout: populated Time and Course preserve readable content and actions', async ({ page, browser }) => {
  test.setTimeout(180_000); page.setDefaultTimeout(15_000); await page.goto('/');
  const enabled = await enableLayoutModules(browser, page), width = page.viewportSize()!.width;
  const folder = join('artifacts', 'shared-card-layout', String(width)); mkdirSync(folder, { recursive: true });
  const results: unknown[] = []; let scenarioError: unknown;
  try {
    const timeTitle = unique('Readable Time card'), courseTitle = unique('Readable Course card');
    await page.goto('/modules/FX18');
    await expect(page.getByRole('button', { name: 'Start timer', exact: true })).toBeVisible();
    expect(await page.getByRole('button', { name: 'Start timer', exact: true }).evaluate(el => el.getBoundingClientRect().height)).toBeGreaterThanOrEqual(44);
    await page.getByRole('button', { name: 'Manual entry', exact: true }).click();
    let modal = page.getByRole('dialog'); await modal.getByLabel('Description', { exact: true }).fill(timeTitle);
    await modal.getByLabel('Category', { exact: true }).fill('Synthetic layout verification');
    await modal.getByLabel('Start (múi giờ trình duyệt)', { exact: true }).fill('2026-10-20T09:00');
    await modal.getByLabel('End (múi giờ trình duyệt)', { exact: true }).fill('2026-10-20T09:15');
    await modal.getByRole('checkbox').check();
    const timeWrite = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/time/entries' && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Save', exact: true }).click();
    const timeResponse = await timeWrite; expect(timeResponse.status()).toBe(201); const entry = await timeResponse.json();
    const timeRows = sql('TimeEntry', entry.id); expect(timeRows).toHaveLength(1); expect(timeRows[0].OwnerId.toLowerCase()).toBe(enabled.me.personalSpaceId.toLowerCase());
    await expect(modal).toHaveCount(0); await showPagedCard(page, timeTitle, 'Load more entries');
    const timeCard = card(page, timeTitle); results.push({ module: 'Time', ...await measureCard(timeCard) });
    await expect(timeCard.getByRole('button', { name: 'Edit entry', exact: true })).toBeVisible();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    await page.screenshot({ path: join(folder, 'time-populated.png'), fullPage: false });

    await page.goto('/modules/FX40'); await page.getByRole('button', { name: 'Courses', exact: true }).click();
    await page.getByRole('button', { name: 'New Course', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Title', { exact: true }).fill(courseTitle);
    await modal.getByLabel('Progress mode', { exact: true }).selectOption('ManualPercent');
    await modal.getByLabel('Provider', { exact: true }).fill('Synthetic local learning');
    const courseWrite = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/learning/courses' && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Save Course', exact: true }).click();
    const courseResponse = await courseWrite; expect(courseResponse.status()).toBe(201); const ack = await courseResponse.json();
    const courseRows = sql('Course', ack.itemId); expect(courseRows).toHaveLength(1); expect(courseRows[0].OwnerId.toLowerCase()).toBe(enabled.me.personalSpaceId.toLowerCase());
    await expect(modal).toHaveCount(0); await showPagedCard(page, courseTitle, 'Load more Course items');
    const courseCard = card(page, courseTitle); results.push({ module: 'Course', ...await measureCard(courseCard) });
    expect(await page.getByLabel('Title search', { exact: true }).evaluate(el => el.getBoundingClientRect().height)).toBeGreaterThanOrEqual(44);
    expect(await page.getByLabel('Status', { exact: true }).evaluate(el => el.getBoundingClientRect().height)).toBeGreaterThanOrEqual(44);
    await expect(courseCard.getByRole('button', { name: 'Edit Course', exact: true })).toBeVisible();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(true);
    await page.screenshot({ path: join(folder, 'course-populated.png'), fullPage: false });
    writeFileSync(join(folder, 'layout-measurements.json'), JSON.stringify({ testedSha: process.env.GITHUB_SHA ?? 'WORKTREE', width, results, scope: 'Selected real Time/Course card layout, not complete module acceptance.' }, null, 2));
  } catch (error) { scenarioError = error; throw error; }
  finally { try { await enabled.close(); } catch (restore) { throw new AggregateError([...(scenarioError ? [scenarioError] : []), restore], 'Layout fixture restoration failed.'); } }
});
