import { test, expect, api, confirm, login, sql, unique } from './fixtures';

test('Sharing repair: approved public Task detail, separate policy impact, cancellation and permanent nonrevival', async ({ page, browser }, worker) => {
  test.setTimeout(240000); page.setDefaultTimeout(15000);
  const adminContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: worker.project.use.viewport });
  const publicContext = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL, viewport: worker.project.use.viewport });
  const admin = await adminContext.newPage(); const viewer = await publicContext.newPage(); await login(admin, 'SuperAdmin');
  await page.goto('/'); const me = await api(page, 'getMe');
  const codes = ['FX04', 'FX11', 'FX12', 'FX20'];
  const originals = (await api(admin, 'listAdminModules')).items.filter((m: any) => codes.includes(m.code));
  const grants = (await api(admin, 'getAdminUserAccess', [me.id])).moduleGrants.filter((g: any) => codes.includes(g.code));
  const policy = async (module: any, desired: Record<string, boolean>) => {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.id === module.id);
    const change = Object.fromEntries(Object.entries(desired).filter(([key, value]) => current[key] !== value));
    if (!Object.keys(change).length) return;
    const preview = await api(admin, 'previewModulePolicy', [module.id, change]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  };
  const grant = async (desired: any[]) => {
    const current = await api(admin, 'getAdminUserAccess', [me.id]);
    const changes = desired.filter(g => current.moduleGrants.find((c: any) => c.moduleId === g.moduleId)?.enabled !== g.enabled);
    if (!changes.length) return;
    const preview = await api(admin, 'previewAdminAccess', [me.id, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [me.id, preview.etag, changes, preview.previewToken]);
  };
  try {
    expect(originals).toHaveLength(4);
    for (const module of originals) {
      expect(module.state).toBe('Ready');
      await policy(module, { systemEnabled: true, registrationEnabled: true, ...(module.code === 'FX12' ? {} : { sharingEnabled: true }) });
    }
    await grant(grants.map((g: any) => ({ moduleId: g.moduleId, enabled: true })));
    const prefix = unique('Shared-projection'); const start = '2001-01-01T00:00:00Z', end = '2001-01-02T00:00:00Z';
    const project = await api(page, 'createProject', [prefix, 'Approved synthetic public description', '2000-01-01T00:00:00Z', '2030-01-05T00:00:00Z']);
    const task = await api(page, 'createTask', [project.id, prefix + ' task', '<script>literal text</script>', 'NotStarted', '2030-01-01T00:00:00Z', start, end, null, '["Approved public tag"]', '["Literal <img src=x onerror=alert(1)>",{"text":"Ordered checked criterion","checked":true}]']);
    const link = await api(page, 'createShareLink', ['Project', project.id, 'PublicLink', null, [], true]);
    const saved = sql('ShareLink', link.id)[0]; expect(saved.OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase());
    expect(saved).not.toHaveProperty('TokenHash'); expect(saved.IsDeleted).toBe(false);
    await viewer.goto('/share/' + link.token);
    await expect(viewer.getByRole('heading', { name: prefix, exact: true })).toBeVisible();
    await expect(viewer.getByRole('columnheader', { name: 'Start / End', exact: true })).toBeVisible();
    await expect(viewer.getByRole('columnheader', { name: 'Acceptance criteria', exact: true })).toBeVisible();
    await expect(viewer.getByText('Approved public tag', { exact: true })).toBeVisible();
    await expect(viewer.getByText(/☑ Ordered checked criterion/)).toBeVisible();
    await expect(viewer.getByText('<script>literal text</script>', { exact: true })).toBeVisible();
    await expect(viewer.locator('.shared-resource-card script, .shared-resource-card img')).toHaveCount(0);
    await expect(viewer.getByRole('columnheader', { name: 'Due', exact: true })).toHaveCount(0);
    const resolved = await api(viewer, 'resolveShareLink', [link.token]); const shared = resolved.project.tasks.find((t: any) => t.id === task.id);
    expect(shared.priority).toBeNull(); expect(shared.isOverdue).toBe(true); expect(shared).not.toHaveProperty('dueAt'); expect(shared).not.toHaveProperty('reminderAt');
    await expect.poll(() => viewer.locator('.shared-resource-card').evaluate(el => el.scrollWidth <= el.clientWidth)).toBe(true);
    const global = originals.find((m: any) => m.code === 'FX04');
    await policy(global, { registrationEnabled: false }); await viewer.reload();
    await expect(viewer.getByRole('heading', { name: prefix, exact: true })).toBeVisible(); expect(sql('ShareLink', link.id)[0].IsDeleted).toBe(false);
    await admin.goto('/admin/modules'); const row = admin.locator('.module-card').filter({ has: admin.getByText('FX04', { exact: true }) });
    await row.getByLabel('Sharing enabled', { exact: true }).uncheck();
    const reviewed = admin.waitForResponse(r => r.url().includes(`/admin/modules/${global.id}/preview`) && r.request().method() === 'POST');
    await row.getByRole('button', { name: 'Xem lại thay đổi FX04', exact: true }).click(); const impact = await (await reviewed).json(); expect(impact.affectedSharingLinks).toBeGreaterThan(0); expect(impact.affectedUsers).toBeGreaterThan(0);
    await expect(admin.getByRole('dialog').getByText(`${impact.affectedSharingLinks} link chia sẻ hiện có sẽ mất hiệu lực vĩnh viễn. Bật lại không phục hồi những link này.`, { exact: true })).toBeVisible();
    await admin.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click(); expect(sql('ShareLink', link.id)[0].IsDeleted).toBe(false);
    await row.getByRole('button', { name: 'Xem lại thay đổi FX04', exact: true }).click(); await confirm(admin, 'Xác nhận policy');
    await expect.poll(() => sql('ShareLink', link.id)[0].IsDeleted).toBe(true); expect(sql('ShareLink', link.id)[0].InvalidationReason).toBe('SharingDisabled');
    await viewer.reload(); await expect(viewer.getByRole('heading', { name: 'Không thể mở nội dung', exact: true })).toBeVisible(); await expect(viewer.getByRole('heading', { name: prefix, exact: true })).toHaveCount(0);
    await row.getByLabel('Sharing enabled', { exact: true }).check(); await row.getByRole('button', { name: 'Xem lại thay đổi FX04', exact: true }).click(); await confirm(admin, 'Xác nhận policy');
    await viewer.reload(); await expect(viewer.getByRole('heading', { name: 'Không thể mở nội dung', exact: true })).toBeVisible(); expect(sql('ShareLink', link.id)[0].IsDeleted).toBe(true);
  } finally {
    await grant(grants.map((g: any) => ({ moduleId: g.moduleId, enabled: g.enabled })));
    for (const module of originals) await policy(module, { systemEnabled: module.systemEnabled, registrationEnabled: module.registrationEnabled, ...(module.code === 'FX12' ? {} : { sharingEnabled: module.sharingEnabled }) });
    await publicContext.close(); await adminContext.close();
  }
});
