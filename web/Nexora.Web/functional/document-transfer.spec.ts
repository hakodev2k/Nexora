import { mkdirSync, writeFileSync } from 'node:fs';
import { inflateRawSync } from 'node:zlib';
import { createHash } from 'node:crypto';
import { test, expect, api, sql, unique, login, card } from './fixtures';
import { response } from './time-focus-helpers';
import type { Browser, Page } from '@playwright/test';

function frozenKey(headers: Record<string, string>) {
  const key = headers['idempotency-key']; expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);
  return key as ReturnType<typeof crypto.randomUUID>;
}

async function access(browser: Browser, page: Page) {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const me = await api(page, 'getMe'); const codes = ['FX07', 'FX10', 'FX20'];
  const modules = (await api(admin, 'listAdminModules')).items.filter((m: any) => codes.includes(m.code));
  expect(modules).toHaveLength(3); for (const m of modules) expect(m.state).toBe('Ready');
  const originals = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.filter((m: any) => codes.includes(m.code));
  // Safe synthetic restoration checkpoint survives a runner/runtime crash;
  // no passwords, cookies, headers, preview tokens or storage paths included.
  mkdirSync('artifacts/document-transfer-review', { recursive: true });
  writeFileSync(`artifacts/document-transfer-review/${test.info().project.name}-${me.id}-access-before.json`, JSON.stringify({ userId: me.id, modules: modules.map((m: any) => ({ id: m.id, code: m.code, systemEnabled: m.systemEnabled, registrationEnabled: m.registrationEnabled })), grants: originals.map((m: any) => ({ moduleId: m.moduleId, code: m.code, enabled: m.enabled })) }, null, 2));
  const policy = async (m: any, systemEnabled: boolean, registrationEnabled: boolean) => {
    const current = (await api(admin, 'listAdminModules')).items.find((x: any) => x.id === m.id);
    if (current.systemEnabled === systemEnabled && current.registrationEnabled === registrationEnabled) return;
    const change = { systemEnabled, registrationEnabled }; const preview = await api(admin, 'previewModulePolicy', [m.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [m.id, preview.etag, change, preview.previewToken]);
  };
  const grants = async (changes: { moduleId: string; enabled: boolean }[]) => {
    const current = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants;
    changes = changes.filter(c => current.find((m: any) => m.moduleId === c.moduleId).enabled !== c.enabled); if (!changes.length) return;
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  const close = async () => {
    const errors: unknown[] = [];
    const verify = async () => {
      const currentModules = (await api(admin, 'listAdminModules')).items;
      const currentGrants = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants;
      for (const m of modules) { const current = currentModules.find((x: any) => x.id === m.id); expect(current.state).toBe('Ready'); expect(current.systemEnabled).toBe(m.systemEnabled); expect(current.registrationEnabled).toBe(m.registrationEnabled); }
      for (const g of originals) expect(currentGrants.find((x: any) => x.moduleId === g.moduleId).enabled).toBe(g.enabled);
    };
    const cleanup = [() => grants(originals.map((g: any) => ({ moduleId: g.moduleId, enabled: g.enabled }))), ...[...modules].reverse().map(m => () => policy(m, m.systemEnabled, m.registrationEnabled)), verify, () => context.close()];
    for (const action of cleanup) { try { await action(); } catch (error) { errors.push(error); } }
    if (errors.length) throw new AggregateError(errors, 'Transfer access restoration failed');
  };
  try { for (const m of modules) await policy(m, true, true); await grants(originals.map((g: any) => ({ moduleId: g.moduleId, enabled: true }))); }
  catch (error) { try { await close(); } catch (cleanup) { throw new AggregateError([error, cleanup], 'Transfer setup and restoration failed'); } throw error; }
  return { me, close, set: (code: string, enabled: boolean) => grants([{ moduleId: originals.find((g: any) => g.code === code).moduleId, enabled }]) };
}

// Inspect the actual bounded generated OOXML download, without an external
// converter, package extraction, file path access or provider call.
function documentXml(bytes: Buffer) {
  expect(bytes.length).toBeLessThanOrEqual(5 * 1024 * 1024);
  const end = bytes.lastIndexOf(Buffer.from([0x50, 0x4b, 0x05, 0x06])); expect(end).toBeGreaterThan(0);
  const count = bytes.readUInt16LE(end + 10); expect(count).toBeLessThan(100);
  let at = bytes.readUInt32LE(end + 16), result = '';
  for (let i = 0; i < count; i++) {
    expect(bytes.readUInt32LE(at)).toBe(0x02014b50);
    const nameLength = bytes.readUInt16LE(at + 28), extra = bytes.readUInt16LE(at + 30), comment = bytes.readUInt16LE(at + 32);
    const name = bytes.subarray(at + 46, at + 46 + nameLength).toString('utf8');
    expect(name).not.toMatch(/(?:^\/|\.\.|vbaProject|\.js$)/i);
    if (name === 'word/document.xml') {
      const local = bytes.readUInt32LE(at + 42), method = bytes.readUInt16LE(at + 10), size = bytes.readUInt32LE(at + 20);
      expect(bytes.readUInt32LE(local)).toBe(0x04034b50);
      const offset = local + 30 + bytes.readUInt16LE(local + 26) + bytes.readUInt16LE(local + 28);
      const compressed = bytes.subarray(offset, offset + size); expect([0, 8]).toContain(method);
      result = (method === 8 ? inflateRawSync(compressed, { maxOutputLength: 5 * 1024 * 1024 }) : compressed).toString('utf8');
    }
    at += 46 + nameLength + extra + comment;
  }
  expect(result).toContain('<w:document'); expect(result).not.toMatch(/<!DOCTYPE|<!ENTITY/); return result;
}
async function capture(page: Page, suffix: string) {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  mkdirSync('artifacts/document-transfer-review', { recursive: true });
  await page.screenshot({ path: `artifacts/document-transfer-review/${test.info().project.name}-${suffix}.png`, fullPage: false });
}

for (const format of ['MD', 'DOCX'] as const) test(`Documents transfer ${format}: real export bytes, scanned import preview/manual save/replay and current-owner denial`, async ({ page, browser }) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000); await page.goto('/');
  const owner = await access(browser, page); const otherContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  let otherAccess: Awaited<ReturnType<typeof access>> | undefined;
  let scenarioError: unknown;
  try {
    const title = unique(`transfer-${format}`), importedTitle = unique(`imported-${format}`);
    const body = `# Transfer heading\n\nSynthetic content ${title}\n\n- First item\n- Second item\n\n**Basic emphasis** and literal code.\n`;
    await page.goto('/modules/FX20'); await page.locator('#document-title').fill(title); await page.locator('#document-type').selectOption('Document'); await page.locator('#document-editor').selectOption('Markdown'); await page.locator('#document-body').fill(body);
    const workspace = page.locator('.documents-workspace'), editor = workspace.locator(':scope > form');
    const workspaceBox = await workspace.boundingBox(), editorBox = await editor.boundingBox();
    expect(workspaceBox).toBeTruthy(); expect(editorBox!.width).toBeGreaterThanOrEqual(workspaceBox!.width * .95);
    expect(await editor.evaluate(element => getComputedStyle(element).position)).toBe('static');
    await page.locator('#document-title').scrollIntoViewIfNeeded(); await capture(page, `${format}-workspace`);
    const create = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/documents' && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Tạo page', exact: true }).click(); const created = await create; expect(created.status()).toBe(201);
    const source = await created.json(); const before = sql('Document', source.id); expect(before[0].OwnerId.toLowerCase()).toBe(owner.me.personalSpaceId.toLowerCase());
    expect((await response(page, `/api/v1/documents/${source.id}/exports/preview`, 'POST', { versionNumber: 1, format: 'PDF' }, source.etag)).status).toBe(422);
    await page.getByRole('button', { name: 'Export bản đã lưu v1', exact: true }).first().click();
    const modal = page.getByRole('dialog', { name: 'Export bản đã lưu v1', exact: true });
    await expect(modal.getByRole('button', { name: 'Tạo file xuất', exact: true })).toBeDisabled(); await modal.getByRole('combobox', { name: /^Format(?:\s|$)/ }).selectOption(format);
    const previewed = page.waitForResponse(r => r.url().endsWith(`/documents/${source.id}/exports/preview`) && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Xem trước chuyển đổi', exact: true }).click(); const preview = await (await previewed).json(); expect(preview.pageId).toBe(source.id); expect(preview.format).toBe(format);
    expect(sql('Document', source.id)).toEqual(before); await expect(modal.getByRole('button', { name: 'Tạo file xuất', exact: true })).toBeDisabled();
    for (const checkbox of await modal.getByRole('checkbox').all()) await checkbox.check();
    const confirmation = await modal.locator('.document-export-confirmation').last().boundingBox();
    const generateButton = await modal.getByRole('button', { name: 'Tạo file xuất', exact: true }).boundingBox();
    expect(generateButton!.y).toBeGreaterThanOrEqual(confirmation!.y + confirmation!.height + 8);
    expect(generateButton!.height).toBeGreaterThanOrEqual(44);
    await capture(page, `${format}-export-preview`);
    const generation = page.waitForResponse(r => r.url().endsWith(`/documents/${source.id}/exports`) && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Tạo file xuất', exact: true }).click(); const generated = await generation; expect(generated.status()).toBe(201); const ack = await generated.json(); expect(ack.committed).toBe(true); expect(ack.detailAvailable).toBe(false);
    const request = generated.request(); const repeated = await response(page, `/api/v1/documents/${source.id}/exports`, 'POST', request.postDataJSON(), request.headers()['if-match'], frozenKey(request.headers())); expect(repeated.status).toBe(201); expect(repeated.body).toEqual(ack);
    const job = sql('DocumentExportJob', ack.id)[0]; expect(job.OwnerId.toLowerCase()).toBe(owner.me.personalSpaceId.toLowerCase()); expect(job.PageId.toLowerCase()).toBe(source.id.toLowerCase()); expect(job.Format).toBe(format);
    await modal.getByRole('button', { name: 'Xem trạng thái hiện tại', exact: true }).click(); await expect(modal.getByText(/Sẵn sàng tải · hết hạn/)).toBeVisible();
    const downloadEvent = page.waitForEvent('download'); await modal.getByRole('button', { name: 'Tải file với quyền hiện tại', exact: true }).click(); const download = await downloadEvent; expect(download.suggestedFilename()).toBe(`document-v1.${format.toLowerCase()}`);
    const stream = await download.createReadStream(); expect(stream).toBeTruthy(); const chunks: Buffer[] = []; for await (const chunk of stream!) chunks.push(Buffer.from(chunk)); const bytes = Buffer.concat(chunks);
    if (format === 'MD') expect(bytes.toString('utf8')).toBe(body); else { const xml = documentXml(bytes); expect(xml).toContain(title); expect(xml).toContain('First item'); expect(xml).toContain('Basic emphasis'); }
    const digest = createHash('sha256').update(bytes).digest('hex').toUpperCase(); expect(job.ArtifactLength).toBe(bytes.length); expect(job.ArtifactDigest).toBe(digest);
    writeFileSync(`artifacts/document-transfer-review/${test.info().project.name}-${format}-export-proof.json`, JSON.stringify({ pageId: source.id, jobId: ack.id, format, versionNumber: 1, byteLength: bytes.length, sha256: digest, sourceUnchanged: true, warnings: preview.warnings }, null, 2));
    expect(sql('Document', source.id)).toEqual(before);
    const other = await otherContext.newPage(); await login(other, 'UserB'); otherAccess = await access(browser, other);
    expect((await api(other, 'getMe')).id).not.toBe(owner.me.id);
    for (const path of [`/api/v1/document-exports/${ack.id}`, `/api/v1/document-exports/${ack.id}/content`]) { const denied = await response(other, path, 'GET'); expect(denied.status).toBe(404); expect(denied.body.code).toBe('ResourceUnavailable'); }
    await owner.set('FX10', false);
    try {
      const denial = page.waitForResponse(r => r.url().endsWith(`/document-exports/${ack.id}/content`));
      await modal.getByRole('button', { name: 'Tải file với quyền hiện tại', exact: true }).click(); const denied = await denial; expect(denied.status()).toBe(403); expect((await denied.json()).code).toBe('ModuleUnavailable');
      await expect(modal).toHaveCount(0); await expect(page.locator('#document-title')).toHaveValue('');
    }
    finally { await owner.set('FX10', true); }
    await page.goto('/files'); const fileName = unique(`roundtrip-${format}`) + '.' + format.toLowerCase();
    await page.getByLabel('Chọn file', { exact: true }).setInputFiles({ name: fileName, mimeType: format === 'MD' ? 'text/markdown' : 'application/vnd.openxmlformats-officedocument.wordprocessingml.document', buffer: bytes });
    const uploaded = page.waitForResponse(r => /\/files\/upload-sessions\/[^/]+\/content$/.test(new URL(r.url()).pathname) && r.request().method() === 'PUT');
    await page.getByRole('button', { name: 'Upload và scan', exact: true }).click(); const upload = await uploaded; expect(upload.ok()).toBe(true); const file = await upload.json(); expect(file.scanState).toBe('Clean'); expect(file.lifecycle).toBe('Active');
    const foreignPreview = await response(other, '/api/v1/document-imports/preview', 'POST', { fileId: file.id, fileETag: file.etag, editorMode: 'Markdown' }); expect(foreignPreview.status).toBe(404);
    await page.goto('/modules/FX20'); await page.locator('#document-title').fill(importedTitle); await page.locator('#document-type').selectOption('Note'); await page.locator('#document-editor').selectOption('Markdown');
    const panel = page.getByRole('region', { name: 'Import trang mới', exact: true });
    await panel.getByRole('button', { name: 'Chọn / xem lại file nguồn', exact: true }).click(); await panel.getByRole('combobox', { name: /^Loại file(?:\s|$)/ }).selectOption(format); await panel.getByLabel('Tìm tên file', { exact: true }).fill(fileName); await panel.getByRole('button', { name: 'Tải Files sạch', exact: true }).click();
    await panel.getByRole('button', { name: fileName + ' · ' + bytes.length + ' byte', exact: true }).click(); const converting = page.waitForResponse(r => r.url().endsWith('/document-imports/preview') && r.request().method() === 'POST'); await panel.getByRole('button', { name: 'Preview chuyển đổi', exact: true }).click(); const converted = await (await converting).json(); expect(converted.parserVersion).toBe(1);
    if (format === 'MD') expect(converted.body).toBe(body);
    else expect(converted.body).toContain(title.replaceAll('-', '\\-'));
    await expect(panel.locator('.document-preview').getByText('Synthetic content ' + title, { exact: true })).toBeVisible();
    expect((await response(page, '/api/v1/documents?limit=100&query=' + encodeURIComponent(importedTitle), 'GET')).body.items).toEqual([]);
    for (const checkbox of await panel.getByRole('checkbox').all()) await checkbox.check(); await panel.getByRole('combobox', { name: /^Cách áp dụng(?:\s|$)/ }).selectOption('replace');
    await panel.locator('.document-preview').scrollIntoViewIfNeeded(); await capture(page, `${format}-import-preview`);
    await panel.getByRole('button', { name: 'Apply vào draft — chưa Save', exact: true }).click(); await expect(page.locator('#document-body')).toHaveValue(converted.body);
    expect((await response(page, '/api/v1/documents?limit=100&query=' + encodeURIComponent(importedTitle), 'GET')).body.items).toEqual([]);
    const commit = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/document-imports' && r.request().method() === 'POST'); await page.getByRole('button', { name: 'Tạo page', exact: true }).click(); const committed = await commit; expect(committed.status()).toBe(201); const imported = await committed.json(); expect(imported.committed).toBe(true); expect(imported.detailAvailable).toBe(false);
    const importRequest = committed.request(); const replay = await response(page, '/api/v1/document-imports', 'POST', importRequest.postDataJSON(), undefined, frozenKey(importRequest.headers())); expect(replay.status).toBe(201); expect(replay.body).toEqual(imported);
    const unconfirmed = await response(page, '/api/v1/document-imports', 'POST', { ...importRequest.postDataJSON(), confirmed: false }); expect(unconfirmed.status).toBe(409); expect(unconfirmed.body.code).toBe('PreviewChanged');
    const row = sql('Document', imported.id)[0]; expect(row.OwnerId.toLowerCase()).toBe(owner.me.personalSpaceId.toLowerCase()); expect(row.VersionNumber).toBe(1); expect(row.Status).toBe('Draft'); expect(row.DocumentType).toBe('Note'); expect(row.EditorMode).toBe('Markdown');
    const report = await response(page, `/api/v1/document-imports/by-page/${imported.id}`, 'GET'); expect(report.body.state).toBe('Committed'); expect(sql('DocumentImportJob', report.body.id)[0].PageId.toLowerCase()).toBe(imported.id.toLowerCase());
    const matches = (await response(page, '/api/v1/documents?limit=100&query=' + encodeURIComponent(importedTitle), 'GET')).body.items; expect(matches.filter((p: any) => p.title === importedTitle)).toHaveLength(1);
    await page.reload(); await card(page, importedTitle).getByRole('button', { name: 'Mở', exact: true }).click(); await expect(page.locator('#document-body')).toHaveValue(converted.body); expect((await api(page, 'getDocument', [imported.id])).body).toBe(converted.body);
    await page.locator('#document-body').scrollIntoViewIfNeeded(); await capture(page, `${format}-imported`);
  } catch (error) { scenarioError = error; throw error; }
  finally {
    const failures: unknown[] = []; for (const cleanup of [() => otherAccess?.close(), () => otherContext.close(), () => owner.close()]) { try { await cleanup(); } catch (error) { failures.push(error); } }
    if (failures.length) throw new AggregateError([...(scenarioError ? [scenarioError] : []), ...failures], 'Owned transfer test cleanup failed');
  }
});

