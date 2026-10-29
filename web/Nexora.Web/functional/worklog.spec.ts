import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';
import type { Browser, Page } from '@playwright/test';

const root = '/api/v1/learning/work-logs';
async function moduleApi(page: Page, name: string, args: unknown[] = []) {
  return page.evaluate(async ({ name, args }) => { const path = '/src/workLogApi.ts'; const module = await import(path); return module[name](...args); }, { name, args });
}
async function enabled(browser: Browser, page: Page) {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const me = await api(page, 'getMe'), installed = (await api(admin, 'listAdminModules')).items.find((row: any) => row.code === 'FX40');
  expect(installed?.state, 'Only the already Ready manual Learning module is used').toBe('Ready');
  const grants = new Map<string, boolean>();
  const policy = async (systemEnabled: boolean) => {
    const current = (await api(admin, 'listAdminModules')).items.find((row: any) => row.code === 'FX40'); if (current.systemEnabled === systemEnabled) return;
    const change = { systemEnabled, registrationEnabled: systemEnabled ? installed.registrationEnabled : false };
    const preview = await api(admin, 'previewModulePolicy', [installed.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [installed.id, preview.etag, change, preview.previewToken]);
  };
  const grantUser = async (id: string, on: boolean) => {
    const current = (await api(admin, 'getAdminUserAccess', [id])).moduleGrants.find((row: any) => row.code === 'FX40');
    if (!grants.has(id)) grants.set(id, current.enabled); if (current.enabled === on) return;
    const changes = [{ moduleId: current.moduleId, enabled: on }], preview = await api(admin, 'previewAdminAccess', [id, { kind: 'modules', changes }]);
    expect(preview.blockers).toEqual([]); await api(admin, 'commitAdminModuleGrant', [id, preview.etag, changes, preview.previewToken]);
  };
  const close = async () => {
    try {
      for (const [id, original] of [...grants].reverse()) await grantUser(id, original);
      await policy(installed.systemEnabled);
      const restored = (await api(admin, 'listAdminModules')).items.find((row: any) => row.code === 'FX40');
      expect(restored.systemEnabled).toBe(installed.systemEnabled); expect(restored.registrationEnabled).toBe(installed.registrationEnabled);
      for (const [id, original] of grants) expect((await api(admin, 'getAdminUserAccess', [id])).moduleGrants.find((row: any) => row.code === 'FX40').enabled).toBe(original);
    } finally { await context.close(); }
  };
  try { await policy(true); await grantUser(me.id, true); } catch (error) { await close(); throw error; }
  return { me, grantUser, close };
}
async function reload(page: Page) { await page.getByRole('button', { name: 'Tải lại', exact: true }).click(); await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled(); }
async function select(page: Page, title: string, status = 'Active') {
  await expect(page.getByLabel('Trạng thái', { exact: true })).toBeEnabled(); await page.getByLabel('Trạng thái', { exact: true }).selectOption(status);
  await page.locator('#worklog-query').fill(title); await page.getByRole('button', { name: 'Áp dụng bộ lọc', exact: true }).click();
  const item = page.locator('article.resource-card').filter({ hasText: title }); await expect(item).toHaveCount(1); await item.getByRole('button', { name: 'Chi tiết', exact: true }).click();
  const detail = page.locator('article.form-panel').filter({ has: page.getByRole('heading', { name: title, exact: true }) }); await expect(detail).toBeVisible(); return detail;
}
async function capture(page: Page, name: string) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: `artifacts/worklog-${test.info().project.name}-${name}.png`, fullPage: true });
}

