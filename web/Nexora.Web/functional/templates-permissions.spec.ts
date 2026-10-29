import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';

// Normal Admin/SuperAdmin sessions and genuine preview/commit changes only.
// This case mutates shared synthetic authority: execute under the coordinator's
// exclusive functional-writer window and restore every touched disposition.
test('Templates: real Admin Bookmark create-only handoff never reads the denied library', async ({ browser }) => {
  test.setTimeout(240_000);
  const adminContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const operatorContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await adminContext.newPage(), operator = await operatorContext.newPage();
  const codes = ['FX21', 'FX24'];
  const effects = new Map([
    ['organization.template.read', 'Allow'], ['organization.template.create', 'Allow'],
    ['organization.template.instantiate', 'Allow'], ['bookmarks.bookmark.create', 'Allow'],
    ['bookmarks.bookmark.read', 'Deny'],
  ]);
  let account: any, before: any, scenarioError: unknown;
  const policies: any[] = [];
  const originalEffects = new Map<string, string>();
  const currentAccess = () => api(operator, 'getAdminUserAccess', [account.id]);
  async function setPermissions(desired: Map<string, string>) {
    const access = await currentAccess();
    const changes = [...desired].filter(([key, effect]) =>
      (access.actionGrants.find((g: any) => g.actionKey === key)?.effect ?? 'Unset') !== effect)
      .map(([actionKey, effect]) => ({ actionKey, effect }));
    if (!changes.length) return;
    const preview = await api(operator, 'previewAdminAccess', [account.id, { kind: 'permissions', changes }]);
    expect(preview.blockers).toEqual([]);
    await api(operator, 'commitAdminActionGrant', [account.id, preview.etag, changes, preview.previewToken]);
  }
  async function setModules(desired: Array<{ moduleId: string; enabled: boolean }>) {
    const access = await currentAccess();
    const changes = desired.filter(row => access.moduleGrants.find((g: any) => g.moduleId === row.moduleId)?.enabled !== row.enabled);
    if (!changes.length) return;
    const preview = await api(operator, 'previewAdminAccess', [account.id, { kind: 'modules', changes }]);
    expect(preview.blockers).toEqual([]);
    await api(operator, 'commitAdminModuleGrant', [account.id, preview.etag, changes, preview.previewToken]);
  }
  async function setPolicy(saved: any, enabled: boolean) {
    const current = (await api(operator, 'listAdminModules')).items.find((m: any) => m.id === saved.id);
    if (current.systemEnabled === enabled && current.registrationEnabled === saved.registrationEnabled) return;
    const change = { systemEnabled: enabled, registrationEnabled: saved.registrationEnabled };
    const preview = await api(operator, 'previewModulePolicy', [saved.id, change]); expect(preview.blockers).toEqual([]);
    await api(operator, 'commitModulePolicy', [saved.id, preview.etag, change, preview.previewToken]);
  }
  try {
    await login(operator, 'SuperAdmin'); await login(admin, 'Admin');
    account = await api(admin, 'getMe'); expect(account.role).toBe('Admin');
    before = await currentAccess();
    expect(before.moduleGrants.filter((g: any) => codes.includes(g.code))).toHaveLength(2);
    const catalog = await api(operator, 'listAdminModules');
    for (const code of codes) {
      const module = catalog.items.find((m: any) => m.code === code);
      expect(module, 'The already-installed contracted module must exist').toBeTruthy();
      expect(module.state, 'The test cannot promote a gated module').toBe('Ready');
      policies.push({ ...module });
    }
    for (const action of effects.keys()) originalEffects.set(action, before.actionGrants.find((g: any) => g.actionKey === action)?.effect ?? 'Unset');
    for (const module of policies) await setPolicy(module, true);
    await setModules(before.moduleGrants.filter((g: any) => codes.includes(g.code)).map((g: any) => ({ moduleId: g.moduleId, enabled: true })));
    await setPermissions(effects);
    const caps = (await response(admin, '/api/v1/bookmarks/capabilities', 'GET')).body;
    expect(caps['bookmarks.bookmark.create']).toBe(true); expect(caps['bookmarks.bookmark.read']).toBe(false);
    // Existing Bookmark handlers intentionally map unavailable action authority
    // to the opaque ModuleUnavailable409 contract, including explicit read Deny.
    const deniedList = await response(admin, '/api/v1/bookmarks', 'GET');
    expect(deniedList.status).toBe(409); expect(deniedList.body.code).toBe('ModuleUnavailable');

    const libraryReads: string[] = [];
    admin.on('request', request => {
      if (request.method() === 'GET' && new URL(request.url()).pathname === '/api/v1/bookmarks') libraryReads.push('denied-library-GET');
    });
    const name = unique('template-create-only'), title = unique('bookmark-template-seed');
    const url = 'https://example.invalid/template-create-only', description = 'Synthetic approved Bookmark seed';
    await admin.goto('/organize/templates');
    await admin.getByRole('button', { name: 'Tạo mẫu mới', exact: true }).click();
    let modal = admin.getByRole('dialog');
    await modal.getByLabel('Tên mẫu', { exact: true }).fill(name);
    await modal.getByLabel('Tiêu đề Bookmark', { exact: true }).fill(title);
    await modal.getByLabel('Đường dẫn HTTP(S)', { exact: true }).fill(url);
    await modal.getByLabel('Mô tả', { exact: true }).fill(description);
    const templateWrite = admin.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/organization/templates' && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Lưu mẫu', exact: true }).click();
    const committed = await templateWrite; expect(committed.status()).toBe(201);
    const templateAck = await committed.json(); expect(templateAck.committed).toBe(true);
    await expect(modal).toHaveCount(0); await admin.reload();
    const templateCard = admin.locator('.template-card').filter({ has: admin.getByRole('heading', { name, exact: true }) });
    await expect(templateCard).toBeVisible();
    const detail = await response(admin, '/api/v1/organization/templates/' + templateAck.id, 'GET');
    expect(detail.status).toBe(200); expect(detail.body.seed).toEqual({ title, url, description });
    await templateCard.getByRole('button', { name: 'Xem trước', exact: true }).click();
    const prepared = admin.waitForResponse(r => new URL(r.url()).pathname === `/api/v1/organization/templates/${templateAck.id}/draft`);
    await admin.getByRole('button', { name: 'Dùng mẫu này', exact: true }).click(); expect((await prepared).status()).toBe(200);
    await expect(admin).toHaveURL('/bookmarks'); modal = admin.getByRole('dialog');
    await expect(modal.getByLabel('URL', { exact: true })).toHaveValue(url);
    await expect(modal.getByLabel('Tiêu đề', { exact: true })).toHaveValue(title);
    await expect(modal.getByLabel('Mô tả', { exact: false })).toHaveValue(description);
    expect(libraryReads).toEqual([]);
    const bookmarkWrite = admin.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/bookmarks' && r.request().method() === 'POST');
    await modal.getByRole('button', { name: 'Tạo bookmark', exact: true }).click();
    const saved = await bookmarkWrite; expect(saved.status()).toBe(201);
    const ack = await saved.json(); expect(ack.committed).toBe(true); expect(ack.detailAvailable).toBe(false);
    await expect(modal).toHaveCount(0);
    const rows = sql('Bookmark', ack.id); expect(rows).toHaveLength(1);
    expect(rows[0].OwnerId.toLowerCase()).toBe(account.personalSpaceId.toLowerCase());
    expect(rows[0].Title).toBe(title); expect(rows[0].Url).toBe(url); expect(rows[0].Description).toBe(description);
    await admin.reload(); await expect(admin.getByRole('heading', { name: 'Thư viện chưa khả dụng', exact: true })).toBeVisible();
    await expect(admin.getByRole('button', { name: 'Thêm bookmark', exact: true })).toBeEnabled();
    expect(libraryReads).toEqual([]);
    const deniedDetail = await response(admin, '/api/v1/bookmarks/' + ack.id, 'GET');
    expect(deniedDetail.status).toBe(409); expect(deniedDetail.body.code).toBe('ModuleUnavailable');
  } catch (error) { scenarioError = error; throw error; }
  finally {
    const cleanupErrors: unknown[] = [];
    const attempt = async (action: () => Promise<void>) => { try { await action(); } catch (error) { cleanupErrors.push(error); } };
    if (account && before) {
      await attempt(() => setPermissions(originalEffects));
      await attempt(() => setModules(before.moduleGrants.filter((g: any) => codes.includes(g.code)).map((g: any) => ({ moduleId: g.moduleId, enabled: g.enabled }))));
      for (const module of [...policies].reverse()) await attempt(() => setPolicy(module, module.systemEnabled));
      await attempt(async () => {
        const restored = await currentAccess(); expect(restored.user.role).toBe(before.user.role);
        for (const [key, effect] of originalEffects) expect(restored.actionGrants.find((g: any) => g.actionKey === key)?.effect ?? 'Unset').toBe(effect);
        for (const row of before.moduleGrants.filter((g: any) => codes.includes(g.code))) expect(restored.moduleGrants.find((g: any) => g.moduleId === row.moduleId)?.enabled).toBe(row.enabled);
        const catalog = await api(operator, 'listAdminModules');
        for (const row of policies) {
          const restoredModule = catalog.items.find((m: any) => m.id === row.id);
          expect(restoredModule.state).toBe(row.state); expect(restoredModule.systemEnabled).toBe(row.systemEnabled); expect(restoredModule.registrationEnabled).toBe(row.registrationEnabled);
        }
      });
    }
    await adminContext.close(); await operatorContext.close();
    if (cleanupErrors.length) throw new AggregateError([...(scenarioError ? [scenarioError] : []), ...cleanupErrors], 'Synthetic authority restoration failed; inspect exact failed preview/commit before another shared run.');
  }
});
