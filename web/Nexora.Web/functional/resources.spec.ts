import { test, expect, api, sql, record, unique, card, dialog, confirm, sourceTask, login } from './fixtures';

test('Bookmark: validation, dirty discard, create/edit/archive/unarchive and SQL persistence', async ({ page }) => {
  await page.goto('/modules/FX21'); const title = unique('bookmark');
  let modal = await dialog(page, 'Thêm bookmark');
  await modal.getByRole('button', { name: 'Tạo bookmark', exact: true }).click();
  await expect(modal.getByText('URL phải là địa chỉ HTTP(S) đầy đủ.', { exact: true }).first()).toBeVisible();
  await page.locator('#bookmark-title').fill('unsaved'); await page.keyboard.press('Escape');
  await modal.getByRole('button', { name: 'Tiếp tục sửa' }).click(); await expect(page.locator('#bookmark-title')).toHaveValue('unsaved');
  await page.keyboard.press('Escape'); await modal.getByRole('button', { name: 'Bỏ bản nháp' }).click();
  await expect(page.getByRole('button', { name: 'Thêm bookmark', exact: true })).toBeFocused();
  modal = await dialog(page, 'Thêm bookmark'); await page.locator('#bookmark-url').fill('https://example.invalid/' + title); await page.locator('#bookmark-title').fill(title);
  await modal.getByRole('button', { name: 'Tạo bookmark', exact: true }).click(); await expect(modal).toHaveCount(0);
  let persisted = await record(page, 'listBookmarks', [true, title, 100], 'Bookmark', title); expect(persisted.row.Status).toBe('Active');
  await card(page, title).getByRole('button', { name: 'Sửa', exact: true }).click(); await page.locator('#bookmark-description').fill('Tiếng Việt <script>literal</script>');
  await page.getByRole('dialog').getByRole('button', { name: 'Lưu bookmark', exact: true }).click(); await expect(page.getByRole('dialog')).toHaveCount(0);
  expect(sql('Bookmark', persisted.item.id)[0].Description).toBe('Tiếng Việt <script>literal</script>');
  await card(page, title).getByRole('button', { name: 'Archive', exact: true }).click(); await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click(); expect(sql('Bookmark', persisted.item.id)[0].Status).toBe('Active');
  await card(page, title).getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive bookmark'); expect(sql('Bookmark', persisted.item.id)[0].Status).toBe('Archived');
  await page.getByLabel('Hiển thị bookmark đã archive').check(); await card(page, title).getByRole('button', { name: 'Unarchive' }).click();
  await expect.poll(() => sql('Bookmark', persisted.item.id)[0].Status).toBe('Active'); await page.reload(); await expect(card(page, title)).toBeVisible();
});

test('Tag: create, rename, cancel delete, delete and owner SQL', async ({ page }) => {
  await page.goto('/modules/FX24'); const name = unique('tag'); let modal = await dialog(page, 'Thêm tag');
  await page.locator('#organization-tag-name').fill(name); await page.locator('#organization-tag-color').fill('#2762cc'); await modal.getByRole('button', { name: 'Tạo tag' }).click(); await expect(modal).toHaveCount(0);
  const { item } = await record(page, 'listOrganizationTags', ['projects', '', 100], 'Tag', name, 'name');
  await card(page, name).getByRole('button', { name: 'Đổi tên' }).click(); await page.locator('#organization-tag-name').fill(name + '-edit'); await page.getByRole('dialog').getByRole('button', { name: 'Lưu tag' }).click();
  await expect.poll(() => sql('Tag', item.id)[0].Name).toBe(name + '-edit'); await page.reload();
  await card(page, name + '-edit').getByRole('button', { name: 'Xóa', exact: true }).click(); await page.getByRole('dialog').getByRole('button', { name: 'Hủy', exact: true }).click(); expect(sql('Tag', item.id)).toHaveLength(1);
  await card(page, name + '-edit').getByRole('button', { name: 'Xóa', exact: true }).click(); await confirm(page, 'Xóa tag'); expect(sql('Tag', item.id)).toHaveLength(0);
});

