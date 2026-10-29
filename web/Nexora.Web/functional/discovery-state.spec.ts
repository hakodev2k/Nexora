import { test, expect, api, login, sql, unique } from './fixtures';
import type { Page } from '@playwright/test';
import { response } from './time-focus-helpers';

// FX25-S01 / BR001,003,005,006 / AC001,003; DISC3/DISC4 concrete contracts.
// Positive paths use current native UI + real API/SQL; no response substitution.
const savedRegion = (page: Page) => page.getByRole('region', { name: 'Saved Searches', exact: true });
const savedCard = (page: Page, title: string) => savedRegion(page).locator('li.resource-card').filter({ has: page.getByText(title, { exact: true }) });
async function capture(page: Page, name: string) {
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: test.info().outputPath(name + '.png'), fullPage: true });
}
async function createSource(page: Page, title: string) {
  return api(page, 'createBookmark', ['https://example.invalid/' + unique('discovery'), title, 'Isolated discovery verification']);
}
async function openSource(page: Page, id: string, title: string) {
  await page.goto('/resources/Bookmark/' + id);
  await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
}
async function recentList(page: Page) {
  return response(page, '/api/v1/recents?resourceType=Bookmark&cursor=', 'GET');
}

test('E25 Saved Searches: versioned definition SQL, fresh run, revision conflict, owner isolation and definition-only delete', async ({ page, browser }) => {
  test.setTimeout(120000);
  await page.goto('/');
  const me = await api(page, 'getMe'), marker = unique('saved-query'), name = unique('Saved definition');
  const first = await createSource(page, marker + ' first');
  const firstBefore = sql('Bookmark', first.id);
  await page.goto('/search/saved');
  await page.locator('#global-search-query').fill(marker);
  await page.locator('#global-search-type').selectOption('Bookmark');
  await savedRegion(page).getByLabel('Tên Saved Search', { exact: true }).fill(name);
  const created = page.waitForResponse(r => r.url().endsWith('/api/v1/saved-searches') && r.request().method() === 'POST');
  await savedRegion(page).getByRole('button', { name: 'Lưu Saved Search', exact: true }).click();
  const creation = await created; expect(creation.status()).toBe(201); const saved = await creation.json();
  expect(sql('SavedQuery', saved.id)).toHaveLength(1);
  const persisted = sql('SavedQuery', saved.id)[0];
  expect(persisted.OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
  expect(persisted.SchemaVersion).toBe(1);
  const definition = JSON.parse(persisted.QueryJson);
  expect(definition.Text ?? definition.text).toBe(marker);
  expect(definition.ResourceType ?? definition.resourceType).toBe('Bookmark');
  await page.reload(); await savedRegion(page).getByRole('button', { name: 'Đọc danh sách đã lưu', exact: true }).click();
  await expect(savedCard(page, name)).toBeVisible();
  await savedCard(page, name).getByRole('button', { name: 'Chạy trên dữ liệu hiện tại', exact: true }).click();
  await expect(savedRegion(page).getByText(marker + ' first', { exact: true })).toBeVisible();
  const second = await createSource(page, marker + ' second');
  await savedCard(page, name).getByRole('button', { name: 'Chạy trên dữ liệu hiện tại', exact: true }).click();
  await expect(savedRegion(page).getByText(marker + ' second', { exact: true })).toBeVisible();
  await capture(page, 'saved-current-results');
  await savedCard(page, name).getByRole('button', { name: 'Sửa định nghĩa', exact: true }).click();
  await page.locator('#global-search-query').fill(marker + ' second');
  await savedRegion(page).getByLabel('Tên Saved Search', { exact: true }).fill(name + ' updated');
  const updatedResponse = page.waitForResponse(r => r.url().endsWith('/api/v1/saved-searches/' + saved.id) && r.request().method() === 'PUT');
  await savedRegion(page).getByRole('button', { name: 'Xác nhận thay bằng định nghĩa phiên bản 1', exact: true }).click();
  const updated = await updatedResponse; expect(updated.status()).toBe(200); const current = await updated.json();
  const afterUpdate = sql('SavedQuery', saved.id);
  expect(afterUpdate[0].Title).toBe(name + ' updated');
  const stale = await response(page, '/api/v1/saved-searches/' + saved.id, 'PUT', { title: name, schemaVersion: 1, query: saved.query }, saved.etag);
  expect(stale.status).toBe(412); expect(stale.body.code).toBe('RevisionConflict');
  const unknown = await response(page, '/api/v1/saved-searches/' + saved.id, 'PUT', { title: name, schemaVersion: 99, query: current.query }, current.etag);
  expect(unknown.status).toBe(409); expect(unknown.body.code).toBe('MigrationRequired');
  expect(sql('SavedQuery', saved.id)).toEqual(afterUpdate);
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  try {
    const other = await foreign.newPage(); await login(other, 'UserB');
    const list = await api(other, 'listSavedSearches'); expect(list.items.some((item: any) => item.id === saved.id)).toBe(false);
    for (const [method, suffix, body] of [['POST', '/run', undefined], ['PUT', '', { title: 'foreign', schemaVersion: 1, query: current.query }], ['DELETE', '', undefined]] as const) {
      expect((await response(other, '/api/v1/saved-searches/' + saved.id + suffix, method, body, current.etag)).status).toBe(404);
    }
    expect(sql('SavedQuery', saved.id)).toEqual(afterUpdate);
  } finally { await foreign.close(); }
  await savedCard(page, name + ' updated').getByRole('button', { name: 'Xóa định nghĩa', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Xóa Saved Search?', exact: true });
  await expect(dialog).toBeVisible(); await capture(page, 'saved-delete-confirmation');
  await dialog.getByRole('button', { name: 'Xác nhận xóa định nghĩa', exact: true }).click();
  await expect(dialog).toHaveCount(0); expect(sql('SavedQuery', saved.id)).toEqual([]);
  expect(sql('Bookmark', first.id)).toEqual(firstBefore); expect(sql('Bookmark', second.id)).toHaveLength(1);
});

test('E25 Recents: real source open, owner-only metadata and preview watermark keeps later opens/source data', async ({ page, browser }) => {
  test.setTimeout(120000);
  await page.goto('/'); const me = await api(page, 'getMe');
  const first = await createSource(page, unique('Recent before')), later = await createSource(page, unique('Recent after'));
  const firstBefore = sql('Bookmark', first.id), laterBefore = sql('Bookmark', later.id);
  await openSource(page, first.id, first.title);
  const listed = await recentList(page); expect(listed.status).toBe(200);
  const old = listed.body.items.find((item: any) => item.resourceId === first.id); expect(old).toBeTruthy();
  expect(sql('RecentItem', old.id)[0].OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
  await page.goto('/recent'); await expect(page.getByText(first.title, { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Xóa lịch sử gần đây…', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Xóa lịch sử mở gần đây?', exact: true }); await expect(dialog).toBeVisible();
  const sibling = await page.context().newPage();
  try { await openSource(sibling, later.id, later.title); } finally { await sibling.close(); }
  const newer = (await recentList(page)).body.items.find((item: any) => item.resourceId === later.id); expect(newer).toBeTruthy();
  await capture(page, 'recents-bounded-clear');
  // A second page may rotate the antiforgery cookie. Observe the real client's
  // one CsrfInvalid refresh, then assert the final handler response (no API retry here).
  const prehandlerResponses: string[] = [];
  const cleared = page.waitForResponse(async r => {
    if (!r.url().endsWith('/api/v1/recents/clear') || r.request().method() !== 'POST') return false;
    if (r.status() === 403 && (await r.json()).code === 'CsrfInvalid') { prehandlerResponses.push('403 CsrfInvalid'); return false; }
    return true;
  });
  await dialog.getByRole('button', { name: 'Xóa metadata đã xem trước', exact: true }).click(); expect((await cleared).status()).toBe(204);
  await expect(dialog).toHaveCount(0); expect(sql('RecentItem', old.id)).toEqual([]); expect(sql('RecentItem', newer.id)).toHaveLength(1);
  expect(prehandlerResponses.length).toBeLessThanOrEqual(1);
  await page.getByRole('button', { name: 'Tải lại', exact: true }).click();
  await expect(page.getByText(later.title, { exact: true })).toBeVisible(); await expect(page.getByText(first.title, { exact: true })).toHaveCount(0);
  await page.reload(); await expect(page.getByText(later.title, { exact: true })).toBeVisible();
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  try { const other = await foreign.newPage(); await login(other, 'UserB'); const list = await recentList(other); expect(list.status).toBe(200); expect(list.body.items.some((item: any) => [first.id, later.id].includes(item.resourceId))).toBe(false); }
  finally { await foreign.close(); }
  expect(sql('Bookmark', first.id)).toEqual(firstBefore); expect(sql('Bookmark', later.id)).toEqual(laterBefore);
});

test('E25 Command Palette: keyboard highlight has no effect and destructive handoff requires normal confirmation', async ({ page, browser }) => {
  test.setTimeout(120000);
  await page.goto('/'); const source = await createSource(page, unique('Palette source'));
  await openSource(page, source.id, source.title); const before = sql('Bookmark', source.id);
  let executed = 0, cleared = 0;
  page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/api/v1/commands/execute')) executed++; if (request.method() === 'POST' && request.url().endsWith('/api/v1/recents/clear')) cleared++; });
  await page.keyboard.press('Control+k');
  const palette = page.getByRole('dialog', { name: 'Command Palette', exact: true }); await expect(palette).toBeVisible();
  await palette.getByRole('combobox', { name: 'Tìm lệnh hoặc nguồn', exact: true }).fill('clear-recents');
  const option = palette.getByRole('option', { name: 'Xóa lịch sử gần đây…', exact: true });
  await expect(option).toBeVisible(); await expect(option).toHaveAttribute('aria-disabled', 'false');
  await page.keyboard.press('ArrowDown'); await expect(option).toHaveAttribute('aria-selected', 'true'); expect(executed).toBe(0); expect(cleared).toBe(0);
  await capture(page, 'palette-keyboard-selection');
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/\/recent$/); await expect(palette).toHaveCount(0);
  const confirmation = page.getByRole('dialog', { name: 'Xóa lịch sử mở gần đây?', exact: true }); await expect(confirmation).toBeVisible();
  expect(executed).toBe(1); expect(cleared).toBe(0);
  await confirmation.getByRole('button', { name: 'Giữ lại Recents', exact: true }).click();
  await expect(confirmation).toHaveCount(0); expect(cleared).toBe(0);
  expect((await recentList(page)).body.items.some((item: any) => item.resourceId === source.id)).toBe(true);
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  try { const other = await foreign.newPage(); await login(other, 'UserB'); const denied = await response(other, '/api/v1/commands/execute', 'POST', { kind: 'Resource', resourceType: 'Bookmark', resourceId: source.id }); expect(denied.status).toBe(404); }
  finally { await foreign.close(); }
  expect(sql('Bookmark', source.id)).toEqual(before);
});
