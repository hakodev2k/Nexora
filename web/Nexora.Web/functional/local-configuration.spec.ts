import { test, expect, api, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';
import { automationRoot, client, configured, screenshot, workflow, workflowCard } from './local-configuration-helpers';

test.use({ actionTimeout: 15000 });

test('E34 configuration: native immutable versions, disabled schedule, run history and Trash exact-cohort restore', async ({ page, browser }) => {
  test.setTimeout(180000);
  await configured(browser, page, ['FX34'], async () => {
    const me = await api(page, 'getMe'), item = await workflow(page);
    const row = sql('AutomationDefinition', item.id)[0]; expect(row.OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase()); expect(row.CurrentVersion).toBe(1); expect(row.Enabled).toBe(false);
    const current = await client(page, 'automationApi', 'automationGet', [item.id]);
    const validation = await client(page, 'automationApi', 'automationValidate', [{ title: item.title, definitionJson: current.version.definitionJson }]);
    expect(validation.shapeAccepted).toBe(true); expect(validation.referencesValid).toBe(true); expect(validation.executionAvailable).toBe(false); expect(validation.verifiedSimulation).toBe(false);
    const denied = await response(page, automationRoot + '/' + item.id + '/enable', 'POST', undefined, current.definition.etag);
    expect(denied.status).toBe(403); expect(denied.body.code).toBe('ModuleUnavailable');
    expect((await api(page, 'listTasks', [item.project.id])).items).toEqual([]);
    await workflowCard(page, item.title).getByRole('button', { name: 'Mở cấu hình', exact: true }).click();
    const editor = page.getByRole('dialog', { name: 'Definition editor', exact: true }); await editor.getByLabel('Tên workflow', { exact: true }).fill(item.title + ' updated');
    await editor.getByRole('button', { name: 'Lưu version', exact: true }).click(); await expect(editor).toHaveCount(0);
    expect(sql('AutomationDefinition', item.id)[0].CurrentVersion).toBe(2);
    await workflowCard(page, item.title + ' updated').getByRole('button', { name: 'Lịch sử version', exact: true }).click();
    const history = page.getByRole('dialog', { name: 'Lịch sử definition', exact: true }); await expect(history.locator('summary')).toHaveCount(2);
    const versions = await client(page, 'automationApi', 'automationHistory', [item.id]); expect(versions.items.map((v: any) => [v.versionNumber, v.title])).toEqual([[2, item.title + ' updated'], [1, item.title]]);
    await screenshot(page, 'automation-immutable-history'); await history.getByRole('button', { name: 'Hủy', exact: true }).click();
    const card = workflowCard(page, item.title + ' updated'); await card.getByRole('button', { name: 'Schedule', exact: true }).click();
    const schedule = page.getByRole('dialog', { name: 'Schedule configuration', exact: true });
    await expect(schedule.getByLabel('Opt-in local scheduled runs', { exact: true })).toBeDisabled();
    await schedule.getByLabel('IANA timezone', { exact: true }).fill('UTC'); await schedule.getByLabel('Start ISO timestamp', { exact: true }).fill('2028-01-01T00:00:00Z');
    await schedule.getByRole('button', { name: 'Preview 5 next runs', exact: true }).click(); await expect(schedule.getByRole('list', { name: 'Next scheduled occurrences', exact: true }).locator('li')).toHaveCount(5);
    const saved = page.waitForResponse(r => r.url().endsWith('/' + item.id + '/schedule') && r.request().method() === 'PUT'); await schedule.getByRole('button', { name: 'Save schedule', exact: true }).click(); expect((await saved).status()).toBe(200);
    await expect.poll(() => sql('AutomationSchedule', item.id).length).toBe(1); const persisted = sql('AutomationSchedule', item.id)[0]; expect(persisted.DefinitionVersion).toBe(2);
    const config = JSON.parse(persisted.ConfigurationJson); expect(config.Enabled ?? config.enabled).toBe(false); await screenshot(page, 'automation-disabled-schedule');
    await schedule.getByRole('button', { name: 'Hủy', exact: true }).click();
    await card.getByRole('button', { name: 'Run history', exact: true }).click(); const runs = page.getByRole('dialog', { name: 'Run history', exact: true }); await expect(runs.getByText('Chưa có run khớp bộ lọc.', { exact: true })).toBeVisible(); await runs.getByRole('button', { name: 'Hủy', exact: true }).click();
    await card.getByRole('button', { name: 'Trash', exact: true }).click(); const trash = page.getByRole('dialog', { name: 'Chuyển workflow vào Trash', exact: true }); await trash.getByRole('button', { name: 'Chuyển vào Trash', exact: true }).click(); await expect.poll(() => sql('AutomationDefinition', item.id)[0].DeletionBatchId).toBeTruthy(); await trash.getByRole('button', { name: 'Hủy', exact: true }).click();
    const batch = sql('AutomationDefinition', item.id)[0].DeletionBatchId; await page.getByLabel('Lifecycle', { exact: true }).selectOption('Trash'); await card.getByRole('button', { name: 'Khôi phục', exact: true }).click(); const restore = page.getByRole('dialog', { name: 'Khôi phục workflow', exact: true }); await restore.getByRole('button', { name: 'Khôi phục', exact: true }).click();
    await expect.poll(() => sql('AutomationDefinition', item.id)[0].DeletionBatchId ?? null).toBe(null); await restore.getByRole('button', { name: 'Hủy', exact: true }).click(); expect(batch).toBeTruthy(); expect(sql('AutomationDefinition', item.id)[0].Enabled).toBe(false);
  });
});

test('E35 inactive connections: native create/update/disable persists safe configuration and never contacts provider', async ({ page, browser }) => {
  test.setTimeout(180000);
  await configured(browser, page, ['FX35'], async () => {
    const requests: string[] = []; page.on('request', request => { const url = new URL(request.url()); if (url.hostname === 'service.example.com' || url.hostname === 'updated.example.com') requests.push(url.origin); });
    await page.goto('/modules/FX35'); await expect(page.getByRole('heading', { name: 'Connections', exact: true })).toBeVisible();
    const registry = await client(page, 'integrationApi', 'integrationConnectors'); expect(registry).toHaveLength(1); expect(registry[0].realOperationsAvailable).toBe(false); expect(registry[0].inboundCommands).toEqual([]); expect(registry[0].outboundEvents).toEqual([]);
    const title = unique('Inactive connection'); await page.getByRole('button', { name: 'New connection', exact: true }).click(); const dialog = page.getByRole('dialog', { name: 'New connection', exact: true });
    await dialog.getByLabel('Tên', { exact: true }).fill(title); await dialog.getByLabel('HTTPS service origin', { exact: true }).fill('https://service.example.com/path');
    const invalid = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/integrations/connections/' && r.request().method() === 'POST'); await dialog.getByRole('button', { name: 'Save Disabled', exact: true }).click(); expect((await invalid).status()).toBe(422); await expect(dialog).toBeVisible();
    await dialog.getByLabel('HTTPS service origin', { exact: true }).fill('https://service.example.com/'); const created = page.waitForResponse(r => new URL(r.url()).pathname === '/api/v1/integrations/connections/' && r.request().method() === 'POST'); await dialog.getByRole('button', { name: 'Save Disabled', exact: true }).click(); const creation = await created; expect(creation.status()).toBe(201); const ack = await creation.json(); await expect(dialog).toHaveCount(0);
    const me = await api(page, 'getMe'), first = sql('IntegrationConnection', ack.id)[0]; expect(first.OwnerId.toLowerCase()).toBe(me.personalSpaceId.toLowerCase()); expect(first.State).toBe('Disabled'); expect(first.AuthMode).toBe('Unconfigured'); expect(first.VaultResourceId ?? null).toBe(null); expect(first.LastTestAt ?? null).toBe(null);
    let card = page.locator('article').filter({ has: page.getByRole('heading', { name: title, exact: true }) }); await card.getByRole('button', { name: 'Open', exact: true }).click(); const editor = page.getByRole('dialog', { name: 'Connection form', exact: true }); await editor.getByLabel('Tên', { exact: true }).fill(title + ' updated'); await editor.getByLabel('HTTPS service origin', { exact: true }).fill('https://updated.example.com/'); await editor.getByRole('button', { name: 'Save Disabled', exact: true }).click(); await expect(editor).toHaveCount(0);
    expect(sql('IntegrationConnection', ack.id)[0].Title).toBe(title + ' updated'); await page.reload(); card = page.locator('article').filter({ has: page.getByRole('heading', { name: title + ' updated', exact: true }) }); await expect(card).toBeVisible(); await screenshot(page, 'inactive-connection-populated');
    await card.getByRole('button', { name: 'Disable / invalidate authority', exact: true }).click(); const disable = page.getByRole('dialog', { name: 'Disable connection', exact: true }); await expect(disable.getByRole('button', { name: 'Xác nhận disable', exact: true })).toBeDisabled(); await disable.getByLabel('Xác nhận disable', { exact: true }).check(); await disable.getByRole('button', { name: 'Xác nhận disable', exact: true }).click(); await expect(disable).toHaveCount(0);
    const last = sql('IntegrationConnection', ack.id)[0]; expect(last.State).toBe('Disabled'); expect(last.AuthorityRevision).toBeGreaterThan(first.AuthorityRevision); expect(JSON.parse(last.ScopesJson)).toEqual({ schemaVersion: 1, inbound: [], outbound: [] }); expect(last.LastTestAt ?? null).toBe(null); expect(requests).toEqual([]);
  });
});