test('WorkLog: real manual form, exact SQL owner/date/minutes, immutable history/gross report and native lifecycle', async ({ page, browser }) => {
  test.setTimeout(120000); await page.goto('/'); const setup = await enabled(browser, page); const title = unique('manual-worklog'), category = unique('category'); let failed = false;
  try {
    await page.goto('/learning/work-log'); await expect(page.getByRole('heading', { name: 'Work Log', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Nhật ký mới', exact: true }).click(); const modal = page.getByRole('dialog');
    await expect(modal.getByRole('button', { name: 'Hủy', exact: true })).toHaveCount(1);
    await modal.getByLabel('Tiêu đề', { exact: true }).fill('Dirty synthetic draft'); await modal.getByLabel('Thời lượng (phút)', { exact: true }).fill('15');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal).toContainText('Bỏ thay đổi chưa lưu?'); await modal.getByRole('button', { name: 'Tiếp tục sửa', exact: true }).click();
    await expect(modal.getByLabel('Tiêu đề', { exact: true })).toHaveValue('Dirty synthetic draft'); await expect(modal.getByLabel('Thời lượng (phút)', { exact: true })).toHaveValue('15');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await modal.getByRole('button', { name: 'Bỏ bản nháp', exact: true }).click(); await expect(modal).toHaveCount(0);
    await page.getByRole('button', { name: 'Nhật ký mới', exact: true }).click(); await expect(modal.getByLabel('Tiêu đề', { exact: true })).toHaveValue(''); await expect(modal.getByLabel('Thời lượng (phút)', { exact: true })).toHaveValue('');
    await modal.getByLabel('Tiêu đề', { exact: true }).fill(title); await modal.getByLabel('Ngày địa phương', { exact: true }).fill('2026-10-18');
    await modal.getByLabel('Thời lượng (phút)', { exact: true }).fill('0');
    let createRequests = 0; const count = (request: any) => { if (request.method() === 'POST' && new URL(request.url()).pathname.replace(/\/$/, '') === root) createRequests++; }; page.on('request', count);
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); expect(await modal.getByLabel('Thời lượng (phút)', { exact: true }).evaluate((input: HTMLInputElement) => input.validity.rangeUnderflow)).toBe(true); expect(createRequests).toBe(0);
    await modal.getByLabel('Thời lượng (phút)', { exact: true }).fill('60'); await modal.getByLabel('Nhóm (tùy chọn)', { exact: true }).fill(category); await modal.getByLabel('Tổ chức (tùy chọn)', { exact: true }).fill('Synthetic local QA'); await modal.getByLabel('Ghi chú (tùy chọn)', { exact: true }).fill('Private synthetic original note'); await capture(page, 'populated-form');
    const created = page.waitForResponse(result => result.request().method() === 'POST' && new URL(result.url()).pathname.replace(/\/$/, '') === root);
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); const result = await created; expect(result.status()).toBe(201); const ack = await result.json(); expect(ack).toEqual({ id: ack.id, committed: true, detailAvailable: false }); await expect(modal).toHaveCount(0); page.off('request', count);
    const initial = sql('WorkLog', ack.id); expect(initial).toHaveLength(1); expect(initial[0].OwnerId.toLowerCase()).toBe(setup.me.personalSpaceId.toLowerCase()); expect(initial[0].RecordedOn).toBe('2026-10-18'); expect(initial[0].Minutes).toBe(60);
    await reload(page); let detail = await select(page, title); await detail.getByRole('button', { name: 'Sửa', exact: true }).click(); await modal.getByLabel('Thời lượng (phút)', { exact: true }).fill('90'); await modal.getByLabel('Ghi chú (tùy chọn)', { exact: true }).fill('Private synthetic corrected note'); await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('WorkLog', ack.id)[0].Minutes).toBe(90); await reload(page); detail = await select(page, title); await detail.getByRole('button', { name: 'Tải lịch sử từ đầu', exact: true }).click();
    const history = page.getByRole('region', { name: 'Lịch sử nhật ký', exact: true }); await expect(history.locator('details')).toHaveCount(2); await history.locator('details').first().locator('summary').click(); await expect(history).toContainText('Private synthetic corrected note'); await history.locator('details').last().locator('summary').click(); await expect(history).toContainText('Private synthetic original note');
    await page.locator('#worklog-from').fill('2026-10-18'); await page.locator('#worklog-to').fill('2026-10-18'); await page.locator('#worklog-filter-category').fill(category); await page.getByRole('button', { name: 'Tổng hợp theo ngày', exact: true }).click(); const report = page.getByRole('region', { name: 'Tổng thời lượng đã nhập', exact: true }); await expect(report).toContainText('1 mục · 90 phút'); await expect(report).toContainText('không tự loại trùng'); await capture(page, 'populated-report');
    detail = await select(page, title); await detail.getByRole('button', { name: 'Archive', exact: true }).click(); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WorkLog', ack.id)[0].Status).toBe('Archived');
    await reload(page); detail = await select(page, title, 'Archived'); await expect(detail.getByRole('button', { name: 'Sửa', exact: true })).toHaveCount(0); await detail.getByRole('button', { name: 'Đưa vào Thùng rác…', exact: true }).click(); await expect(modal).toContainText('Archived → Trash'); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WorkLog', ack.id)[0].PreTrashState).toBe('Archived');
    await reload(page); detail = await select(page, title, 'Trash'); await detail.getByRole('button', { name: 'Khôi phục về Archived…', exact: true }).click(); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WorkLog', ack.id)[0].Status).toBe('Archived');
    await reload(page); detail = await select(page, title, 'Archived'); await detail.getByRole('button', { name: 'Unarchive', exact: true }).click(); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WorkLog', ack.id)[0].Status).toBe('Active');
    await reload(page); detail = await select(page, title); await detail.getByRole('button', { name: 'Đưa vào Thùng rác…', exact: true }).click(); await modal.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect(modal).toHaveCount(0); await reload(page); detail = await select(page, title, 'Trash'); await detail.getByRole('button', { name: 'Xóa vĩnh viễn…', exact: true }).click(); await expect(modal).toContainText('Không có hoàn tác'); await modal.getByRole('button', { name: 'Xóa vĩnh viễn nội dung và lịch sử', exact: true }).click(); await expect(modal).toHaveCount(0); expect(sql('WorkLog', ack.id)).toEqual([]);
  } catch (error) { failed = true; throw error; } finally { try { await setup.close(); } catch (error) { if (!failed) throw error; console.error('WorkLog policy restoration failed after workflow failure.'); } }
});

