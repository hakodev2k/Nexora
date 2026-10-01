import { test, expect, api, sql, sqlProfile, sourceTask, unique } from './fixtures';

test('FN-028 Settings theme: explicit save, SQL owner/value, reload and unchanged module grants', async ({ page }) => {
  await page.goto('/settings/profile'); const before = await api(page, 'getMe');
  const save = page.getByRole('button', { name: 'Lưu preferences', exact: true });
  for (const mode of ['Dark', 'Light', 'System']) {
    await page.locator('#theme-mode').selectOption(mode); await save.click();
    await expect(save).toBeEnabled();
    let preference: any;
    await expect.poll(async () => {
      preference = (await api(page, 'listPreferences')).items.find((item: any) => item.preferenceKey === 'theme');
      return preference ? JSON.parse(preference.valueJson).mode : null;
    }).toBe(mode);
    const persisted = sql('Preference', preference.id)[0];
    expect(persisted.OwnerId.toLowerCase()).toBe(before.personalSpaceId.toLowerCase());
    expect(JSON.parse(persisted.ValueJson).mode).toBe(mode);
    await page.reload(); await expect(page.locator('#theme-mode')).toHaveValue(mode);
    if (mode !== 'System') await expect(page.locator('html')).toHaveAttribute('data-theme', mode.toLowerCase());
  }
  expect((await api(page, 'getMe')).modules).toEqual(before.modules);
});

test('FN-029 Profile: invalid timezone preserves draft, vi/en persistence and Task instant unchanged', async ({ page }) => {
  await page.goto('/'); const original = await api(page, 'getMe'); const { task } = await sourceTask(page); const taskBefore = sql('Task', task.id);
  try {
    await page.goto('/settings/profile'); const name = unique('profile');
    await page.locator('#profile-display-name').fill(name); await page.locator('#profile-timezone').fill('Invalid/TimeZone');
    await page.getByRole('button', { name: 'Lưu profile', exact: true }).click();
    await expect(page.getByRole('alert')).toBeVisible(); await expect(page.locator('#profile-display-name')).toHaveValue(name);
    expect(sqlProfile(original.id)[0].TimeZoneId).toBe(original.timeZoneId);
    await page.locator('#profile-timezone').fill('Etc/UTC'); await page.locator('#profile-locale').selectOption('en');
    await page.getByRole('button', { name: 'Lưu profile', exact: true }).click();
    await expect.poll(() => sqlProfile(original.id)[0].Locale).toBe('en');
    expect(sqlProfile(original.id)[0].DisplayName).toBe(name); expect(sqlProfile(original.id)[0].TimeZoneId).toBe('Etc/UTC');
    expect(sql('Task', task.id)).toEqual(taskBefore);
    await page.reload(); await expect(page.locator('#profile-timezone')).toHaveValue('Etc/UTC'); await expect(page.locator('#profile-locale')).toHaveValue('en');
    await expect(page.getByRole('button', { name: 'Save profile', exact: true })).toBeVisible();
    await page.locator('#profile-display-name').fill(original.displayName); await page.locator('#profile-timezone').fill(original.timeZoneId); await page.locator('#profile-locale').selectOption('vi');
    await page.getByRole('button', { name: 'Save profile', exact: true }).click(); await expect.poll(() => sqlProfile(original.id)[0].Locale).toBe('vi');
  } finally {
    await api(page, 'getMe'); await api(page, 'updateMe', [{ displayName: original.displayName, timeZoneId: original.timeZoneId, locale: original.locale }]);
  }
});

test('FN-030 Profile: real two-tab conflict, explicit comparison retains draft, server version recovery', async ({ page, context }) => {
  await page.goto('/settings/profile'); const original = await api(page, 'getMe'); const name = unique('profile-race');
  const other = await context.newPage();
  try {
    await other.goto('/settings/profile'); await other.locator('#profile-display-name').fill(name);
    await other.getByRole('button', { name: 'Lưu profile', exact: true }).click(); await expect.poll(() => sqlProfile(original.id)[0].DisplayName).toBe(name);
    await page.locator('#profile-display-name').fill('retained synthetic draft'); await page.getByRole('button', { name: 'Lưu profile', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Tải bản server để so sánh', exact: true })).toBeVisible();
    await expect(page.locator('#profile-display-name')).toHaveValue('retained synthetic draft'); expect(sqlProfile(original.id)[0].DisplayName).toBe(name);
    await page.getByRole('button', { name: 'Tải bản server để so sánh', exact: true }).click();
    await expect(page.getByRole('button', { name: 'Dùng bản server', exact: true })).toBeVisible(); await expect(page.locator('#profile-display-name')).toHaveValue('retained synthetic draft');
    await page.getByRole('button', { name: 'Dùng bản server', exact: true }).click(); await expect(page.locator('#profile-display-name')).toHaveValue(name);
    await page.locator('#profile-display-name').fill(original.displayName); await page.getByRole('button', { name: 'Lưu profile', exact: true }).click();
    await expect.poll(() => sqlProfile(original.id)[0].DisplayName).toBe(original.displayName);
  } finally {
    await api(page, 'getMe'); await api(page, 'updateMe', [{ displayName: original.displayName, timeZoneId: original.timeZoneId, locale: original.locale }]); await other.close();
  }
});
