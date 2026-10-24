import { test as base, expect, type Page, type BrowserContext } from '@playwright/test';
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import AxeBuilder from '@axe-core/playwright';
export { expect };
export async function login(page: Page, role = 'UserA') {
  // Optional setup pacing respects the unchanged server limit (10 login requests/minute).
  // Workers=1; the private timestamp coordinates ordinary form logins across worker processes.
  const pacing = Number(process.env.NEXORA_E2E_LOGIN_PACING_MS ?? '0');
  if (!Number.isFinite(pacing) || pacing < 0 || pacing > 60_000) throw new Error('Invalid login pacing interval');
  if (pacing) {
    const stamp = process.env.NEXORA_E2E_ACCOUNTS! + '.login-pacing';
    const last = existsSync(stamp) ? Number(readFileSync(stamp, 'utf8')) : 0;
    const wait = Math.max(0, last + pacing - Date.now());
    if (wait) await page.waitForTimeout(Math.min(wait, 60_000));
    writeFileSync(stamp, String(Date.now()), { mode: 0o600 });
  }
  const account = JSON.parse(readFileSync(process.env.NEXORA_E2E_ACCOUNTS!, 'utf8')).find((a: { role: string }) => a.role === role);
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill(account.email);
  await page.getByLabel('Mật khẩu', { exact: true }).fill(account.password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.locator('.app-shell')).toBeVisible();
}
// One normal login per viewport worker; cookies stay in the actual browser context.
// No injected session, storageState, auth bypass, or rate-limit changes.
export const test = base.extend<{}, { session: BrowserContext }>({
  session: [async ({ browser }, use, worker) => {
    const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: worker.project.use.viewport, acceptDownloads: true });
    const page = await context.newPage(); await login(page); await page.close();
    await use(context); await context.close();
  }, { scope: 'worker' }],
  context: async ({ session }, use) => { await session.setOffline(false); await use(session); await session.setOffline(false); },
  page: async ({ context }, use) => { const page = await context.newPage(); await use(page); await page.close(); }
});
export const unique = (prefix: string) => `${prefix}-${randomUUID().slice(0, 8)}`;
export async function api<T = any>(page: Page, name: string, args: unknown[] = []): Promise<T> {
  return page.evaluate(async ({ name, args }) => {
    const apiPath = '/src/api.ts';
    const module = await import(apiPath);
    return module[name](...args);
  }, { name, args });
}
export function sql(kind: string, id: string): any[] {
  return JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ['read-resource', kind, id], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
}
export function sqlProfile(id: string): any[] {
  return JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ['read-profile', id], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
}
export async function record(page: Page, method: string, args: unknown[], kind: string, name: string, field: string = 'title') {
  let item: any;
  await expect.poll(async () => {
    const cursorIndex: Record<string,number> = {listProjects:1,listTasks:2,listCalendarEvents:3};
    const query=[...args];const seen=new Set<string>();
    for(let n=0;n<100;n++){
      const list=await api(page,method,query);item=list.items.find((i:any)=>i[field]===name);
      if(item||!list.nextCursor||cursorIndex[method]===undefined)break;
      if(seen.has(list.nextCursor))throw new Error('Repeated pagination cursor');seen.add(list.nextCursor);query[cursorIndex[method]]=list.nextCursor;
    }
    return Boolean(item);
  }, { message: `${kind} persisted through the real API` }).toBe(true);
  expect(item, `${kind} persisted through the real API`).toBeTruthy();
  const rows = sql(kind, item.id); expect(rows).toHaveLength(1);
  const profile = await api(page, 'getMe'); expect(rows[0].OwnerId.toLowerCase()).toBe(profile.personalSpaceId.toLowerCase());
  return { item, row: rows[0] };
}
export function card(page: Page, name: string) { return page.locator('.resource-card').filter({ has: page.getByRole('heading', { name, exact: true }) }); }
export async function dialog(page: Page, trigger: string) {
  await page.getByRole('button', { name: trigger, exact: true }).click();
  const modal = page.getByRole('dialog'); await expect(modal).toBeVisible();
  expect(await modal.getAttribute('aria-modal')).toBe('true');
  const controls = modal.locator('input:not([disabled]),select:not([disabled]),textarea:not([disabled]),button:not([disabled])');
  await expect(controls.first()).toBeFocused();
  await page.keyboard.press('Shift+Tab'); await expect(controls.last()).toBeFocused();
  await page.keyboard.press('Tab'); await expect(controls.first()).toBeFocused();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  return modal;
}
export async function confirm(page: Page, name: string) { const modal = page.getByRole('dialog'); await expect(modal).toBeVisible(); await modal.getByRole('button', { name, exact: true }).click(); await expect(modal).toHaveCount(0); }
export async function sourceTask(page: Page) {
  const start = new Date(Date.now() + 86400000).toISOString(), end = new Date(Date.now() + 172800000).toISOString();
  const project = await api(page, 'createProject', [unique('source-project'), 'Prerequisite synthetic project', start, end]);
  const task = await api(page, 'createTask', [project.id, unique('source-task'), 'Prerequisite task', 'NotStarted', end, start, end]);
  return { project, task };
}

export function sqlSession(id: string): any[] {
  return JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ['read-session', id], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }));
}

// Follow visible pagination controls; never hide accumulated SQL data to make a case pass.
export async function showPagedCard(page: Page,title:string,loadMoreLabel:string){
 for(let n=0;n<100;n++){
  const target=card(page,title),next=page.getByRole('button',{name:loadMoreLabel,exact:true});
  // isVisible alone does not wait for the initial async list load.
  await expect.poll(async()=>await target.isVisible()||await next.isVisible()).toBe(true);
  if(await target.isVisible())return;
  await expect(next).toBeVisible();
  const before=await page.locator('.resource-card').count();await next.click();
  await expect.poll(()=>page.locator('.resource-card').count()).toBeGreaterThan(before);
 }
 throw new Error('Target not found after 100 visible pages');
}

export function sqlFocusCompletion(id: string): any[] { return JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ["read-focus-completion", id], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] })); }

export function sqlTimeReportSeries(owner: string): any[] { return JSON.parse(execFileSync(process.env.NEXORA_E2E_SQL_OPERATOR!, ["read-time-report-series", owner], { encoding: "utf8", stdio: ["ignore", "pipe", "pipe"] })); }
