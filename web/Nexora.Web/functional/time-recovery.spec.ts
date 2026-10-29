import { test, expect, sql, card, unique } from './fixtures';
import { moduleApi, enableForTest } from './time-focus-helpers';
import type { Page, Route } from '@playwright/test';

type CapturedCommand = { path: string; body: string | null; key: string | undefined; etag: string | undefined; id: string };
// Execute the real handler/SQL commit first, then lose only its response. Retry
// also uses the real backend response; no synthesized successful response exists.
async function loseFirstCommittedResponse(page: Page, path: string, method: string) {
  const committed: CapturedCommand[] = [];
  const handle = async (route: Route) => {
    const request = route.request();
    if (new URL(request.url()).pathname !== path || request.method() !== method) { await route.continue(); return; }
    const response = await route.fetch();
    if (!response.ok()) { await route.fulfill({ response }); return; } // Preserve real CSRF refresh/denial handling.
    const result = await response.json();
    const headers = request.headers();
    committed.push({ path, body: request.postData(), key: headers['idempotency-key'], etag: headers['if-match'], id: result.id });
    if (committed.length === 1) await route.abort('failed');
    else await route.fulfill({ response });
  };
  await page.route('**/api/v1/time/**', handle);
  return { committed, close: () => page.unroute('**/api/v1/time/**', handle) };
}
async function entries(page: Page) {
  const rows: any[] = []; let cursor: string | null = null;
  for (let count = 0; count < 200; count++) { const batch = await moduleApi(page, 'timeApi', 'timeEntries', [false, cursor ?? undefined]); rows.push(...batch.items); if (!batch.nextCursor) return rows; cursor = batch.nextCursor; }
  throw new Error('Time entry pagination exceeded isolated-fixture bound');
}
async function showCard(page: Page, name: string) {
  for (let count = 0; count < 200; count++) {
    const target = card(page, name), more = page.getByRole('button', { name: 'Load more entries', exact: true });
    await expect.poll(async () => await target.isVisible() || await more.isVisible()).toBe(true);
    if (await target.isVisible()) return target;
    const previous = await page.locator('.resource-card').count(); await more.click();
    await expect.poll(() => page.locator('.resource-card').count()).toBeGreaterThan(previous);
  }
  throw new Error('Expected Time entry not found through visible pagination');
}
function assertExactReplay(commands: CapturedCommand[]) {
  expect(commands).toHaveLength(2); expect(commands[0].key).toMatch(/^[0-9a-f-]{36}$/i); expect(commands[0].id).toBeTruthy(); expect(commands[1]).toEqual(commands[0]);
}
async function stopExistingTimer(page: Page) {
  const running = await moduleApi(page, 'timeApi', 'timeTimer');
  if (running) await moduleApi(page, 'timeApi', 'timeStop', [running, crypto.randomUUID()]);
}

