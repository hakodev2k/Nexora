/**
 * PR #4 synthetic UI/UX evidence scanner. NOT a business acceptance test or
 * a substitute for a human visual reviewer. All writes use job-owned SQL only.
 * Screenshots contain SYNTHETIC TEST PERSONAS; never run against real users.
 */
import { test, expect, type Page, type TestInfo } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

type Finding = { code: string; severity: 'P1' | 'P2' | 'P3'; detail: string };
type Observation = {
  route: string; persona: string; viewport: string; status: string;
  h1?: string; screenshot?: string; checks?: Record<string, number | boolean | string>;
  dialogs?: Array<{ trigger: string; visible: boolean; screenshot?: string; scrollOverflow?: boolean }>;
  tabs?: Array<{ title: string; accessible: boolean; screenshot?: string }>;
  findings: Finding[];
};
const artifactRoot = join(process.cwd(), 'artifacts', 'uiux-audit');
const screenshotRoot = join(artifactRoot, 'screenshots');
const modules = Array.from({ length: 40 }, (_, i) => '/modules/FX' + String(i + 1).padStart(2, '0'));
const publicRoutes = ['/login', '/register', '/verify-email', '/password/forgot', '/password/reset', '/share/synthetic-invalid-token'];
const visiblePanelTriggers = /^(Thêm|Tạo|Xem|Chi tiết|Lịch sử|Chỉnh sửa|Sửa|Bộ lọc|Tùy chỉnh|Thiết lập|Cấu hình|Add |New |Create |View |Edit |Filter |History |Import |Export )/i;
const prohibitedTriggers = /(Đăng xuất|Lưu|Gửi|Xóa|Purge|Archive|Remove|Delete|Khôi phục|Chạy|Áp dụng|Confirm|Revoke|Phê duyệt|Logout|Save|Run |Submit|Execute|Thực thi|Bật|Tắt)/i;

