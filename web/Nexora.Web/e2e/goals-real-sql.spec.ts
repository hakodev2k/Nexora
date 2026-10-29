import { readFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { expect, test } from '@playwright/test';
import { safeStep } from './safe-step';

type Detail = { goal: { id: string; status: string; progress: number; preArchiveStatus: string | null }; targets: Array<{ currentValueExact: string }> };
// FX16 BR001/003 and numeric/progress-history/archive contracts (2026-10-18).
// UI writes and independent API reads use the job-owned real SQL target.
test('FX16 real SQL numeric progress and Archive/Unarchive persist without auto-completion', async ({ page }) => {
  test.setTimeout(90_000);
  const title = 'Synthetic goal ' + randomUUID();
  let id = '';
  async function readDetail(): Promise<Detail> {
    const response = await page.request.get('/api/v1/goals/' + id);
    expect(response.status()).toBe(200);
    return await response.json() as Detail;
  }
  await safeStep('GOAL_LOGIN', async () => {
    const path = process.env.NEXORA_E2E_ACCOUNTS;
    if (!path) throw Error('Synthetic account file required');
    const account = (JSON.parse(readFileSync(path, 'utf8')) as Array<{ role: string; email: string; password: string }>).find(a => a.role === 'UserA');
    if (!account?.email.endsWith('@example.invalid')) throw Error('Synthetic owner required');
    await page.goto('/login');
    await page.getByLabel('Email').fill(account.email);
    await page.getByLabel('Mật khẩu').fill(account.password);
    await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
    await expect(page).toHaveURL('/');
  });
  await safeStep('GOAL_CREATE', async () => {
    await page.goto('/goals');
    await page.getByLabel('Tên goal', { exact: true }).fill(title);
    await page.getByLabel('Tên target', { exact: true }).fill('Synthetic numeric target');
    await page.getByLabel('Initial', { exact: true }).fill('0');
    await page.getByLabel('Current', { exact: true }).fill('2.5');
    await page.getByLabel('Target', { exact: true }).fill('10');
    const pending = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/goals' && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Tạo goal', exact: true }).click();
    const response = await pending;
    expect(response.status()).toBe(201);
    const created = await response.json() as Detail;
    id = created.goal.id;
    expect(id).toMatch(/^[a-f0-9-]{36}$/i);
    expect(created.goal.status).toBe('Draft');
    expect(created.goal.progress).toBe(0.25);
    await expect(page.locator('.goal-detail h2')).toHaveText(title);
    await page.locator('.goal-detail').getByRole('button', { name: 'Bắt đầu', exact: true }).click();
    await expect(page.locator('.goal-detail .state-pill')).toHaveText('Active');
  });
  await safeStep('GOAL_PROGRESS', async () => {
    await page.getByRole('button', { name: 'Ghi tiến độ', exact: true }).click();
    const dialog = page.getByRole('dialog', { name: 'Ghi tiến độ target', exact: true });
    await dialog.getByLabel('Giá trị mới', { exact: true }).fill('10');
    await dialog.getByLabel('Ghi chú (tùy chọn)').fill('Synthetic progress evidence');
    await dialog.getByRole('button', { name: 'Lưu', exact: true }).click();
    await expect(dialog).toBeHidden();
    await expect(page.locator('.goal-progress-summary strong')).toHaveText('100.0%');
    const detail = await readDetail();
    expect(detail.goal.status).toBe('Active'); // BR003: 100% never auto-completes.
    expect(detail.goal.progress).toBe(1);
    expect(Number(detail.targets[0].currentValueExact)).toBe(10);
    const history = await page.request.get('/api/v1/goals/' + id + '/progress-history');
    expect(history.status()).toBe(200);
    const evidence = await history.json() as { items: Array<{ note: string | null }> };
    expect(evidence.items.some(item => item.note === 'Synthetic progress evidence')).toBe(true);
  });
  await safeStep('GOAL_LIFECYCLE', async () => {
    await page.locator('.goal-detail').getByRole('button', { name: 'Archive', exact: true }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Archive', exact: true }).click();
    await expect(page.locator('.goal-detail .state-pill')).toHaveText('Archived');
    expect((await readDetail()).goal.preArchiveStatus).toBe('Active');
    await page.reload();
    await page.locator('.resource-card-button').filter({ hasText: title }).click();
    await expect(page.locator('.goal-detail .state-pill')).toHaveText('Archived');
    await page.locator('.goal-detail').getByRole('button', { name: 'Unarchive', exact: true }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Unarchive', exact: true }).click();
    await expect(page.locator('.goal-detail .state-pill')).toHaveText('Active');
  });
  await safeStep('GOAL_PERSISTENCE', async () => {
    const fresh = await page.context().newPage();
    try {
      await fresh.goto('/goals');
      await fresh.locator('.resource-card-button').filter({ hasText: title }).click();
      await expect(fresh.locator('.goal-detail .state-pill')).toHaveText('Active');
      await expect(fresh.locator('.goal-progress-summary strong')).toHaveText('100.0%');
      const persisted = await readDetail();
      expect(persisted.goal.status).toBe('Active');
      expect(persisted.goal.progress).toBe(1);
      expect(Number(persisted.targets[0].currentValueExact)).toBe(10);
    } finally { await fresh.close(); }
  });
});