test('WorkLog: real boundary validation, foreign-owner private denial and current revoked draft clearing', async ({ page, browser }) => {
  test.setTimeout(120000); await page.goto('/'); const setup = await enabled(browser, page); const title = unique('private-worklog'); let failed = false;
  const foreignContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  try {
    const body = { title, recordedOn: '2026-10-18', minutes: 60, notes: 'Private synthetic canary note', category: null, employer: null };
    for (const [minutes, status] of [[0,422],[-1,422],[1441,422],[1.5,400]]) { const rejected = await response(page, root, 'POST', { ...body, minutes }); expect(rejected.status).toBe(status); }
    const none = await moduleApi(page, 'workLogList', [{ status: 'Active', query: title, from: '', to: '' }]); expect(none.items).toEqual([]);
    const saved = await moduleApi(page, 'workLogSave', [body, crypto.randomUUID()]); const before = sql('WorkLog', saved.id)[0];
    const foreign = await foreignContext.newPage(); await login(foreign, 'UserB'); const foreignMe = await api(foreign, 'getMe'); await setup.grantUser(foreignMe.id, true);
    for (const suffix of ['', '/history']) { const denied = await response(foreign, `${root}/${saved.id}${suffix}`, 'GET'); expect(denied.status).toBe(404); expect(JSON.stringify(denied.body)).not.toContain(title); expect(JSON.stringify(denied.body)).not.toContain(body.notes); }
    const foreignList = await moduleApi(foreign, 'workLogList', [{ status: 'Active', query: title, from: '', to: '' }]); expect(foreignList.items).toEqual([]);
    await page.goto('/learning/work-log'); const detail = await select(page, title); await detail.getByRole('button', { name: 'Sửa', exact: true }).click(); const modal = page.getByRole('dialog'); await modal.getByLabel('Ghi chú (tùy chọn)', { exact: true }).fill('Revoked private unsaved draft');
    await setup.grantUser(setup.me.id, false); await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); await expect(modal).toHaveCount(0); await expect(page.getByText(title, { exact: true })).toHaveCount(0); await expect(page.getByText(body.notes, { exact: true })).toHaveCount(0); await expect(page.locator('#worklog-notes')).toHaveCount(0); expect(sql('WorkLog', saved.id)[0]).toEqual(before);
    await setup.grantUser(setup.me.id, true); await page.goto('/learning/work-log'); await select(page, title); await capture(page, 'restored-owner-detail');
  } catch (error) { failed = true; throw error; } finally { await foreignContext.close(); try { await setup.close(); } catch (error) { if (!failed) throw error; console.error('WorkLog policy restoration failed after security workflow failure.'); } }
});
