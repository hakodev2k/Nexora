import type { Browser, Page } from '@playwright/test';
import { writeFileSync } from 'node:fs';
import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

test.use({ actionTimeout: 15000 });
export const automationRoot = '/api/v1/automation/definitions';
export async function client(page: Page, file: 'automationApi' | 'integrationApi', name: string, args: unknown[] = []): Promise<any> {
  return page.evaluate(async ({ file, name, args }) => { const module = await import('/src/' + file + '.ts'); return module[name](...args); }, { file, name, args });
}
// Only already-installed Ready modules; no permission metadata, roles, provider
// switches or runtime execution flags are modified by this configuration helper.
export async function configured(browser: Browser, page: Page, codes: string[], scenario: () => Promise<void>) {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const operator = await context.newPage(); let before: any, owner: any, scenarioError: unknown; const policies: any[] = [];
  const access = () => api(operator, 'getAdminUserAccess', [owner.id]);
  async function grant(moduleId: string, enabled: boolean) {
    const current = await access(); if (current.moduleGrants.find((g: any) => g.moduleId === moduleId)?.enabled === enabled) return;
    const changes = [{ moduleId, enabled }], preview = await api(operator, 'previewAdminAccess', [owner.id, { kind: 'modules', changes }]);
    expect(preview.blockers).toEqual([]); await api(operator, 'commitAdminModuleGrant', [owner.id, preview.etag, changes, preview.previewToken]);
  }
  async function policy(original: any, enabled: boolean) {
    const current = (await api(operator, 'listAdminModules')).items.find((m: any) => m.id === original.id);
    if (current.systemEnabled === enabled && current.registrationEnabled === original.registrationEnabled) return;
    const change = { systemEnabled: enabled, registrationEnabled: original.registrationEnabled };
    const preview = await api(operator, 'previewModulePolicy', [original.id, change]); expect(preview.blockers).toEqual([]);
    await api(operator, 'commitModulePolicy', [original.id, preview.etag, change, preview.previewToken]);
  }
  try {
    await page.goto('/'); owner = await api(page, 'getMe'); await login(operator, 'SuperAdmin'); before = await access();
    const catalog = await api(operator, 'listAdminModules');
    for (const code of codes) { const module = catalog.items.find((m: any) => m.code === code); expect(module?.state).toBe('Ready'); policies.push(module); }
    // Persist only synthetic access dispositions before the first mutation so an
    // interrupted runtime can be reconciled without credentials/browser storage.
    writeFileSync(test.info().outputPath('local-configuration-access-before.json'), JSON.stringify({
      runId: process.env.NEXORA_E2E_RUN_ID, userId: owner.id,
      modules: policies.map(module => ({ id: module.id, code: module.code, state: module.state,
        systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled,
        userEnabled: before.moduleGrants.find((grant: any) => grant.moduleId === module.id)?.enabled })),
    }, null, 2));
    for (const module of policies) { await policy(module, true); await grant(module.id, true); }
    await scenario();
  } catch (error) { scenarioError = error; throw error; }
  finally {
    const cleanup: unknown[] = [];
    if (before) {
      for (const module of [...policies].reverse()) try { await grant(module.id, before.moduleGrants.find((g: any) => g.moduleId === module.id).enabled); } catch (error) { cleanup.push(error); }
      for (const module of [...policies].reverse()) try { await policy(module, module.systemEnabled); } catch (error) { cleanup.push(error); }
      try {
        const restored = await access(); expect(restored.user.role).toBe(before.user.role); expect(restored.moduleGrants).toEqual(before.moduleGrants); expect(restored.actionGrants).toEqual(before.actionGrants);
        const catalog = await api(operator, 'listAdminModules');
        for (const original of policies) { const current = catalog.items.find((m: any) => m.id === original.id); expect([current.state, current.systemEnabled, current.registrationEnabled]).toEqual([original.state, original.systemEnabled, original.registrationEnabled]); }
      } catch (error) { cleanup.push(error); }
    }
    try { await context.close(); } catch (error) { cleanup.push(error); }
    if (cleanup.length) throw new AggregateError([...(scenarioError ? [scenarioError] : []), ...cleanup], 'Local configuration authority restoration failed');
  }
}
export const workflowCard = (page: Page, name: string) => page.locator('article.resource-card').filter({ has: page.getByRole('heading', { name, exact: true }) });
export async function screenshot(page: Page, name: string) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: test.info().outputPath(name + '.png'), fullPage: true });
}
export async function workflow(page: Page) {
  const title = unique('Local workflow'), taskTitle = unique('Local task');
  const project = await api(page, 'createProject', [unique('Workflow project'), 'Synthetic local workflow prerequisite', '2028-01-01T00:00:00Z', '2028-01-10T00:00:00Z']);
  await page.goto('/modules/FX34');
  const projectRead = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/projects' && r.request().method() === 'GET');
  await page.getByRole('button', { name: 'Workflow mới', exact: true }).click(); expect((await projectRead).status()).toBe(200);
  const dialog = page.getByRole('dialog', { name: 'Workflow mới', exact: true });
  await dialog.getByLabel('Tên workflow', { exact: true }).fill(title);
  const picker = dialog.getByLabel('Project', { exact: true });
  await expect(picker).toBeEnabled();
  for (let n = 0; n < 100 && !(await picker.locator('option').evaluateAll((items, id) => items.some(item => (item as HTMLOptionElement).value === id), project.id)); n++) {
    const next = dialog.getByRole('button', { name: '25 Projects tiếp', exact: true }); await expect(next).toBeEnabled();
    const loaded = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/projects' && r.request().method() === 'GET'); await next.click(); expect((await loaded).status()).toBe(200); await expect(picker).toBeEnabled();
  }
  await picker.selectOption(project.id); await dialog.getByLabel('Task title', { exact: true }).fill(taskTitle);
  await dialog.getByLabel('Bắt đầu', { exact: true }).fill('2028-01-03T10:00'); await dialog.getByLabel('Kết thúc', { exact: true }).fill('2028-01-04T10:00');
  const created = page.waitForResponse(r => new URL(r.url()).pathname === automationRoot + '/' && r.request().method() === 'POST');
  await dialog.getByRole('button', { name: 'Lưu version', exact: true }).click(); const result = await created; expect(result.status()).toBe(201); const ack = await result.json();
  await expect(dialog).toHaveCount(0); await expect(workflowCard(page, title)).toBeVisible(); return { id: ack.id as string, title, taskTitle, project };
}