test('FX18 lost manual-create response: exact retry, one SQL record, metadata edit preserves millisecond instants', async ({ page, browser }) => {
  test.setTimeout(120000); await page.goto('/'); const enabled = await enableForTest(browser, page, 'FX18');
  const loss = await loseFirstCommittedResponse(page, '/api/v1/time/entries', 'POST'); const name = unique('time-recovery-manual');
  try {
    await page.goto('/modules/FX18'); await expect(page.getByRole('button', { name: 'Manual entry', exact: true })).toBeEnabled();
    await page.getByRole('button', { name: 'Manual entry', exact: true }).click();
    const modal = page.getByRole('dialog'); await modal.getByLabel('Description', { exact: true }).fill(name);
    await modal.getByLabel('Start (múi giờ trình duyệt)', { exact: true }).fill('2003-03-03T10:00:42.125');
    await modal.getByLabel('End (múi giờ trình duyệt)', { exact: true }).fill('2003-03-03T11:01:29.875'); await modal.getByRole('checkbox').check();
    await modal.getByRole('button', { name: 'Save', exact: true }).click();
    await expect(modal.getByRole('button', { name: 'Thử lại đúng thao tác', exact: true })).toBeVisible();
    expect(loss.committed).toHaveLength(1); const committedId = loss.committed[0].id;
    const before = sql('TimeEntry', committedId)[0]; expect(before.OwnerId.toLowerCase()).toBe(enabled.me.personalSpaceId.toLowerCase()); expect(before.Description).toBe(name);
    await expect(modal.getByLabel('Description', { exact: true })).toBeDisabled(); await expect(modal.getByRole('button', { name: 'Hủy', exact: true })).toBeDisabled();
    await modal.screenshot({ path: `artifacts/time-recovery-${test.info().project.name}-manual-pending.png` });
    await page.keyboard.press('Escape'); await expect(modal).toBeVisible();
    await modal.getByRole('button', { name: 'Thử lại đúng thao tác', exact: true }).click(); await expect(modal).toHaveCount(0); assertExactReplay(loss.committed);
    expect((await entries(page)).filter(row => row.description === name)).toHaveLength(1);
    const original = await moduleApi(page, 'timeApi', 'timeGet', [committedId]); expect(original.startAt).toContain('.125'); expect(original.endAt).toContain('.875');
    await (await showCard(page, name)).getByRole('button', { name: 'Edit entry', exact: true }).click(); await page.getByRole('dialog').getByLabel('Description', { exact: true }).fill(name + '-edited');
    // Repeated viewport/rerun fixtures intentionally share this interval; honor the
    // actual overlap confirmation contract for metadata updates as well.
    await page.getByRole('dialog').getByRole('checkbox').check();
    await page.getByRole('dialog').getByRole('button', { name: 'Save', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
    const edited = await moduleApi(page, 'timeApi', 'timeGet', [committedId]); expect(edited.startAt).toBe(original.startAt); expect(edited.endAt).toBe(original.endAt); expect(edited.durationMilliseconds).toBe(original.durationMilliseconds);
    const after = sql('TimeEntry', committedId)[0]; expect(after.StartAt).toBe(before.StartAt); expect(after.EndAt).toBe(before.EndAt); expect(after.Description).toBe(name + '-edited');
  } finally { await loss.close(); await enabled.close(); }
});

test('FX18 lost Stop response: original key/ETag replay retains one stopped SQL entry', async ({ page, browser }) => {
  test.setTimeout(120000); await page.goto('/'); const enabled = await enableForTest(browser, page, 'FX18'); let loss: Awaited<ReturnType<typeof loseFirstCommittedResponse>> | undefined;
  try {
    await stopExistingTimer(page); const name = unique('time-recovery-stop'); const running = await moduleApi(page, 'timeApi', 'timeStart', [name, null, crypto.randomUUID()]);
    loss = await loseFirstCommittedResponse(page, `/api/v1/time/timer/${running.id}/stop`, 'POST'); await page.goto('/modules/FX18');
    await page.getByRole('button', { name: 'Stop timer', exact: true }).click(); const modal = page.getByRole('dialog', { name: 'Đối chiếu thao tác thời gian', exact: true }); await expect(modal).toBeVisible();
    const before = sql('TimeEntry', running.id)[0]; expect(before.Status).toBe('Stopped'); expect(loss.committed[0].etag).toBe(running.etag);
    await expect(modal.getByRole('button', { name: 'Hủy', exact: true })).toBeDisabled(); await page.keyboard.press('Escape'); await expect(modal).toBeVisible();
    await modal.screenshot({ path: `artifacts/time-recovery-${test.info().project.name}-stop-pending.png` });
    await modal.getByRole('button', { name: 'Thử lại thao tác thời gian', exact: true }).click(); await expect(modal).toHaveCount(0); assertExactReplay(loss.committed);
    const after = sql('TimeEntry', running.id)[0]; expect(after.EndAt).toBe(before.EndAt); expect(after.Status).toBe('Stopped'); expect((await entries(page)).filter(row => row.description === name)).toHaveLength(1); expect(await moduleApi(page, 'timeApi', 'timeTimer')).toBeNull();
    // Timer instants originate in DateTimeOffset/SQL datetime2(7), rather than
    // being rounded synthetic millisecond inputs. Metadata must retain them raw.
    const original = await moduleApi(page, 'timeApi', 'timeGet', [running.id]);
    await (await showCard(page, name)).getByRole('button', { name: 'Edit entry', exact: true }).click();
    await page.getByRole('dialog').getByLabel('Description', { exact: true }).fill(name + '-metadata');
    await page.getByRole('dialog').getByRole('button', { name: 'Save', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
    const edited = await moduleApi(page, 'timeApi', 'timeGet', [running.id]); expect(edited.startAt).toBe(original.startAt); expect(edited.endAt).toBe(original.endAt);
    const afterMetadata = sql('TimeEntry', running.id)[0]; expect(afterMetadata.StartAt).toBe(before.StartAt); expect(afterMetadata.EndAt).toBe(before.EndAt);
  } finally { await loss?.close(); await enabled.close(); }
});

test('FX18 lost Resume response: exact original source/key creates one new timer without rewriting source', async ({ page, browser }) => {
  test.setTimeout(120000); await page.goto('/'); const enabled = await enableForTest(browser, page, 'FX18'); let loss: Awaited<ReturnType<typeof loseFirstCommittedResponse>> | undefined; let resumedId: string | undefined;
  try {
    await stopExistingTimer(page); const name = unique('time-recovery-resume'); const source = await moduleApi(page, 'timeApi', 'timeSave', [{ startAt: '2003-03-04T10:00:00Z', endAt: '2003-03-04T11:00:00Z', description: name, category: null, confirmOverlap: true }, crypto.randomUUID()]); const sourceBefore = sql('TimeEntry', source.id)[0];
    loss = await loseFirstCommittedResponse(page, '/api/v1/time/timer', 'POST'); await page.goto('/modules/FX18'); await (await showCard(page, name)).getByRole('button', { name: 'Resume as new timer', exact: true }).click();
    const modal = page.getByRole('dialog', { name: 'Đối chiếu thao tác thời gian', exact: true }); await expect(modal).toBeVisible(); resumedId = loss.committed[0].id;
    expect(resumedId).not.toBe(source.id); expect(JSON.parse(loss.committed[0].body!).resumeEntryId).toBe(source.id); expect(sql('TimeEntry', resumedId!)[0].Status).toBe('Running');
    await modal.screenshot({ path: `artifacts/time-recovery-${test.info().project.name}-resume-pending.png` });
    await modal.getByRole('button', { name: 'Thử lại thao tác thời gian', exact: true }).click(); await expect(modal).toHaveCount(0); assertExactReplay(loss.committed);
    // Entry list deliberately exposes Stopped only; running state has its own action.
    const records = (await entries(page)).filter(row => row.description === name); expect(records).toHaveLength(1); expect(records[0].id).toBe(source.id);
    expect(sql('TimeEntry', source.id)[0]).toEqual(sourceBefore); expect((await moduleApi(page, 'timeApi', 'timeTimer')).id).toBe(resumedId);
  } finally { await loss?.close(); resumedId ??= loss?.committed[0]?.id; if (resumedId) { const timer = await moduleApi(page, 'timeApi', 'timeTimer'); if (timer?.id === resumedId) await moduleApi(page, 'timeApi', 'timeStop', [timer, crypto.randomUUID()]); } await enabled.close(); }
});
