import { test, expect, api, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';
import { startedAccess } from './started-flow-access';

test('Career appointments: native link/reload/unlink preserves Calendar source and denies foreign-owner references', async ({ page, browser }) => {
  test.setTimeout(150000); page.setDefaultTimeout(15000); await page.goto('/'); const access = await startedAccess(browser, page, ['FX13', 'FX39']);
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); let failed = false;
  try {
    const title = unique('AppointmentJob'), eventTitle = unique('InterviewEvent'), root = '/api/v1/career/jobs';
    const metadata = { title, companyId: null, url: null, location: null, workMode: null, employmentType: null, salaryText: null, salaryMin: null, salaryMax: null, currency: null, description: null, notes: 'Synthetic private job', source: null, discoveredOn: null, appliedOn: null };
    const job = await response(page, root, 'POST', { metadata, stage: 'Saved' }); expect(job.status).toBe(201); const id = job.body.itemId;
    const event = await api(page, 'createCalendarEvent', [eventTitle, 'Synthetic private interview', '2026-11-10T09:00:00Z', '2026-11-10T10:00:00Z', 'UTC']);
    const before = sql('Event', event.id)[0]; expect(before.OwnerId.toLowerCase()).toBe(access.me.personalSpaceId.toLowerCase());
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX39', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    const jobs = page.getByRole('region', { name: 'Jobs', exact: true }); await jobs.getByLabel('Job search', { exact: true }).fill(title); await jobs.getByRole('button', { name: 'Filter Jobs', exact: true }).click();
    await jobs.locator('.resource-card').filter({ has: page.getByRole('heading', { name: title, exact: true }) }).getByRole('button', { name: 'Calendar links', exact: true }).click();
    const panel = page.getByRole('region', { name: 'Linked Calendar Events', exact: true }); await panel.getByRole('button', { name: 'Tải lại Job/links', exact: true }).click(); await expect(panel.getByText('Chưa có Calendar link.', { exact: true })).toBeVisible();
    await panel.getByRole('combobox', { name: /^Thao tác/ }).selectOption('link'); await panel.getByRole('button', { name: 'Đọc lựa chọn Personal Event', exact: true }).click();
    const choices = panel.getByRole('combobox', { name: /^Scheduled Personal Event/ });
    for (let n = 0; n < 100; n++) {
      const option = choices.locator(`option[value="${event.id}"]`), more = panel.getByRole('button', { name: 'Tải thêm Event', exact: true });
      await expect.poll(async () => await option.count() > 0 || await more.isVisible()).toBe(true); if (await option.count()) break;
      await more.click(); await expect(more).toBeEnabled();
    }
    await choices.selectOption(event.id); await panel.getByRole('button', { name: 'Link Event đã chọn', exact: true }).click(); await expect(panel.getByText('Tải lại revision của Job trước thao tác tiếp theo.', { exact: true })).toBeVisible();
    await panel.getByRole('button', { name: 'Tải lại Job/links', exact: true }).click(); await expect(panel.getByText(eventTitle, { exact: true })).toBeVisible();
    let links = (await response(page, root + '/' + id + '/events', 'GET')).body; expect(links.items).toHaveLength(1); expect(links.items[0].eventId.toLowerCase()).toBe(event.id.toLowerCase()); expect(links.items[0].event.title).toBe(eventTitle);
    const link = links.items[0], current = (await response(page, root + '/' + id, 'GET')).body;
    expect((await response(page, root + '/' + id + '/events', 'POST', { eventId: event.id, event: null }, current.etag)).status).toBe(409);
    expect((await response(page, root + '/' + id + '/events', 'GET')).body.items).toHaveLength(1);
    // The source's current owner and Scheduled eligibility are mandatory, not granted by the Job.
    const other = await foreign.newPage(); await login(other, 'UserB'); const otherMe = await api(other, 'getMe');
    await access.grant(otherMe.id, 'FX13', true); await access.grant(otherMe.id, 'FX39', true);
    expect((await response(other, root + '/' + id + '/events', 'GET')).status).toBe(404);
    const otherEvent = await api(other, 'createCalendarEvent', [unique('ForeignInterview'), 'Other synthetic owner', '2026-11-11T09:00:00Z', '2026-11-11T10:00:00Z', 'UTC']);
    expect((await response(page, root + '/' + id + '/events', 'POST', { eventId: otherEvent.id, event: null }, current.etag)).status).toBe(404);
    expect((await response(page, root + '/' + id + '/events', 'GET')).body.items).toHaveLength(1);
    await page.reload(); await jobs.getByLabel('Job search', { exact: true }).fill(title); await jobs.getByRole('button', { name: 'Filter Jobs', exact: true }).click();
    await jobs.locator('.resource-card').filter({ has: page.getByRole('heading', { name: title, exact: true }) }).getByRole('button', { name: 'Calendar links', exact: true }).click();
    await panel.getByRole('button', { name: 'Tải lại Job/links', exact: true }).click(); await expect(panel.getByText(eventTitle, { exact: true })).toBeVisible();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true); await page.screenshot({ path: `artifacts/career-appointments-${test.info().project.name}.png`, fullPage: true });
    await panel.getByRole('button', { name: 'Unlink', exact: true }).click(); const modal = page.getByRole('dialog'); await expect(modal).toContainText('Event và reminder không bị xóa hoặc hủy.');
    await modal.getByRole('button', { name: 'Xác nhận unlink', exact: true }).click(); await expect(modal).toHaveCount(0); await panel.getByRole('button', { name: 'Tải lại Job/links', exact: true }).click(); await expect(panel.getByText('Chưa có Calendar link.', { exact: true })).toBeVisible();
    links = (await response(page, root + '/' + id + '/events', 'GET')).body; expect(links.items).toEqual([]);
    expect(sql('Event', event.id)[0]).toEqual(before); expect((await response(page, '/api/v1/calendar/events/' + event.id, 'GET')).body.title).toBe(eventTitle);
    expect(link.id).toBeTruthy();
  } catch (error) { failed = true; throw error; }
  finally { try { await access.close(); } catch (error) { if (!failed) throw error; console.error('Career appointment authority restoration failed after original failure'); } finally { await foreign.close(); } }
});