test('Finance: category, exact decimal record, edit, usage guard and unused delete', async ({ page }) => {
  await page.goto('/modules/FX27'); const name = unique('finance'); let modal = await dialog(page, 'Thêm category');
  await page.locator('#finance-category-title').fill(name); await modal.getByRole('button', { name: 'Tạo category' }).click(); await expect(modal).toHaveCount(0);
  const category = (await record(page, 'listFinanceCategories', ['', 100], 'Category', name)).item;
  modal = await dialog(page, 'Thêm record'); await page.locator('#finance-record-category').selectOption(category.id); await page.locator('#finance-record-amount').fill('123456.12345678'); await page.locator('#finance-record-note').fill(name);
  await modal.getByRole('button', { name: 'Tạo record' }).click(); await expect(modal).toHaveCount(0);
  const list = await api(page, 'listFinanceRecords', [{ query: name, limit: 100 }]); const item = list.items.find((i: any) => i.note === name); expect(item.amount).toBe('123456.12345678'); expect(sql('Record', item.id)[0].Amount).toBe(123456.12345678);
  await page.locator('.resource-card').filter({ hasText: name }).filter({ has: page.getByRole('heading', { name: '123456.12345678 VND', exact: true }) }).getByRole('button', { name: 'Sửa', exact: true }).click();
  await page.locator('#finance-record-amount').fill('7.25'); await page.getByRole('dialog').getByRole('button', { name: 'Lưu record' }).click(); await expect.poll(() => sql('Record', item.id)[0].Amount).toBe(7.25);
  await page.reload(); await expect(card(page, name).getByRole('button', { name: 'Đang dùng' })).toBeDisabled();
  modal = await dialog(page, 'Thêm category'); await page.locator('#finance-category-title').fill(name + '-unused'); await modal.getByRole('button', { name: 'Tạo category' }).click();
  const unused = (await record(page, 'listFinanceCategories', ['', 100], 'Category', name + '-unused')).item;
  await card(page, name + '-unused').getByRole('button', { name: 'Xóa', exact: true }).click(); await confirm(page, 'Xóa category'); expect(sql('Category', unused.id)).toHaveLength(0);
});

test('Bookmark: real two-tab conflict preserves draft and explicit revision recovery', async ({ page, context }) => {
  await page.goto('/modules/FX21'); const title = unique('conflict'); const item = await api(page, 'createBookmark', ['https://example.invalid/' + title, title, 'initial']); await page.reload();
  const other = await context.newPage(); await other.goto('/modules/FX21');
  await card(page, title).getByRole('button', { name: 'Sửa', exact: true }).click(); await card(other, title).getByRole('button', { name: 'Sửa', exact: true }).click();
  await other.locator('#bookmark-description').fill('other revision'); await other.getByRole('dialog').getByRole('button', { name: 'Lưu bookmark' }).click(); await expect(other.getByRole('dialog')).toHaveCount(0);
  await page.locator('#bookmark-description').fill('retained draft'); await page.getByRole('dialog').getByRole('button', { name: 'Lưu bookmark' }).click();
  await expect(page.getByRole('dialog').getByRole('button', { name: 'Dùng revision mới' })).toBeVisible(); await expect(page.locator('#bookmark-description')).toHaveValue('retained draft'); expect(sql('Bookmark', item.id)[0].Description).toBe('other revision');
  await page.getByRole('dialog').getByRole('button', { name: 'Dùng revision mới' }).click(); await page.getByRole('dialog').getByRole('button', { name: 'Lưu bookmark' }).click(); await expect(page.getByRole('dialog')).toHaveCount(0); expect(sql('Bookmark', item.id)[0].Description).toBe('retained draft'); await other.close();
});

test('Bookmark: offline failure retains draft, real retry commits one row', async ({ page, context }) => {
  await page.goto('/modules/FX21'); const title = unique('offline'); const modal = await dialog(page, 'Thêm bookmark'); await page.locator('#bookmark-url').fill('https://example.invalid/' + title); await page.locator('#bookmark-title').fill(title);
  await context.setOffline(true); await modal.getByRole('button', { name: 'Tạo bookmark' }).click(); await expect(modal.getByText(/Không thể kết nối/).first()).toBeVisible(); await expect(page.locator('#bookmark-title')).toHaveValue(title);
  await context.setOffline(false); await modal.getByRole('button', { name: 'Tạo bookmark' }).click(); await expect(modal).toHaveCount(0);
  const list = await api(page, 'listBookmarks', [true, title, 100]); expect(list.items.filter((i: any) => i.title === title)).toHaveLength(1); expect(sql('Bookmark', list.items[0].id)).toHaveLength(1);
});

test('Owner isolation: real User B login cannot read User A bookmark in API or UI', async ({ page, browser }) => {
  await page.goto('/'); const title = unique('owner-canary'); const item = await api(page, 'createBookmark', ['https://example.invalid/' + title, title, 'private synthetic data']); await record(page, 'listBookmarks', [true, title, 100], 'Bookmark', title);
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const other = await context.newPage(); await login(other, 'UserB');
  const before = sql('Bookmark', item.id);
  const status = await other.evaluate(async id => (await fetch('/api/v1/bookmarks/' + id)).status, item.id); expect(status).toBe(404);
  const mutationStatuses = await other.evaluate(async item => {
    const path = '/src/api.ts'; const m = await import(path); const results: number[] = [];
    for (const action of [() => m.updateBookmark(item.id, item.etag, item.url, 'cross-owner attempt', null), () => m.transitionBookmark(item.id, item.etag, 'Archived')]) {
      try { await action(); results.push(200); } catch (e: any) { results.push(e.status); }
    }
    return results;
  }, item);
  expect(mutationStatuses).toEqual([404, 404]); await other.goto('/modules/FX21'); await expect(card(other, title)).toHaveCount(0); expect(sql('Bookmark', item.id)).toEqual(before); await context.close();
});