function safeSlug(value: string) { return value.replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '').slice(0, 86) || 'home'; }
function finding(code: string, severity: 'P1' | 'P2' | 'P3', detail: string): Finding { return { code, severity, detail }; }
async function screenshot(page: Page, persona: string, route: string, suffix: string) {
  mkdirSync(screenshotRoot, { recursive: true });
  const base = safeSlug(persona + '-' + route + '-' + suffix);
  const relative = 'screenshots/' + base + '.png';
  await page.screenshot({
    path: join(artifactRoot, relative), animations: 'disabled', timeout: 10_000,
    fullPage: false, mask: [page.locator('input[type="password"], input[autocomplete="current-password"], input[autocomplete="new-password"]')],
  });
  return relative;
}
async function logIn(page: Page, role: string) {
  const path = process.env.NEXORA_E2E_ACCOUNTS;
  if (!path) throw new Error('Missing isolated synthetic accounts path');
  const accounts = JSON.parse(readFileSync(path, 'utf8')) as Array<{ role: string; email: string; password: string }>;
  const account = accounts.find(a => a.role === role);
  if (!account || !account.email.endsWith('@example.invalid')) throw new Error('Synthetic role unavailable');
  await page.goto('/login');
  await page.getByLabel('Email').fill(account.email);
  await page.getByLabel('Mật khẩu').fill(account.password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL('/', { timeout: 20_000 });
  await expect(page.locator('main')).toBeVisible();
  // Do not include credential/email in report or test logs.
}
async function trySeed(page: Page, records: string[], domain: string) {
  if (domain === 'knowledge') try {
    // Each UI write goes to this run's disposable SQL, not mocked network or production.
    await page.goto('/bookmarks');
    await page.getByRole('button', { name: 'Thêm bookmark' }).click({ timeout: 5_000 });
    const dialog = page.getByRole('dialog').first();
    await dialog.getByLabel('URL', { exact: true }).fill('https://example.invalid/nexora-uiux-bookmark');
    await dialog.getByLabel('Tiêu đề', { exact: true }).fill('Synthetic UIUX Bookmarks item');
    await dialog.getByRole('button', { name: 'Lưu bookmark' }).click();
    await page.waitForTimeout(500);
    records.push('Bookmarks: attempted one synthetic real SQL UI create (observe subsequent list)');
  } catch { records.push('Bookmarks: UI seeding unverified/blocked (do not claim populated)'); }
  if (domain === 'knowledge') try {
    await page.goto('/modules/FX22');
    await page.getByLabel('Tiêu đề', { exact: true }).fill('Synthetic UIUX snippet');
    await page.getByLabel('Language', { exact: true }).fill('csharp');
    await page.getByLabel('Source code / text', { exact: true }).fill('public static int Add(int a, int b) => a + b;');
    await page.getByRole('button', { name: 'Tạo snippet', exact: true }).click();
    await expect(page.locator('.snippet-code code')).toBeVisible({ timeout: 5_000 });
    records.push('Snippets: synthetic real SQL UI creation confirmed');
  } catch { records.push('Snippets: UI seeding unverified/blocked'); }
  if (domain === 'productivity') try {
    await page.goto('/modules/FX11');
    await page.locator('#project-name').fill('Synthetic UIUX Project');
    await page.locator('#project-description').fill('Synthetic isolated QA project for populated screenshots');
    await page.locator('#project-start').fill('2026-10-20T09:00');
    await page.locator('#project-end').fill('2026-11-20T18:00');
    await page.getByRole('button', { name: 'Tạo Project', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Synthetic UIUX Project' })).toBeVisible({ timeout: 5_000 });
    records.push('Projects: synthetic real SQL UI creation confirmed');
  } catch { records.push('Projects: UI seeding unverified/blocked'); }
  if (domain === 'productivity') try {
    await page.goto('/modules/FX12');
    await page.locator('#task-project').selectOption({ label: 'Synthetic UIUX Project' });
    await page.locator('#task-title').fill('Synthetic UIUX Task');
    await page.locator('#task-start').fill('2026-10-21T09:00');
    await page.locator('#task-end').fill('2026-10-22T18:00');
    await page.getByRole('button', { name: 'Tạo Task', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Synthetic UIUX Task' })).toBeVisible({ timeout: 5_000 });
    records.push('Tasks: synthetic real SQL UI creation confirmed');
  } catch { records.push('Tasks: UI seeding unverified/blocked'); }
  if (domain === 'business') try {
    await page.goto('/finance');
    await page.getByRole('button', { name: 'Thêm category' }).click();
    await page.locator('#finance-category-title').fill('Synthetic UIUX category');
    await page.getByRole('dialog').getByRole('button', { name: 'Tạo category' }).click();
    await page.getByRole('button', { name: 'Thêm record' }).click();
    await page.locator('#finance-record-category').selectOption({ label: 'Synthetic UIUX category' });
    await page.locator('#finance-record-amount').fill('123456.125');
    await page.locator('#finance-record-currency').fill('VND');
    await page.locator('#finance-record-date').fill('2026-10-19');
    await page.getByRole('dialog').getByRole('button', { name: 'Tạo record' }).click();
    await expect(page.getByText('123456.125', { exact: false }).first()).toBeVisible({ timeout: 5_000 });
    records.push('Finance: synthetic category plus explicit-VND real SQL record confirmed');
  } catch { records.push('Finance: category/record UI seeding unverified/blocked'); }
  if (domain === 'knowledge') try {
    const op = process.env.NEXORA_E2E_SQL_OPERATOR;
    const state = await page.request.get('/api/v1/me');
    const profile = await state.json() as { id?: string };
    if (!op || !state.ok() || !/^[a-f0-9-]{36}$/i.test(profile.id ?? '')) throw Error('Synthetic owner identity not ready');
    execFileSync(op, ['seed-notification', profile.id!, 'Synthetic UX Notification'], { encoding: 'utf8', timeout: 15_000, stdio: ['ignore', 'pipe', 'pipe'] });
    records.push('Notifications: local SQL synthetic notification with no provider dispatch');
  } catch { records.push('Notifications: synthetic SQL fixture unverified/blocked'); }
}
async function observe(page: Page, route: string, persona: string, testInfo: TestInfo): Promise<Observation> {
  const out: Observation = { route, persona, viewport: testInfo.project.name, status: 'NOT_RUN', dialogs: [], tabs: [], findings: [] };
  let jsErrors = 0;
  const onError = () => { jsErrors += 1; };
  page.on('pageerror', onError);
  try {
    const response = await page.goto(route, { waitUntil: 'domcontentloaded', timeout: 25_000 });
    await page.locator('main').first().waitFor({ state: 'visible', timeout: 10_000 });
    await page.waitForTimeout(200);
    const stats = await page.evaluate(() => {
      const main = document.querySelector('main');
      const dialogs = [...document.querySelectorAll('[role="dialog"],[role="alertdialog"]')];
      const badTargets = [...document.querySelectorAll('main button, main a, main input, main select')].filter(el => {
        const node = el as HTMLElement;
        const r = node.getBoundingClientRect();
        const visible = r.width > 0 && r.height > 0 && getComputedStyle(node).visibility !== 'hidden';
        return visible && (r.height < 28 || r.width < 28);
      }).length;
      const unlabeledControls = [...document.querySelectorAll('main button, main input, main select, main textarea')].filter(el => {
        const node = el as HTMLElement;
        const visible = node.getBoundingClientRect().height > 0;
        if (!visible) return false;
        if (node.tagName === 'INPUT' && (el as HTMLInputElement).type === 'hidden') return false;
        return !node.getAttribute('aria-label') && !node.getAttribute('aria-labelledby') && !node.getAttribute('title') &&
          !node.textContent?.trim() && !(node as HTMLInputElement).labels?.length;
      }).length;
      return {
        documentWidth: document.documentElement.scrollWidth, viewportWidth: innerWidth,
        mainWidth: Math.round(main?.getBoundingClientRect().width ?? 0),
        scrollHeight: document.documentElement.scrollHeight,
        totalButtons: document.querySelectorAll('main button').length,
        dialogs: dialogs.length, unlabeledControls, smallTargets: badTargets,
      };
    });
    out.h1 = ((await page.locator('main h1').first().textContent().catch(() => '')) ?? '').trim().slice(0, 150);
    const actualRoute = new URL(page.url()).pathname;
    // Initialize check data before assigning bound actual route metadata.
    if (actualRoute !== route && !route.startsWith('/share/')) out.findings.push(finding('ROUTE_REDIRECT_OR_DENIAL', 'P2', 'Requested ' + route + ', actual ' + actualRoute + '; do not classify the requested page as observed.'));
    const bodyTitle = out.h1.toLowerCase();
    if (route.startsWith('/modules/') && /không khả dụng|unavailable|disabled|chưa triển khai/.test(bodyTitle)) out.findings.push(finding('MODULE_UNAVAILABLE_NOT_FEATURE_IMPLEMENTED', 'P2', 'Only the module unavailable state was observed, not the target feature.'));

    out.checks = { statusCode: response?.status() ?? 0, ...stats, jsErrors, actualRoute };
    out.status = 'AUTO_PROBED';
    if (!out.h1) out.findings.push(finding('MISSING_H1', 'P2', 'No visible <h1> in main after initial load'));
    if (stats.documentWidth > stats.viewportWidth + 1) out.findings.push(finding('PAGE_HORIZONTAL_OVERFLOW', 'P1', 'Entire page scrollWidth exceeds viewport by ' + (stats.documentWidth - stats.viewportWidth) + 'px'));
    if (stats.unlabeledControls) out.findings.push(finding('UNNAMED_CONTROLS', 'P2', String(stats.unlabeledControls) + ' visible input/button controls lack a programmatic name'));
    if (stats.smallTargets > 3) out.findings.push(finding('SMALL_INTERACTIVE_TARGETS', 'P3', String(stats.smallTargets) + ' visible controls under 28px in one dimension; manual overlap/spacing review needed'));
    if (jsErrors) out.findings.push(finding('BROWSER_RUNTIME_ERRORS', 'P1', String(jsErrors) + ' uncaught JS exceptions; raw messages redacted'));
    if ((await page.locator('main').innerText()).includes('Đang tải các widget')) {
      out.findings.push(finding('POSSIBLE_UNSETTLED_DASHBOARD', 'P2', 'Dashboard loading observed; re-evaluate whether it settles after API response'));
    }
    try {
      const sampleAxe = !route.startsWith('/modules/') || /\/modules\/FX(11|12|20|24|27|40)$/.test(route);
      if (!sampleAxe) { out.checks.axeCoverage = 'NOT_RUN_SAMPLED'; }
      else {
      const axe = await new AxeBuilder({ page }).include('main').withTags(['wcag2a', 'wcag2aa', 'wcag21aa']).analyze();
      out.checks.axeViolations = axe.violations.length;
      for (const v of axe.violations.slice(0, 8)) out.findings.push(finding('AXE_' + v.id, 'P2', 'axe rule ' + v.id + ' affects ' + v.nodes.length + ' element(s); human WCAG review still required'));
      }
    } catch { out.findings.push(finding('AXE_NOT_EXECUTED', 'P3', 'Axe could not complete on this state')); }
    out.screenshot = await screenshot(page, persona, route, 'initial').catch(() => undefined);
    if (!out.screenshot) out.findings.push(finding('SCREENSHOT_MISSING', 'P1', 'No reviewable visual evidence captured; NOT accepted'));
    // Only open clearly non-destructive interactive disclosures. Never submit/save/revoke/delete.
    if (!route.startsWith('/modules/FX30') && !route.startsWith('/modules/FX34') && !route.startsWith('/modules/FX35')) {
      const candidates = await page.locator('main button:visible').evaluateAll(nodes =>
        nodes.map((node, index) => ({
          index,
          name: (node.getAttribute('aria-label') || node.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 90),
          disabled: (node as HTMLButtonElement).disabled,
        })).filter(c => c.name));
      let opened = 0;
      for (const candidate of candidates) {
        if (opened >= (route.startsWith('/modules/') ? 1 : 2)) break;
        if (candidate.disabled || !visiblePanelTriggers.test(candidate.name) || prohibitedTriggers.test(candidate.name)) continue;
        if (candidate.name.includes('Tạo tài khoản') || candidate.name.includes('Tạo snippet')) continue;
        try {
          await page.goto(route);
          await page.locator('main').first().waitFor({ state: 'visible' });
          const button = page.locator('main button:visible').filter({ hasText: candidate.name }).first();
          if (!(await button.count()) || !(await button.isEnabled())) continue;
          await button.click({ timeout: 2_000 });
          const modal = page.locator('[role="dialog"],[role="alertdialog"]').first();
          if (await modal.isVisible({ timeout: 700 }).catch(() => false)) {
            const width = await modal.evaluate(el => ({ width: el.scrollWidth, client: el.clientWidth }));
            out.dialogs!.push({
              trigger: candidate.name, visible: true,
              scrollOverflow: width.width > width.client + 1,
              screenshot: await screenshot(page, persona, route, 'dialog-' + opened).catch(() => undefined),
            });
            if (width.width > width.client + 1) out.findings.push(finding('DIALOG_HORIZONTAL_OVERFLOW', 'P1', 'Dialog opened by ' + candidate.name + ' exceeds own width'));
            opened++;
            await page.keyboard.press('Escape').catch(() => undefined);
          }
        } catch { /* The candidate is not a stable dialog trigger; report only observed dialogs. */ }
      }
    }
    // Real tab controls are audited separately from action buttons; do not fake subpage success.
    await page.goto(route);
    const tabs = page.getByRole('tab');
    const tabCount = Math.min(await tabs.count(), 5);
    for (let i = 0; i < tabCount; i++) {
      const tab = tabs.nth(i);
      const name = (await tab.getAttribute('aria-label')) || (await tab.textContent()) || 'Tab';
      try {
        await tab.click({ timeout: 1500 });
        out.tabs!.push({ title: name.trim().slice(0, 90), accessible: await tab.getAttribute('aria-selected') !== null, screenshot: await screenshot(page, persona, route, 'tab-' + i).catch(() => undefined) });
      } catch { out.tabs!.push({ title: name.trim().slice(0, 90), accessible: false }); }
    }
    return out;
  } catch {
    out.status = 'BLOCKED_RUNTIME';
    out.findings.push(finding('ROUTE_NOT_AUDITED', 'P1', 'Route could not reach a stable main landmark; failure sanitized'));
    return out;
  } finally { page.off('pageerror', onError); }
}
function persist(testInfo: TestInfo, name: string, results: Observation[], seed: string[]) {
  mkdirSync(artifactRoot, { recursive: true });
  const artifact = {
    schemaVersion: 1, scope: 'PR4 synthetic SQL/API/React browser UX evidence; no professional-design signoff',
    testedSha: process.env.GITHUB_SHA ?? 'unverified', project: testInfo.project.name, persona: name,
    results, syntheticSeeding: seed,
    counts: {
      attempted: results.length, autoProbed: results.filter(r => r.status === 'AUTO_PROBED').length,
      blocked: results.filter(r => r.status !== 'AUTO_PROBED').length,
      screenshots: results.filter(r => r.screenshot).length,
      dialogs: results.reduce((acc, r) => acc + (r.dialogs?.length ?? 0), 0),
      findings: results.reduce((acc, r) => acc + r.findings.length, 0),
    },
    explicitLimits: ['No personal/prod account or live provider effects', 'Empty states are not populated UX acceptance',
      'Axe is not a WCAG audit', 'Visible-dialog probe is not exhaustive hidden-state coverage',
      'Qualitative professional design evaluation requires manual screenshot inspection'],
  };
  writeFileSync(join(artifactRoot, safeSlug(testInfo.project.name + '-' + name) + '.json'), JSON.stringify(artifact, null, 2));
}

// Independent GitHub Actions matrix reviewers. No shard reads another shard's SQL,
// browser session, credential file, logs or artifacts. Unselected shards do no work.
type AuditGroup = { group: string; role: string; routes: string[]; seed: string };
const groups: AuditGroup[] = [
  { group: 'auth', role: 'Anonymous', routes: publicRoutes, seed: 'none' },
  { group: 'navigation', role: 'UserA', routes: ['/', '/search', '/search/saved', '/favorites', '/recent', '/command-palette', '/settings/profile', '/settings/security', '/notifications', '/trash'], seed: 'none' },
  { group: 'productivity', role: 'UserA', routes: ['/goals', '/planner', '/habits', ...modules.slice(10, 19)], seed: 'productivity' },
  { group: 'knowledge', role: 'UserA', routes: ['/bookmarks', '/snippets', '/read-later', '/organize/tags', '/files', '/learning/work-log', '/sharing', ...modules.slice(19, 24)], seed: 'knowledge' },
  { group: 'business', role: 'UserA', routes: ['/finance', '/modules/FX27', '/modules/FX29', '/modules/FX31', '/modules/FX32', '/modules/FX36', '/modules/FX37', '/modules/FX38', '/modules/FX39', '/modules/FX40'], seed: 'business' },
  { group: 'platform', role: 'UserA', routes: modules.slice(0, 10), seed: 'none' },
  { group: 'gated', role: 'UserA', routes: ['/developer/tools', '/support', '/modules/FX25', '/modules/FX26', '/modules/FX28', '/modules/FX30', '/modules/FX33', '/modules/FX34', '/modules/FX35'], seed: 'none' },
  { group: 'admin', role: 'SuperAdmin', routes: ['/','/admin/access','/admin/modules','/settings/security','/support','/sharing','/notifications'], seed: 'none' },
  { group: 'restricted', role: 'Restricted', routes: ['/', '/modules/FX11', '/modules/FX20', '/sharing', '/files', '/settings/profile'], seed: 'none' },
];
for (const group of groups) {
  test('parallel UIUX group: ' + group.group, async ({ page }, info) => {
    const results: Observation[] = [];
    const seed: string[] = [];
    try {
      if (group.role !== 'Anonymous') await logIn(page, group.role);
      if (group.seed !== 'none') await trySeed(page, seed, group.seed);
      for (const route of group.routes) {
        const observation = await observe(page, route, group.role, info);
        results.push(observation);
        // Persist partial evidence after EVERY route; a later bad page must not erase work.
        persist(info, group.role + '-' + group.group, results, seed);
      }
    } catch {
      seed.push('ROLE_OR_SEED_BLOCKED: no private credentials or secret exception details are logged');
    } finally { persist(info, group.role + '-' + group.group, results, seed); }
    if (results.length !== group.routes.length)
      throw new Error('AUDIT_PARTIAL: group ' + group.group + ' observed ' + results.length + '/' + group.routes.length + ' routes; see sanitized JSON.');
    const blocked = results.filter(r => r.status !== 'AUTO_PROBED' || !r.screenshot);
    if (blocked.length) throw new Error('AUDIT_INCOMPLETE: group ' + group.group + ' has ' + blocked.length + ' blocked/unscreened routes; JSON records remain available.');
  });
}
