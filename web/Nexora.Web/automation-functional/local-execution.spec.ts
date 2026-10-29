import { test, expect, api, sql } from '../functional/fixtures';
import { client, configured, screenshot, workflow, workflowCard } from '../functional/local-configuration-helpers';

test.use({ actionTimeout: 15000 });

// Run only in a coordinator-owned runtime explicitly opted into the already
// contracted local Task worker. This test changes no operator/provider flags.
test('E34 local execution opt-in: native manual run creates exactly one real SQL Task and immutable run history', async ({ page, browser }) => {
  test.setTimeout(180000);
  await configured(browser, page, ['FX34'], async () => {
    const item = await workflow(page), card = workflowCard(page, item.title);
    let scenarioError: unknown;
    try {
    const detail = await client(page, 'automationApi', 'automationGet', [item.id]); const validation = await client(page, 'automationApi', 'automationValidate', [{ title: item.title, definitionJson: detail.version.definitionJson }]); expect(validation.executionAvailable).toBe(true);
    await card.getByRole('button', { name: 'Enable', exact: true }).click(); let dialog = page.getByRole('dialog', { name: 'Enable workflow', exact: true }); await dialog.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect.poll(() => sql('AutomationDefinition', item.id)[0].Enabled).toBe(true); await dialog.getByRole('button', { name: 'Hủy', exact: true }).click();
    await card.getByRole('button', { name: 'Run', exact: true }).click(); dialog = page.getByRole('dialog', { name: 'Chạy workflow', exact: true }); const started = page.waitForResponse(r => r.url().endsWith('/' + item.id + '/runs') && r.request().method() === 'POST'); await dialog.getByRole('button', { name: 'Xác nhận', exact: true }).click(); const result = await started; expect(result.status()).toBe(202); const run = await result.json();
    await expect.poll(async () => (await client(page, 'automationApi', 'automationRunGet', [run.id])).run.state, { timeout: 45000 }).toBe('Succeeded');
    const completed = await client(page, 'automationApi', 'automationRunGet', [run.id]); expect(completed.run.versionNumber).toBe(1); expect(completed.steps).toHaveLength(1); expect(completed.steps[0].state).toBe('Succeeded');
    const tasks = await api(page, 'listTasks', [item.project.id]); expect(tasks.items).toHaveLength(1); expect(tasks.items[0].id).toBe(completed.steps[0].taskId); const task = sql('Task', completed.steps[0].taskId)[0]; expect(task.Title).toBe(item.taskTitle); expect(task.ProjectId.toLowerCase()).toBe(item.project.id.toLowerCase());
    await dialog.getByRole('button', { name: 'Hủy', exact: true }).click(); await card.getByRole('button', { name: 'Run history', exact: true }).click(); const history = page.getByRole('dialog', { name: 'Run history', exact: true }); await expect(history.getByText(new RegExp(run.id))).toBeVisible(); await screenshot(page, 'local-task-run-succeeded'); await history.getByRole('button', { name: 'Hủy', exact: true }).click();
    await card.getByRole('button', { name: 'Disable', exact: true }).click(); const disable = page.getByRole('dialog', { name: 'Disable workflow', exact: true }); await disable.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect.poll(() => sql('AutomationDefinition', item.id)[0].Enabled).toBe(false); await disable.getByRole('button', { name: 'Hủy', exact: true }).click();
    expect((await api(page, 'listTasks', [item.project.id])).items).toHaveLength(1);
    } catch (error) { scenarioError = error; throw error; }
    finally {
      try {
        const current = (await client(page, 'automationApi', 'automationGet', [item.id])).definition;
        if (current.enabled) await client(page, 'automationApi', 'automationEnable', [item.id, current.etag, false, crypto.randomUUID()]);
        expect(sql('AutomationDefinition', item.id)[0].Enabled).toBe(false);
      } catch (error) { throw new AggregateError([...(scenarioError ? [scenarioError] : []), error], 'Owned synthetic workflow disable failed'); }
    }
  });
});