test('Documents export committed HTTP408: frozen exact retry recovers the original real SQL artifact', async ({ page, browser }) => {
  test.setTimeout(90000); page.setDefaultTimeout(15000); await page.goto('/');
  const owner = await access(browser, page), commands: { key: string; etag: string; bodyDigest: string; id: string }[] = [];
  const title = unique('export-timeout'); let scenarioError: unknown;
  const pattern = '**/api/v1/documents/*/exports';
  try {
    await page.goto('/modules/FX20'); await page.locator('#document-title').fill(title);
    await page.locator('#document-type').selectOption('Document'); await page.locator('#document-editor').selectOption('Markdown'); await page.locator('#document-body').fill('Synthetic export timeout content.');
    const createdResponse = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/documents' && r.request().method() === 'POST');
    await page.getByRole('button', { name: 'Tạo page', exact: true }).click(); const source = await (await createdResponse).json(); const before = sql('Document', source.id);
    await page.route(pattern, async route => {
      const request = route.request();
      if (request.method() !== 'POST' || !new URL(request.url()).pathname.endsWith(`/documents/${source.id}/exports`)) { await route.continue(); return; }
      const actual = await route.fetch({ maxRedirects: 0 });
      if (actual.status() !== 201) { await route.fulfill({ response: actual }); return; }
      const ack = await actual.json();
      commands.push({ key: request.headers()['idempotency-key'], etag: request.headers()['if-match'], bodyDigest: createHash('sha256').update(request.postData()!).digest('hex'), id: ack.id });
      if (commands.length === 1) await route.fulfill({ status: 408, contentType: 'application/problem+json', body: JSON.stringify({ status: 408, code: 'RequestTimeout', title: 'Synthetic response timeout after actual SQL commit.' }) });
      else await route.fulfill({ response: actual });
    });
    await page.getByRole('button', { name: 'Export bản đã lưu v1', exact: true }).first().click();
    const modal = page.getByRole('dialog', { name: 'Export bản đã lưu v1', exact: true });
    await modal.getByRole('combobox', { name: /^Format(?:\s|$)/ }).selectOption('MD');
    await modal.getByRole('button', { name: 'Xem trước chuyển đổi', exact: true }).click();
    await expect(modal.getByRole('checkbox').last()).toBeVisible(); for (const box of await modal.getByRole('checkbox').all()) await box.check();
    const timedOut = page.waitForResponse(r => new URL(r.url()).pathname.endsWith(`/documents/${source.id}/exports`) && r.request().method() === 'POST' && r.status() === 408);
    await modal.getByRole('button', { name: 'Tạo file xuất', exact: true }).click(); await timedOut;
    await expect(modal.getByRole('button', { name: 'Đối chiếu cùng yêu cầu', exact: true })).toBeVisible();
    await expect(modal.getByRole('combobox', { name: /^Format(?:\s|$)/ })).toBeDisabled();
    expect(commands).toHaveLength(1); expect(sql('DocumentExportJob', commands[0].id)).toHaveLength(1); expect(sql('Document', source.id)).toEqual(before);
    await capture(page, 'committed-408-frozen');
    await modal.getByRole('button', { name: 'Đối chiếu cùng yêu cầu', exact: true }).click();
    await expect(modal.getByText('File xuất đã được tạo riêng cho bạn.', { exact: false })).toBeVisible();
    expect(commands).toHaveLength(2); expect(commands[0].key).toMatch(/^[0-9a-f-]{36}$/i); expect(commands[0].etag).toBe(source.etag); expect(commands[1]).toEqual(commands[0]);
    expect(sql('DocumentExportJob', commands[0].id)).toHaveLength(1); expect(sql('Document', source.id)).toEqual(before);
    await modal.getByRole('button', { name: 'Xem trạng thái hiện tại', exact: true }).click(); await expect(modal.getByText(/Sẵn sàng tải · hết hạn/)).toBeVisible();
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await page.reload();
    expect(sql('DocumentExportJob', commands[0].id)).toHaveLength(1); expect(sql('Document', source.id)).toEqual(before);
  } catch (error) { scenarioError = error; throw error; }
  finally {
    const failures: unknown[] = []; for (const cleanup of [() => page.unroute(pattern), () => owner.close()]) { try { await cleanup(); } catch (error) { failures.push(error); } }
    if (failures.length) throw new AggregateError([...(scenarioError ? [scenarioError] : []), ...failures], 'Export timeout cleanup failed');
  }
});
