import { mkdirSync } from 'node:fs';
import { test, expect, api, sql, unique, login } from './fixtures';
import { enableForTest } from './time-focus-helpers';

const base = '/api/v1/organization/collections';
async function collectionApi(page: import('@playwright/test').Page, name: string, args: unknown[]) {
  return page.evaluate(async ({ name, args }) => { const path = '/src/collectionApi.ts'; const module = await import(path); return module[name](...args); }, { name, args });
}

// E24b / FX24-BR004, AC003: existing reference container only. Real UI commands
// and SQL evidence; no new providers or implicit source permissions.
test('Collections: real membership order, rename, foreign-owner denial and delete preserves sources', async ({ page, browser }) => {
  test.setTimeout(180_000); page.setDefaultTimeout(15_000); await page.goto('/');
  const owner = await enableForTest(browser, page, 'FX24');
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  let foreignAccess: Awaited<ReturnType<typeof enableForTest>> | undefined;
  let scenarioFailure: unknown;
  try {
    const name = unique('collection'), firstName = unique('collection-source-A'), secondName = unique('collection-source-B');
    const start = new Date(Date.now() + 86400000).toISOString(), end = new Date(Date.now() + 172800000).toISOString();
    const first = await api(page, 'createProject', [firstName, 'Owned synthetic collection source', start, end]);
    const second = await api(page, 'createProject', [secondName, 'Owned synthetic collection source', start, end]);
    const sourceBefore = [sql('Project', first.id), sql('Project', second.id)];
    await page.goto('/collections'); await expect(page.getByRole('heading', { name: 'Collections', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'Tạo Collection', exact: true }).click();
    let modal = page.getByRole('dialog'); await expect(modal.getByRole('button', { name: 'Lưu', exact: true })).toBeDisabled();
    await modal.getByLabel('Tên (1–100 ký tự)', { exact: true }).fill(name);
    await modal.getByLabel('Mô tả (tùy chọn)', { exact: true }).fill('Synthetic ordered references');
    const created = page.waitForResponse(r => new URL(r.url()).pathname === base && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click();
    const response = await created; expect(response.status()).toBe(201); const ack = await response.json();
    expect(ack.committed).toBe(true); expect(ack.detailAvailable).toBe(false); await expect(modal).toHaveCount(0);
    expect(sql('Collection', ack.id)[0].OwnerId.toLowerCase()).toBe(owner.me.personalSpaceId.toLowerCase());
    await expect(page.getByRole('heading', { name, exact: true })).toBeVisible();
    for (const title of [firstName, secondName]) {
      await page.getByRole('button', { name: 'Thêm nguồn hiện có', exact: true }).click(); modal = page.getByRole('dialog');
      await modal.getByLabel('Loại nguồn', { exact: true }).selectOption('Project');
      await modal.getByLabel('Tìm tên nguồn', { exact: true }).fill(title);
      await modal.getByRole('button', { name: 'Tìm nguồn', exact: true }).click();
      const candidate = modal.locator('li').filter({ hasText: title }); await expect(candidate).toHaveCount(1);
      await candidate.getByRole('button', { name: 'Thêm', exact: true }).click(); await expect(modal).toHaveCount(0);
      await expect(page.locator('.collections-members').getByText(title, { exact: true })).toBeVisible();
    }
    await page.getByRole('button', { name: `Di chuyển ${secondName} lên`, exact: true }).click();
    await expect.poll(async () => (await collectionApi(page, 'getCollection', [ack.id])).members.map((m: any) => m.resourceId)).toEqual([second.id, first.id]);
    await page.reload(); await expect(page.locator('.collections-members li').first()).toContainText(secondName);
    await expect(page.getByRole('button', { name: `Di chuyển ${secondName} lên`, exact: true })).toBeDisabled();
    await page.getByRole('button', { name: 'Sửa', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByLabel('Tên (1–100 ký tự)', { exact: true }).fill(name + '-edited');
    await page.keyboard.press('Escape'); await modal.getByRole('button', { name: 'Tiếp tục sửa', exact: true }).click();
    await expect(modal.getByLabel('Tên (1–100 ký tự)', { exact: true })).toHaveValue(name + '-edited');
    await modal.getByRole('button', { name: 'Lưu', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('Collection', ack.id)[0].Name).toBe(name + '-edited');
    mkdirSync('artifacts/collections-review', { recursive: true });
    await page.screenshot({ path: `artifacts/collections-review/${test.info().project.name}-populated.png`, fullPage: false });
    const other = await foreign.newPage(); await login(other, 'UserB'); await other.goto('/');
    foreignAccess = await enableForTest(browser, other, 'FX24');
    expect((await collectionApi(other, 'collectionCapabilities', []) )['organization.collection.read']).toBe(true);
    const denied = await other.evaluate(async id => { const path = '/src/collectionApi.ts', module = await import(path); try { await module.getCollection(id); return 200; } catch (e: any) { return e.status; } }, ack.id);
    expect(denied).toBe(404); expect(JSON.stringify(await collectionApi(other, 'listCollections', [name, 1]))).not.toContain(ack.id);
    expect((await api(other, 'getMe')).id).not.toBe(owner.me.id);
    const foreignTitle = unique('foreign-collection-source');
    const foreignProject = await api(other, 'createProject', [foreignTitle, 'Foreign synthetic source stays private', start, end]);
    const foreignBefore = sql('Project', foreignProject.id);
    expect(await collectionApi(page, 'collectionCandidates', ['Project', foreignTitle])).toEqual([]);
    const current = await collectionApi(page, 'getCollection', [ack.id]);
    const deniedAttach = await page.evaluate(async intent => {
      const path = '/src/collectionApi.ts', module = await import(path);
      try { await module.sendCollectionIntent(intent); return 200; } catch (error: any) { return error.status; }
    }, { path: `/${ack.id}/members`, method: 'POST', body: JSON.stringify({ resourceType: 'Project', resourceId: foreignProject.id }), etag: current.collection.etag, key: crypto.randomUUID(), expectedId: ack.id });
    expect(deniedAttach).toBe(404);
    expect((await collectionApi(page, 'getCollection', [ack.id])).members).toEqual(current.members);
    expect(sql('Project', foreignProject.id)).toEqual(foreignBefore);
    const member = page.locator('.collections-members li').filter({ hasText: firstName });
    await member.getByRole('button', { name: 'Gỡ tham chiếu', exact: true }).click();
    await expect.poll(async () => (await collectionApi(page, 'getCollection', [ack.id])).members.length).toBe(1);
    await page.getByRole('button', { name: 'Xóa Collection', exact: true }).click(); modal = page.getByRole('dialog');
    await modal.getByRole('button', { name: 'Xóa Collection, giữ nguồn', exact: true }).click(); await expect(modal).toHaveCount(0);
    expect(sql('Collection', ack.id)).toHaveLength(0);
    expect([sql('Project', first.id), sql('Project', second.id)]).toEqual(sourceBefore);
    await page.reload(); await expect(page.getByRole('heading', { name: name + '-edited', exact: true })).toHaveCount(0);
  } catch (error) { scenarioFailure = error; throw error; }
  finally {
    const cleanupFailures: unknown[] = [];
    for (const cleanup of [() => foreignAccess?.close(), () => foreign.close(), () => owner.close()]) {
      try { await cleanup(); } catch (error) { cleanupFailures.push(error); }
    }
    if (cleanupFailures.length) throw new AggregateError([...(scenarioFailure ? [scenarioFailure] : []), ...cleanupFailures], 'Owned Collections test cleanup failed.');
  }
});