test('E34 local scheduler opt-in: one finite UTC occurrence creates one pinned run and one real Task', async ({ page, browser }) => {
  test.setTimeout(180000);
  await configured(browser, page, ['FX34'], async () => {
    const item = await workflow(page), card = workflowCard(page, item.title); let scenarioError: unknown;
    try {
      await card.getByRole('button', { name: 'Schedule', exact: true }).click(); const dialog = page.getByRole('dialog', { name: 'Schedule configuration', exact: true });
      const optIn = dialog.getByLabel('Opt-in local scheduled runs', { exact: true }); await expect(optIn).toBeEnabled();
      await dialog.getByLabel('Frequency', { exact: true }).selectOption('Interval'); await dialog.getByLabel('Interval minutes (1–1440)', { exact: true }).fill('1');
      await dialog.getByLabel('IANA timezone', { exact: true }).fill('UTC');
      // Exactly one future occurrence in [Start,End), no catch-up or lingering schedule.
      const start = new Date(Date.now() + 35000), end = new Date(start.getTime() + 30000);
      await dialog.getByLabel('Start ISO timestamp', { exact: true }).fill(start.toISOString()); await dialog.getByLabel('End ISO timestamp (optional)', { exact: true }).fill(end.toISOString()); await optIn.check();
      await dialog.getByRole('button', { name: 'Preview 5 next runs', exact: true }).click(); await expect(dialog.getByRole('list', { name: 'Next scheduled occurrences', exact: true }).locator('li')).toHaveCount(1);
      const saved = page.waitForResponse(r => r.url().endsWith('/' + item.id + '/schedule') && r.request().method() === 'PUT'); await dialog.getByRole('button', { name: 'Save schedule', exact: true }).click(); expect((await saved).status()).toBe(200); await dialog.getByRole('button', { name: 'Hủy', exact: true }).click();
      await card.getByRole('button', { name: 'Enable', exact: true }).click(); const enable = page.getByRole('dialog', { name: 'Enable workflow', exact: true }); await enable.getByRole('button', { name: 'Xác nhận', exact: true }).click(); await expect.poll(() => sql('AutomationDefinition', item.id)[0].Enabled).toBe(true); await enable.getByRole('button', { name: 'Hủy', exact: true }).click();
      await expect.poll(async () => (await client(page, 'automationApi', 'automationScheduleGet', [item.id])).recentOccurrences.length, { timeout: 60000 }).toBe(1);
      const snapshot = await client(page, 'automationApi', 'automationScheduleGet', [item.id]); const occurrence = snapshot.recentOccurrences[0];
      expect(occurrence.state).toBe('Queued'); expect(occurrence.code).toBe('OccurrenceQueued'); expect(new Date(occurrence.dueAt).getTime()).toBe(start.getTime()); expect(occurrence.runId).toBeTruthy();
      await expect.poll(async () => (await client(page, 'automationApi', 'automationRunGet', [occurrence.runId])).run.state, { timeout: 45000 }).toBe('Succeeded');
      const completed = await client(page, 'automationApi', 'automationRunGet', [occurrence.runId]); expect(completed.run.versionNumber).toBe(1); expect(completed.steps).toHaveLength(1);
      const tasks = await api(page, 'listTasks', [item.project.id]); expect(tasks.items).toHaveLength(1); expect(tasks.items[0].id).toBe(completed.steps[0].taskId); expect(sql('Task', completed.steps[0].taskId)[0].Title).toBe(item.taskTitle);
      const scheduleSql = sql('AutomationSchedule', item.id); expect(scheduleSql).toHaveLength(1); expect(scheduleSql[0].DefinitionVersion).toBe(1);
      await card.getByRole('button', { name: 'Run history', exact: true }).click(); const history = page.getByRole('dialog', { name: 'Run history', exact: true }); await expect(history.getByText(new RegExp(occurrence.runId))).toBeVisible(); await screenshot(page, 'local-scheduled-task-succeeded'); await history.getByRole('button', { name: 'Hủy', exact: true }).click();
    } catch (error) { scenarioError = error; throw error; }
    finally {
      try {
        const current = (await client(page, 'automationApi', 'automationGet', [item.id])).definition;
        if (current.enabled) await client(page, 'automationApi', 'automationEnable', [item.id, current.etag, false, crypto.randomUUID()]);
        const schedule = await client(page, 'automationApi', 'automationScheduleGet', [item.id]);
        if (schedule.configuration?.enabled) await client(page, 'automationApi', 'automationScheduleSave', [item.id, schedule.etag, { ...schedule.configuration, enabled: false }, crypto.randomUUID()]);
        expect(sql('AutomationDefinition', item.id)[0].Enabled).toBe(false); expect((await client(page, 'automationApi', 'automationScheduleGet', [item.id])).configuration?.enabled ?? false).toBe(false);
      } catch (error) { throw new AggregateError([...(scenarioError ? [scenarioError] : []), error], 'Owned synthetic schedule opt-out failed'); }
    }
  });
});