test('Goal: title-only creation does not require optional numeric target', async ({ page }) => {
  await page.goto('/modules/FX16'); const title = unique('goal'); await page.locator('#goal-title').fill(title); await page.getByRole('button', { name: 'Tạo goal', exact: true }).click();
  const { row } = await record(page, 'listGoals', ['', '', 100], 'Goal', title); expect(row.Status).toBe('Draft'); await page.reload(); await expect(page.getByText(title, { exact: true }).first()).toBeVisible();
});

test('Goal: numeric progress and explicit completed/reopen/abandoned lifecycle', async ({ page }) => {
  await page.goto('/modules/FX16'); const title = unique('numeric'); await page.locator('#goal-title').fill(title); await page.locator('#goal-target-title').fill('Target'); await page.locator('#goal-target-value').fill('10'); await page.getByRole('button', { name: 'Tạo goal', exact: true }).click();
  const { item } = await record(page, 'listGoals', ['', '', 100], 'Goal', title); await page.getByRole('button', { name: 'Bắt đầu', exact: true }).click(); await expect.poll(() => sql('Goal', item.id)[0].Status).toBe('Active');
  let modal = await dialog(page, 'Ghi progress'); await page.locator('#goal-progress-value').fill('10'); await modal.getByRole('button', { name: 'Ghi progress', exact: true }).click(); await expect(modal).toHaveCount(0);
  const detail = await api(page, 'getGoal', [item.id]); expect(detail.targets[0].currentValue).toBe(10); expect(sql('GoalTarget', detail.targets[0].id)[0].CurrentValue).toBe(10); expect(sql('Goal', item.id)[0].Status).toBe('Active');
  await page.getByRole('button', { name: 'Hoàn thành', exact: true }).click(); await confirm(page, 'Hoàn thành'); await expect.poll(() => sql('Goal', item.id)[0].Status).toBe('Completed'); await page.getByRole('button', { name: 'Mở lại', exact: true }).click(); await page.getByRole('button', { name: 'Bỏ goal', exact: true }).click(); await confirm(page, 'Bỏ goal'); await expect.poll(() => sql('Goal', item.id)[0].Status).toBe('Abandoned');
});

test('Planner: pin, move day, unpin preserves source Task', async ({ page }) => {
  await page.goto('/'); const { task } = await sourceTask(page); const before = sql('Task', task.id); await page.goto('/modules/FX15'); const date = await page.locator('#planner-date').inputValue(); const modal = await dialog(page, 'Thêm pin'); for (let n = 0; n < 150; n++) {
    await expect(page.locator('#planner-task')).toBeEnabled();
    if (await page.locator('#planner-task option').evaluateAll((options, id) => options.some(option => (option as HTMLOptionElement).value === id), task.id)) break;
    const more = modal.getByRole('button', { name: 'Load more Task choices', exact: true });
    await expect(more).toBeVisible(); await more.click();
  }
  await page.locator('#planner-task').selectOption(task.id); await page.locator('#planner-notes').fill('planning only'); await modal.getByRole('button', { name: 'Pin vào plan', exact: true }).click(); await expect(modal).toHaveCount(0);
  const plan = await api(page, 'listPlanner', [date, date]); const pin = plan.pins.find((p: any) => p.taskId === task.id); expect(sql('PlannerPin', pin.id)[0].Notes).toBe('planning only');
  const row = page.locator('.resource-card').filter({ hasText: task.title }); await row.getByRole('button', { name: 'Ngày sau →' }).click(); await expect.poll(() => sql('PlannerPin', pin.id)[0].PlanDate.slice(0, 10)).not.toBe(date);
  await page.locator('#planner-date').fill(sql('PlannerPin', pin.id)[0].PlanDate.slice(0, 10)); await row.getByRole('button', { name: 'Unpin', exact: true }).click(); await confirm(page, 'Unpin'); await expect.poll(() => sql('PlannerPin', pin.id)).toHaveLength(0); expect(sql('Task', task.id)).toEqual(before);
});

