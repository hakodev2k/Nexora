import { test, expect, api, login, unique } from './fixtures';

test('Files Trash: real upload, dependency preview and lost ACK retry preserve one durable effect', async ({ page: fixturePage, browser }) => {
  test.setTimeout(180000);
  const adminContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await adminContext.newPage(); await login(admin, 'SuperAdmin');
  const ownerContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: fixturePage.viewportSize() ?? undefined });
  const page = await ownerContext.newPage(); await login(page, 'SuperAdmin'); await page.goto('/');
  const me = await api(page, 'getMe');
  const module = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX07');
  const originalPolicy = { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled };
  const originalGrant = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX07');
  const policy = async (change: typeof originalPolicy) => {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.code === 'FX07');
    if (current.systemEnabled === change.systemEnabled && current.registrationEnabled === change.registrationEnabled) return;
    const preview = await api(admin, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  const grant = async (enabled: boolean) => {
    const current = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.find((m: any) => m.code === 'FX07');
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: originalGrant.moduleId, enabled }];
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  let failed = false;
  try {
    await policy({ systemEnabled: true, registrationEnabled: true }); await grant(true);
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX07', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    const name = unique('file-trash') + '.txt';
    await expect(page.getByLabel('Chọn file', { exact: true })).toBeEnabled();
    await page.getByLabel('Chọn file', { exact: true }).setInputFiles({ name, mimeType: 'text/plain', buffer: Buffer.from('Isolated synthetic Files Trash browser acceptance.') });
    await page.getByRole('button', { name: 'Upload và scan', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Tải lại', exact: true })).toBeEnabled();
    await page.getByLabel('Tìm tên file').fill(name);
    await page.getByRole('button', { name: 'Lọc files', exact: true }).click();
    const card = page.locator('.resource-card').filter({ has: page.getByRole('heading', { name, exact: true }) });
    await expect(card).toBeVisible();
    await card.getByRole('button', { name: 'Move to Trash', exact: true }).click();
    const modal = page.getByRole('dialog');
    await expect(modal.getByRole('button', { name: 'Move to Trash', exact: true })).toBeEnabled();
    await expect(modal.getByLabel('Affected file')).toContainText('Affected: 1 file');
    await modal.getByRole('button', { name: 'Hủy', exact: true }).click(); await expect(modal).toHaveCount(0);
    await expect(card).toBeVisible();
    await card.getByRole('button', { name: 'Move to Trash', exact: true }).click();
    await expect(modal.getByRole('button', { name: 'Move to Trash', exact: true })).toBeEnabled();
    const requests: { key: string; etag: string; id: string }[] = [];
    await page.route('**/api/v1/files/*/trash', async route => {
      const request = route.request(), headers = request.headers();
      requests.push({ key: headers['idempotency-key'], etag: headers['if-match'], id: new URL(request.url()).pathname.split('/').at(-2)! });
      if (requests.length === 1) { const committed = await route.fetch(); expect(committed.status()).toBe(204); await route.abort('failed'); }
      else await route.continue();
    });
    await modal.getByRole('button', { name: 'Move to Trash', exact: true }).click();
    await expect(modal.getByRole('alert')).toContainText('Chưa rõ kết quả');
    await expect(modal.getByRole('button', { name: 'Tải lại preview', exact: true })).toBeDisabled();
    await modal.getByRole('button', { name: 'Retry cùng request', exact: true }).click();
    await expect(modal).toHaveCount(0);
    expect(requests).toHaveLength(2); expect(requests[1]).toEqual(requests[0]);
    await page.unroute('**/api/v1/files/*/trash');
    await expect(card).toHaveCount(0);
    await page.getByLabel('File lifecycle').selectOption('Trash');
    await page.getByRole('button', { name: 'Lọc files', exact: true }).click(); await expect(card).toBeVisible();
    await expect(card.getByRole('link', { name: 'Tải xuống', exact: true })).toHaveCount(0);
    await expect(card.getByRole('button', { name: 'Move to Trash', exact: true })).toHaveCount(0);
    const record = await api(page, 'apiFetch', [`/api/v1/files/${requests[0].id}`]);
    expect(record.lifecycle).toBe('Trash');
    expect(await page.evaluate(async path => (await fetch(path, { credentials: 'same-origin' })).status,
      `/api/v1/files/${requests[0].id}/content`)).toBe(404);
    await card.getByRole('button', { name: 'Restore', exact: true }).click();
    await expect(modal.getByRole('button', { name: 'Restore', exact: true })).toBeEnabled();
    await expect(modal.getByLabel('Affected file')).toContainText('Affected: 1 file');
    const restores: { key: string; etag: string; body: string | null }[] = [];
    await page.route('**/api/v1/files/*/restore', async route => {
      const request = route.request(), headers = request.headers();
      restores.push({ key: headers['idempotency-key'], etag: headers['if-match'], body: request.postData() });
      if (restores.length === 1) { const committed = await route.fetch(); expect(committed.status()).toBe(200); await route.abort('failed'); }
      else await route.continue();
    });
    await modal.getByRole('button', { name: 'Restore', exact: true }).click();
    await expect(modal.getByRole('alert')).toContainText('Chưa rõ kết quả');
    await modal.getByRole('button', { name: 'Retry cùng request', exact: true }).click();
    await expect(modal).toHaveCount(0);
    expect(restores).toHaveLength(2); expect(restores[1]).toEqual(restores[0]);
    await page.unroute('**/api/v1/files/*/restore');
    await expect(card).toHaveCount(0);
    await page.getByLabel('File lifecycle').selectOption('Active');
    await page.getByRole('button', { name: 'Lọc files', exact: true }).click(); await expect(card).toBeVisible();
    expect((await api(page, 'apiFetch', [`/api/v1/files/${requests[0].id}`])).lifecycle).toBe('Active');
    expect(await page.evaluate(async path => (await fetch(path, { credentials: 'same-origin' })).text(),
      `/api/v1/files/${requests[0].id}/content`)).toBe('Isolated synthetic Files Trash browser acceptance.');
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  } catch (e) { failed = true; throw e; }
  finally { try { await grant(originalGrant.enabled); await policy(originalPolicy); } catch (e) { if (!failed) throw e; } finally { await ownerContext.close(); await adminContext.close(); } }
});
