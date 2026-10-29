import { readFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

async function appApi(page: Page, name: string, args: unknown[] = []): Promise<any> {
  return page.evaluate(async ({ name, args }) => {
    const modulePath = '/src/api.ts';
    const api = await import(modulePath);
    return api[name](...args);
  }, { name, args });
}
async function signIn(page: Page, role: string) {
  const accounts = JSON.parse(readFileSync(process.env.NEXORA_E2E_ACCOUNTS!, 'utf8').replace(/^\uFEFF/, '')) as Array<{role: string; email: string; password: string}>;
  const account = accounts.find(row => row.role === role);
  if (!account?.email.endsWith('@example.invalid')) throw new Error('Synthetic account required');
  const session = await page.request.get('/api/v1/me');
  if (session.ok()) { await page.goto('/'); await expect(page.locator('.app-shell')).toBeVisible(); await appApi(page, 'logout'); }
  await page.goto('/login');
  await page.getByLabel('Email', { exact: true }).fill(account.email);
  await page.getByLabel('Mật khẩu', { exact: true }).fill(account.password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page.locator('.app-shell')).toBeVisible();
}
async function stage(code: string, action: () => Promise<void>) {
  try { await action(); } catch { throw new Error('COMPLETION_FLOW[' + code + ']'); }
}

test('owner uses a Bookmark template, saves a seller, and records certification renewal in real SQL', async ({ page }, info) => {
  test.setTimeout(120_000);
  page.setDefaultTimeout(10_000);
  const suffix = randomUUID().slice(0, 8), template = 'Reading starter ' + suffix, bookmark = 'A useful reference ' + suffix;
  const seller = 'Local stationery ' + suffix, certificate = 'Learning milestone ' + suffix;
  await stage('REAL_AUTH_AND_APPROVED_MODULE_SETUP', async () => {
    await signIn(page, 'UserA'); const owner = await appApi(page, 'getMe');
    await appApi(page, 'logout'); await signIn(page, 'SuperAdmin');
    const modules = (await appApi(page, 'listAdminModules')).items as Array<{id: string; code: string; state: string; systemEnabled: boolean}>;
    const selected = modules.filter(row => ['FX31', 'FX40'].includes(row.code));
    expect(selected).toHaveLength(2);
    for (const module of selected) {
      // Use real SuperAdmin preview/commit; never rewrite Ready status or SQL grants.
      expect(module.state).toBe('Ready');
      if (!module.systemEnabled) {
        const change = { systemEnabled: true };
        const preview = await appApi(page, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
        await appApi(page, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken, randomUUID()]);
      }
    }
    const changes = selected.filter(row => !owner.modules.some((current: {code: string; enabled: boolean}) => current.code === row.code && current.enabled)).map(row => ({ moduleId: row.id, enabled: true }));
    if (changes.length) {
      const change = { kind: 'modules', changes };
      const preview = await appApi(page, 'previewAdminAccess', [owner.id, change]); expect(preview.blockers).toEqual([]);
      await appApi(page, 'commitAdminModuleGrant', [owner.id, preview.etag, changes, preview.previewToken, randomUUID()]);
    }
    await appApi(page, 'logout'); await signIn(page, 'UserA');
  });
  await stage('TEMPLATE_TO_NORMAL_BOOKMARK_FORM', async () => {
    await page.goto('/organize/templates'); await page.getByRole('button', { name: 'Tạo mẫu mới', exact: true }).click();
    const modal = page.getByRole('dialog');
    await modal.getByLabel('Tên mẫu', { exact: true }).fill(template);
    await modal.getByLabel('Tiêu đề Bookmark', { exact: true }).fill(bookmark);
    await modal.getByLabel('Đường dẫn HTTP(S)', { exact: true }).fill('https://example.invalid/reference');
    await modal.getByRole('button', { name: 'Lưu mẫu', exact: true }).click(); await expect(modal).toBeHidden();
    await page.locator('.template-card').filter({ hasText: template }).getByRole('button', { name: 'Xem trước', exact: true }).click();
    await page.getByRole('button', { name: 'Dùng mẫu này', exact: true }).click();
    await expect(page).toHaveURL('/bookmarks');
    const bookmarkForm = page.getByRole('dialog'); await expect(bookmarkForm).toBeVisible();
    await expect(bookmarkForm.getByLabel('Tiêu đề', { exact: true })).toHaveValue(bookmark);
    // The handoff has not created a target until the ordinary Save is chosen.
    expect((await appApi(page, 'listBookmarks', [false, bookmark])).items).toHaveLength(0);
    await bookmarkForm.getByRole('button', { name: 'Tạo bookmark', exact: true }).click(); await expect(bookmarkForm).toBeHidden();
    await page.reload(); await expect(page.getByRole('heading', { name: bookmark, exact: true })).toBeVisible();
    expect((await appApi(page, 'listBookmarks', [false, bookmark])).items).toHaveLength(1);
  });
  await stage('SELLER_UI_SAVE_AND_PERSISTENCE', async () => {
    await page.goto('/shopping/sellers'); await page.getByRole('button', { name: 'Thêm người bán', exact: true }).click();
    const modal = page.getByRole('dialog'); await modal.getByLabel('Tên người bán', { exact: true }).fill(seller);
    await modal.getByLabel('Website công khai', { exact: true }).fill('https://example.invalid/shop');
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); await expect(modal).toBeHidden();
    await page.goto('/shopping/sellers'); await expect(page.getByRole('heading', { name: seller, exact: true })).toBeVisible();
  });
  await stage('CERTIFICATION_CREATE_RENEW_AND_HISTORY', async () => {
    await page.goto('/learning/certifications'); await page.getByRole('button', { name: 'Thêm chứng chỉ', exact: true }).click();
    let modal = page.getByRole('dialog'); await modal.getByLabel('Tên chứng chỉ', { exact: true }).fill(certificate);
    await modal.getByLabel('Đơn vị cấp', { exact: true }).fill('Synthetic learning institute');
    await modal.getByLabel('Ngày cấp (tùy chọn)', { exact: true }).fill('2026-01-01');
    await modal.getByLabel('Ngày hết hạn (tùy chọn)', { exact: true }).fill('2027-01-01');
    await modal.getByRole('button', { name: 'Lưu chứng chỉ', exact: true }).click(); await expect(modal).toBeHidden();
    await page.locator('.resource-card').filter({ has: page.getByRole('heading', { name: certificate, exact: true }) }).getByRole('button', { name: 'Xem chứng chỉ', exact: true }).click();
    await page.getByRole('button', { name: 'Ghi nhận gia hạn', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Ngày hết hạn (tùy chọn)', { exact: true }).fill('2028-01-01');
    await modal.getByRole('button', { name: 'Lưu chứng chỉ', exact: true }).click(); await expect(modal).toBeHidden();
    await page.reload(); await expect(page.getByText('01/01/2028', { exact: true }).first()).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
    await page.screenshot({ path: info.outputPath('certification-populated.png'), fullPage: false });
  });
});