test('Habit: real check-in correction, future schedule and pause/archive lifecycle', async ({ page }) => {
  await page.goto('/modules/FX17'); const title = unique('habit'); await page.locator('#habit-title').fill(title); const today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Ho_Chi_Minh' }).format(new Date()); await page.locator('#habit-effective').fill(today); await page.getByRole('button', { name: 'Tạo Habit' }).click();
  const { item } = await record(page, 'listHabits', ['', '', 100], 'Habit', title); let modal = await dialog(page, 'Check-in'); await page.locator('#habit-check-note').fill('first'); await modal.getByRole('button', { name: 'Record check-in' }).click(); await expect(modal).toHaveCount(0);
  let detail = await api(page, 'getHabit', [item.id]); const id = detail.checkIns[0].id; expect(sql('HabitCheckIn', id)[0].Count).toBe(1);
  modal = await dialog(page, 'Check-in'); await page.locator('#habit-check-count').fill('0'); await modal.getByRole('button', { name: 'Record check-in' }).click(); await expect(modal).toHaveCount(0); expect(sql('HabitCheckIn', id)[0].Count).toBe(0);
  modal = await dialog(page, 'Future schedule'); await modal.getByRole('button', { name: 'Save schedule' }).click(); await expect(modal).toHaveCount(0);
  await page.getByRole('button', { name: 'Pause', exact: true }).click(); await expect.poll(() => sql('Habit', item.id)[0].State).toBe('Paused'); await page.getByRole('button', { name: 'Resume' }).click(); await page.getByRole('button', { name: 'Archive', exact: true }).click(); await confirm(page, 'Archive Habit'); await expect.poll(() => sql('Habit', item.id)[0].State).toBe('Archived'); await page.getByRole('button', { name: 'Unarchive' }).click(); await expect.poll(() => sql('Habit', item.id)[0].State).toBe('Active'); expect(sql('HabitCheckIn', id)[0].Count).toBe(0);
});

test('Reminder: preset, None, Exact and remove retains None intent', async ({ page }) => {
  await page.goto('/'); const { task } = await sourceTask(page); await page.goto('/modules/FX14');
  const sourceChoice = page.locator('#reminder-source');
  for (let pageIndex = 0; pageIndex < 150 && await sourceChoice.locator('option').evaluateAll((options, value) => !options.some(option => (option as HTMLOptionElement).value === value), 'Task:' + task.id); pageIndex++) {
    const more = page.getByRole('button', { name: 'Load more Task sources', exact: true }); await expect(more).toBeVisible(); await expect(more).toBeEnabled(); await more.click(); await expect.poll(async () => await more.count() === 0 || await more.isEnabled()).toBe(true);
  }
  await expect(sourceChoice).toBeEnabled(); await sourceChoice.selectOption('Task:' + task.id);
  let modal = await dialog(page, 'Cấu hình reminder'); await modal.getByRole('button', { name: /Đặt reminder|Lưu thay đổi/ }).click(); await expect(modal).toHaveCount(0);
  const view = await api(page, 'getReminderSource', ['Task', task.id]); const id = view.reminder.id; expect(sql('Reminder', id)[0].ConfigType).toBe('BeforeStart15m');
  modal = await dialog(page, 'Cấu hình reminder'); await page.locator('#reminder-config').selectOption('None'); await modal.getByRole('button', { name: 'Lưu thay đổi' }).click(); await expect(modal).toHaveCount(0); expect(sql('Reminder', id)[0].State).toBe('None');
  modal = await dialog(page, 'Cấu hình reminder'); await page.locator('#reminder-config').selectOption('Exact'); const tomorrow = new Date(Date.now() + 86400000).toISOString().slice(0, 10); await page.locator('#reminder-exact-at').fill(tomorrow + 'T12:00'); await modal.getByRole('button', { name: 'Lưu thay đổi' }).click(); await expect(modal).toHaveCount(0); expect(sql('Reminder', id)[0].ConfigType).toBe('Exact');
  await page.getByRole('button', { name: 'Gỡ reminder', exact: true }).click(); await confirm(page, 'Gỡ reminder'); expect(sql('Reminder', id)[0].ConfigType).toBe('None'); expect(sql('Task', task.id)).toHaveLength(1);
});

// The operational Files branch must not be reported as passed on the disabled baseline.
test('Files: baseline availability gate is enforced in UI and direct API', async ({ page }, info) => {
  await page.goto('/modules/FX07'); await expect(page.getByRole('heading', { name: 'Module chưa khả dụng', exact: true })).toBeVisible();
  expect(await page.evaluate(async () => (await fetch('/api/v1/files')).status)).toBe(409);
  info.annotations.push({ type: 'gate', description: 'FX07 baseline disabled: upload/rename/download/restore/purge positive cases remain Not run.' });
});
