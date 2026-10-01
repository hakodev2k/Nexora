import { test, expect, api, sql, unique, card, confirm } from './fixtures';

test('FN-031 Search: body-only authorized match, safe source navigation and archive filter', async ({ page }) => {
  await page.goto('/'); const marker = unique('search-body'); const title = unique('search-document');
  const source = await api(page, 'createDocument', [title, 'Note', 'Markdown', '# Body\n' + marker]);
  const before = sql('Document', source.id);
  await page.goto('/search'); await expect(page.getByRole('button', { name: 'Tìm kiếm', exact: true })).toBeDisabled();
  await page.locator('#global-search-query').fill(marker); await page.locator('#global-search-type').selectOption('Document');
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click();
  const result = page.locator('.module-card').filter({ has: page.getByRole('heading', { name: title, exact: true }) });
  await expect(result).toBeVisible(); await result.getByRole('button', { name: 'Mở nguồn', exact: true }).click();
  await expect(page.locator('.resource-body-preview')).toHaveText('# Body\n' + marker);
  await page.getByRole('button', { name: 'Mở module nguồn', exact: true }).click();
  await card(page, title).getByRole('button', { name: 'Mở', exact: true }).click();
  await expect(page.locator('#document-body')).toHaveValue('# Body\n' + marker);
  expect(sql('Document', source.id)).toEqual(before);
  await page.getByRole('button', { name: 'Publish', exact: true }).click();
  await expect.poll(() => sql('Document', source.id)[0].Status).toBe('Published');
  await page.getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive document');
  await page.goto('/search'); await page.locator('#global-search-query').fill(marker); await page.locator('#global-search-type').selectOption('Document');
  await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Không có kết quả', exact: true })).toBeVisible();
  await page.getByLabel('Include archived source records').check(); await page.getByRole('button', { name: 'Tìm kiếm', exact: true }).click(); await expect(result).toBeVisible();
  expect(sql('Document', source.id)[0].Status).toBe('Archived');
});

test('FN-032 Dashboard: real recent document projection excludes body and archived source', async ({ page }) => {
  await page.goto('/'); const title = unique('dashboard-document'), body = unique('private-body-canary');
  const source = await api(page, 'createDocument', [title, 'Note', 'Markdown', body]); const before = sql('Document', source.id);
  await page.reload(); await expect(page.getByText(title, { exact: true }).first()).toBeVisible(); await expect(page.getByText(body, { exact: true })).toHaveCount(0);
  const dashboard = await api(page, 'getDashboard'); const widget = dashboard.widgets.find((w: any) => w.id === 'documents-recent');
  expect(widget.items.some((i: any) => i.id === source.id)).toBe(true);
  expect(JSON.stringify(dashboard).includes(body)).toBe(false); expect(sql('Document', source.id)).toEqual(before);
  await page.goto('/modules/FX20'); await card(page, title).getByRole('button', { name: 'Mở', exact: true }).click();
  await page.getByRole('button', { name: 'Publish', exact: true }).click(); await expect.poll(() => sql('Document', source.id)[0].Status).toBe('Published');
  await page.getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive document');
  await page.goto('/'); await expect(page.getByText(title, { exact: true })).toHaveCount(0);
  const refreshed = await api(page, 'getDashboard'); expect(refreshed.widgets.find((w: any) => w.id === 'documents-recent').items.some((i: any) => i.id === source.id)).toBe(false);
  expect(sql('Document', source.id)[0].Status).toBe('Archived');
});
