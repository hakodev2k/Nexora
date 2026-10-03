import { createHash } from 'node:crypto';
import { test, expect, api, confirm } from './fixtures';
import type { Page } from '@playwright/test';

const output = (page: Page) => page.getByLabel('Tool output', { exact: true });
async function open(page: Page, code: string) {
  await page.goto('/modules/FX32');
  await page.locator('#tool-code').selectOption(code);
}
async function run(page: Page, expectedStatus = 200) {
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/developer/tools/run') && r.request().method() === 'POST');
  await page.getByRole('button', { name: 'Chạy tool', exact: true }).click();
  expect((await response).status()).toBe(expectedStatus);
  await expect(page.getByRole('button', { name: 'Chạy tool', exact: true })).toBeEnabled();
}

test('FN-019 Toolbox Base64: Unicode round-trip, invalid decode recovery and confirmed clear', async ({ page }) => {
  await open(page, 'base64');
  const input = 'Tiếng Việt 😀\n<&> synthetic';
  await page.locator('#tool-input').fill(input); await run(page);
  const encoded = Buffer.from(input, 'utf8').toString('base64');
  await expect(output(page)).toHaveText(encoded);
  await page.locator('#tool-operation').selectOption('decode');
  await page.locator('#tool-input').fill(encoded); await run(page);
  await expect(output(page)).toHaveText(input);
  await page.locator('#tool-input').fill('%%% invalid'); await run(page, 422);
  await expect(page.locator('#tool-input')).toHaveValue('%%% invalid');
  await expect(output(page)).toHaveCount(0);
  await page.locator('#tool-input').fill(encoded); await run(page);
  await page.getByRole('button', { name: 'Xóa', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click();
  await expect(page.locator('#tool-input')).toHaveValue(encoded);
  await page.getByRole('button', { name: 'Xóa', exact: true }).click(); await confirm(page, 'Xóa input/output');
  await expect(page.locator('#tool-input')).toHaveValue(''); await expect(output(page)).toHaveCount(0);
});

test('FN-020 Toolbox URL: exact codec output never navigates or fetches pasted URL', async ({ page }) => {
  await open(page, 'url-codec');
  const value = 'https://example.invalid/a?x=Tiếng Việt&y=😀#fragment';
  const outbound: string[] = [];
  page.on('request', request => { if (new URL(request.url()).hostname === 'example.invalid') outbound.push(request.url()); });
  await page.locator('#tool-input').fill(value); await run(page);
  const encoded = encodeURIComponent(value); await expect(output(page)).toHaveText(encoded);
  await page.locator('#tool-operation').selectOption('decode'); await page.locator('#tool-input').fill(encoded); await run(page);
  await expect(output(page)).toHaveText(value); await expect(page).toHaveURL(/\/modules\/FX32$/);
  expect(outbound).toEqual([]);
});

test('FN-021 Toolbox HTML: decoded hostile content remains literal text', async ({ page }) => {
  await open(page, 'html-codec'); await page.locator('#tool-operation').selectOption('decode');
  const literal = '<img src=x onerror="window.__toolboxExecuted=true"><script>window.__toolboxExecuted=true</script>';
  await page.locator('#tool-input').fill(literal.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;'));
  await run(page); await expect(output(page)).toHaveText(literal);
  await expect(output(page).locator('img,script')).toHaveCount(0);
  expect(await page.evaluate(() => (window as any).__toolboxExecuted)).toBeUndefined();
});

test('FN-022 Toolbox Hash: all five algorithms match independent oracle and legacy warning', async ({ page }) => {
  await open(page, 'hash'); const value = 'abc Tiếng Việt 😀'; await page.locator('#tool-input').fill(value);
  for (const [label, algorithm] of [['SHA-256', 'sha256'], ['SHA-384', 'sha384'], ['SHA-512', 'sha512'], ['MD5', 'md5'], ['SHA-1', 'sha1']]) {
    await page.locator('#tool-algorithm').selectOption(label); await run(page);
    await expect(output(page)).toHaveText(createHash(algorithm).update(value, 'utf8').digest('hex'));
    if (algorithm === 'md5' || algorithm === 'sha1') await expect(page.getByText(/Legacy checksum only/)).toBeVisible();
    else await expect(page.getByText(/Legacy checksum only/)).toHaveCount(0);
  }
});

test('FN-023 Toolbox UUID: count boundaries, version, uniqueness and failed run clears old success', async ({ page }) => {
  await open(page, 'uuid');
  for (const count of [1, 20]) {
    await page.locator('#tool-count').fill(String(count)); await run(page);
    const values = (await output(page).innerText()).split('\n'); expect(values).toHaveLength(count);
    expect(new Set(values).size).toBe(count);
    for (const value of values) expect(value).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
  }
  await page.locator('#tool-count').fill('21'); await run(page, 422);
  await expect(output(page)).toHaveCount(0); await expect(page.locator('#tool-count')).toHaveValue('21');
  await page.locator('#tool-count').fill('2'); await run(page); await expect(output(page)).toBeVisible();
});

test('FN-024 Toolbox Password: bounds and memory-only reset without exposing generated values', async ({ page }) => {
  await open(page, 'password');
  for (const length of [15, 128]) {
    await page.locator('#tool-length').fill(String(length)); await run(page);
    // Assert only shape/length; never include a generated password in an error or attachment.
    const safeShape = await output(page).evaluate((node, size) => node.textContent?.length === size && /^[ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*_-]+$/.test(node.textContent ?? ''), length);
    expect(safeShape).toBe(true);
  }
  await page.locator('#tool-length').fill('14'); await run(page, 422); await expect(output(page)).toHaveCount(0);
  await page.reload(); await expect(output(page)).toHaveCount(0);
});

test('FN-025 Toolbox JSON: semantic format/compact, parser error, retained input and recovery', async ({ page }) => {
  await open(page, 'json'); const value = '{"text":"Tiếng Việt","array":[1,true,null],"nested":{"x":2}}';
  await page.locator('#tool-input').fill(value); await run(page);
  expect(JSON.parse(await output(page).innerText())).toEqual(JSON.parse(value));
  expect((await output(page).innerText()).includes('\n')).toBe(true);
  await page.getByLabel('Indent output').uncheck(); await run(page);
  expect(JSON.parse(await output(page).innerText())).toEqual(JSON.parse(value));
  expect((await output(page).innerText()).includes('\n')).toBe(false);
  await page.locator('#tool-input').fill('{invalid'); await run(page, 422);
  await expect(page.locator('#tool-input')).toHaveValue('{invalid'); await expect(output(page)).toHaveCount(0);
  await page.locator('#tool-input').fill(value); await run(page); await expect(output(page)).toBeVisible();
});

test('FN-026 Toolbox Regex: matching, ignore-case, truncation, invalid pattern and bounded timeout', async ({ page }) => {
  await open(page, 'regex'); await page.locator('#tool-pattern').fill('hello'); await page.locator('#tool-input').fill('Hello hello HELLO');
  await run(page); expect(JSON.parse(await output(page).innerText()).count).toBe(1);
  await page.getByLabel('Ignore case').check(); await run(page);
  const result = JSON.parse(await output(page).innerText()); expect(result.matches.map((m: any) => m.index)).toEqual([0, 6, 12]);
  await page.locator('#tool-pattern').fill('a'); await page.locator('#tool-input').fill('a'.repeat(101)); await run(page);
  const capped = JSON.parse(await output(page).innerText()); expect(capped.count).toBe(100); expect(capped.truncated).toBe(true);
  await page.locator('#tool-pattern').fill('['); await run(page, 422); await expect(output(page)).toHaveCount(0);
  await page.locator('#tool-pattern').fill('^(a+)+$'); await page.locator('#tool-input').fill('a'.repeat(5000) + '!');
  const response = page.waitForResponse(r => r.url().endsWith('/api/v1/developer/tools/run'));
  await run(page, 422); expect((await response).request().method()).toBe('POST');
  expect((await (await response).json()).code).toBe('RegexTimeout');
  await expect(page.locator('#tool-input')).toHaveValue('a'.repeat(5000) + '!');
  await page.locator('#tool-pattern').fill('a+'); await page.locator('#tool-input').fill('aaa'); await run(page);
  expect(JSON.parse(await output(page).innerText()).count).toBe(1);
});

test('FN-027 Toolbox catalog: supported IDs, no persistent canary and unsupported network gate', async ({ page }) => {
  await open(page, 'base64');
  const catalog = await api(page, 'listDeveloperTools');
  expect(catalog.items.map((t: any) => t.code).sort()).toEqual(['base64', 'hash', 'html-codec', 'json', 'password', 'regex', 'url-codec', 'uuid']);
  const canary = 'synthetic-toolbox-memory-canary'; await page.locator('#tool-input').fill(canary); await run(page);
  expect(await page.evaluate(value => [...Object.values(localStorage), ...Object.values(sessionStorage)].some(item => item.includes(value)), canary)).toBe(false);
  const unsupported = await page.evaluate(async () => {
    const path = '/src/api.ts'; const m = await import(path);
    try { await m.runDeveloperTool('http', 'https://example.invalid', {}); return { status: 200, code: 'Success' }; }
    catch (e: any) { return { status: e.status, code: e.code }; }
  });
  expect(unsupported.status).toBe(404); expect(unsupported.code).toBe('ToolUnavailable');
  await page.reload(); await expect(page.locator('#tool-input')).toHaveValue(''); await expect(output(page)).toHaveCount(0);
});
