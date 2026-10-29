import { mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { test, expect, api, sql, unique } from './fixtures';
import type { Page, Route } from '@playwright/test';

// The actual application commits to real SQL before only the response is lost.
// Positive writes/retries are never fulfilled with fabricated business results.
async function timeoutAfterCommit(page: Page) {
  const commands: { key?: string; body: string | null; id: string }[] = [];
  const handle = async (route: Route) => {
    const request = route.request();
    if (new URL(request.url()).pathname !== '/api/v1/bookmarks' || request.method() !== 'POST') { await route.continue(); return; }
    const response = await route.fetch({ maxRedirects: 0 });
    if (!response.ok()) { await route.fulfill({ response }); return; }
    const result = await response.json();
    commands.push({ key: request.headers()['idempotency-key'], body: request.postData(), id: result.id });
    if (commands.length === 1) await route.fulfill({ status: 408, contentType: 'application/problem+json',
      body: JSON.stringify({ status: 408, title: 'Synthetic response timeout after the real commit.', code: 'RequestTimeout' }) });
    else await route.fulfill({ response });
  };
  await page.route('**/api/v1/bookmarks', handle);
  return { commands, close: () => page.unroute('**/api/v1/bookmarks', handle) };
}
async function begin(page: Page, title: string) {
  await page.goto('/bookmarks'); await page.getByRole('button', { name: 'Thêm bookmark', exact: true }).click();
  const modal = page.getByRole('dialog'); await modal.getByLabel('URL', { exact: true }).fill('https://example.invalid/' + title);
  await modal.getByLabel('Tiêu đề', { exact: true }).fill(title);
  await modal.getByRole('button', { name: 'Tạo bookmark', exact: true }).click();
  await expect(modal.getByText('Chưa xác định được kết quả lưu.', { exact: false })).toBeVisible();
  await expect(modal.getByLabel('Tiêu đề', { exact: true })).toHaveValue(title);
  await expect(modal.getByLabel('Tiêu đề', { exact: true })).toBeDisabled();
  await expect(modal.getByRole('button', { name: 'Hủy', exact: true })).toBeDisabled();
  await page.keyboard.press('Escape'); await expect(modal).toBeVisible();
  return modal;
}
async function capture(page: Page, name: string) {
  const width = page.viewportSize()!.width, path = join('artifacts', 'existing-recovery-review', String(width));
  mkdirSync(path, { recursive: true }); await page.screenshot({ path: join(path, name + '.png'), fullPage: false });
}

test('Bookmark committed HTTP408: frozen exact retry yields one persisted record after reload', async ({ page }) => {
  page.setDefaultTimeout(15_000); const title = unique('bookmark-timeout'), fault = await timeoutAfterCommit(page);
  try {
    const modal = await begin(page, title); await capture(page, 'bookmark-uncertain-retry');
    expect(fault.commands).toHaveLength(1); expect(sql('Bookmark', fault.commands[0].id)[0].Title).toBe(title);
    await modal.getByRole('button', { name: 'Tạo bookmark', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(fault.commands).toHaveLength(2); expect(fault.commands[0].key).toBeTruthy(); expect(fault.commands[1]).toEqual(fault.commands[0]);
    await page.reload(); await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    const found = (await api(page, 'listBookmarks', [false, title, 100])).items;
    expect(found).toHaveLength(1); expect(found[0].id).toBe(fault.commands[0].id);
    expect(sql('Bookmark', found[0].id)).toHaveLength(1);
  } finally { await fault.close(); }
});

test('Bookmark committed HTTP408: explicit informed abandonment checks saved data without replay', async ({ page }) => {
  page.setDefaultTimeout(15_000); const title = unique('bookmark-abandon'), fault = await timeoutAfterCommit(page);
  try {
    const modal = await begin(page, title);
    await modal.getByRole('button', { name: 'Dừng đối chiếu…', exact: true }).click();
    await expect(modal.getByText('Bookmark có thể đã được lưu.', { exact: false })).toBeVisible();
    await modal.getByRole('button', { name: 'Tiếp tục đối chiếu', exact: true }).click();
    await expect(modal.getByLabel('Tiêu đề', { exact: true })).toBeDisabled();
    await modal.getByRole('button', { name: 'Dừng đối chiếu…', exact: true }).click();
    await capture(page, 'bookmark-informed-abandon');
    await modal.getByRole('button', { name: 'Tôi hiểu, kiểm tra dữ liệu đã lưu', exact: true }).click();
    await expect(modal).toHaveCount(0); expect(fault.commands).toHaveLength(1);
    await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    await page.reload(); expect((await api(page, 'listBookmarks', [false, title, 100])).items).toHaveLength(1);
    expect(sql('Bookmark', fault.commands[0].id)[0].Title).toBe(title);
  } finally { await fault.close(); }
});
