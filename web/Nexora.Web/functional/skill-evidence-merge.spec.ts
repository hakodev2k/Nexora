import { test, expect, api, card, login, sql, unique } from './fixtures';
import { response } from './time-focus-helpers';
import { startedAccess } from './started-flow-access';

test('Skill evidence/merge: native Course links, overlap preservation, readonly source and current owner denial', async ({ page, browser }) => {
  test.setTimeout(180000); page.setDefaultTimeout(15000); await page.goto('/'); const access = await startedAccess(browser, page, ['FX40']);
  const foreign = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); let failed = false;
  try {
    const prefix = unique('EvidenceMerge'), root = '/api/v1/learning/skills';
    const skills: string[] = [];
    for (const [name, level] of [['Source', 'Beginner'], ['Target', 'Expert']]) {
      const created = await response(page, root, 'POST', { title: prefix + name, level, description: name + ' retained metadata' });
      expect(created.status).toBe(201); skills.push(created.body.itemId);
    }
    const [sourceId, targetId] = skills; const courseName = prefix + 'Course';
    const course = await response(page, '/api/v1/learning/courses', 'POST', { title: courseName, provider: null, url: null, notes: null, progressMode: 'ManualPercent', startedOn: null });
    expect(course.status).toBe(201); const courseId = course.body.itemId;
    await page.goto('/'); await page.locator('.module-card').filter({ has: page.getByRole('heading', { name: 'FX40', exact: true }) }).getByRole('button', { name: 'Mở module', exact: true }).click();
    async function search(name: string, status = 'Active') {
      await expect(page.getByLabel('Status', { exact: true })).toBeEnabled(); await page.getByLabel('Status', { exact: true }).selectOption(status);
      await page.getByLabel('Title search', { exact: true }).fill(name); await page.getByRole('button', { name: 'Search Skill', exact: true }).click(); await expect(card(page, name)).toBeVisible();
    }
    // Add the same real Course from both native panels, preserving each interpretation.
    for (const suffix of ['Source', 'Target']) {
      await search(prefix + suffix); await card(page, prefix + suffix).getByRole('button', { name: 'Evidence', exact: true }).click();
      await page.getByRole('button', { name: 'Tải evidence hiện tại', exact: true }).click(); await expect(page.getByText('Chưa có evidence.', { exact: true })).toBeVisible();
      await page.getByRole('combobox', { name: /^Trạng thái Course/ }).selectOption('Planned'); await page.getByRole('button', { name: 'Chọn từ Course hiện tại', exact: true }).click();
      for (let n = 0; n < 100 && !await page.getByRole('combobox', { name: /^Course/ }).locator(`option[value="${courseId}"]`).count(); n++) {
        const more = page.getByRole('button', { name: 'Tải thêm Course', exact: true });
        await expect.poll(async () => await page.getByRole('combobox', { name: /^Course/ }).locator(`option[value="${courseId}"]`).count() > 0 || await more.isVisible()).toBe(true);
        if (await page.getByRole('combobox', { name: /^Course/ }).locator(`option[value="${courseId}"]`).count()) break;
        await more.click(); await expect(more).toBeEnabled();
      }
      await page.getByRole('combobox', { name: /^Course/ }).selectOption(courseId); await page.getByRole('textbox', { name: /^Ghi chú/ }).fill(suffix + ' private interpretation');
      await page.getByRole('button', { name: 'Ghi evidence', exact: true }).click(); await expect(page.getByText('Đã ghi thay đổi. Tải lại để đọc evidence và revision hiện tại.', { exact: true })).toBeVisible();
      await page.getByRole('button', { name: 'Tải evidence hiện tại', exact: true }).click(); await expect(page.getByRole('heading', { name: courseName, exact: true })).toBeVisible();
      await expect(page.getByText(suffix + ' private interpretation', { exact: true })).toBeVisible(); await page.getByRole('button', { name: 'Back to Skills', exact: true }).click();
    }
    const sourceBeforeMerge = (await response(page, root + '/' + sourceId, 'GET')).body;
    expect((await response(page, root + '/' + sourceId + '/evidence', 'POST', { resourceType: 'learning.Course', resourceId: courseId, note: 'Duplicate must not overwrite' }, sourceBeforeMerge.etag)).status).toBe(409);
    await search(prefix + 'Source'); await card(page, prefix + 'Source').getByRole('button', { name: 'Merge into another Skill', exact: true }).click();
    await page.getByLabel('Tìm Skill đích', { exact: true }).fill(prefix + 'Target'); await page.getByRole('button', { name: 'Search targets', exact: true }).click();
    await page.getByRole('button', { name: 'Preview merge into ' + prefix + 'Target', exact: true }).click();
    const modal = page.getByRole('dialog'); await expect(modal).toContainText('1 liên kết trùng'); await expect(modal).toContainText('Expert');
    await modal.getByRole('button', { name: 'Confirm merge', exact: true }).click(); await expect(modal).toHaveCount(0);
    const source = (await response(page, root + '/' + sourceId, 'GET')).body, target = (await response(page, root + '/' + targetId, 'GET')).body;
    expect(source.status).toBe('Archived'); expect(source.mergedIntoId.toLowerCase()).toBe(targetId.toLowerCase()); expect(source.level).toBe('Beginner'); expect(target.level).toBe('Expert');
    expect((await response(page, root + '/' + sourceId + '/evidence', 'POST', { resourceType: 'learning.Course', resourceId: courseId, note: 'Readonly mutation denied' }, source.etag)).status).toBe(409);
    for (const [id, note] of [[sourceId, 'Source'], [targetId, 'Target']]) {
      const evidence = (await response(page, root + '/' + id + '/evidence', 'GET')).body;
      expect(evidence.items).toHaveLength(1); expect(evidence.items[0].resourceId.toLowerCase()).toBe(courseId.toLowerCase()); expect(evidence.items[0].note).toBe(note + ' private interpretation');
    }
    expect(sql('Skill', sourceId)[0].OwnerId.toLowerCase()).toBe(access.me.personalSpaceId.toLowerCase());
    await search(prefix + 'Source', 'Archived'); await card(page, prefix + 'Source').getByRole('button', { name: 'Merge history', exact: true }).click(); await expect(page.getByRole('dialog')).toContainText(prefix + 'Target');
    await page.getByRole('button', { name: 'Read preserved source evidence', exact: true }).click(); await page.getByRole('button', { name: 'Tải evidence hiện tại', exact: true }).click();
    await expect(page.getByText('Source private interpretation', { exact: true })).toBeVisible(); await expect(page.getByRole('button', { name: 'Ghi evidence', exact: true })).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: `artifacts/skill-evidence-merge-${test.info().project.name}.png`, fullPage: true });
    const other = await foreign.newPage(); await login(other, 'UserB'); const otherMe = await api(other, 'getMe'); await access.grant(otherMe.id, 'FX40', true);
    expect((await response(other, root + '/' + sourceId + '/evidence', 'GET')).status).toBe(404);
    expect((await response(other, root + '/' + targetId + '/merge-history', 'GET')).status).toBe(404);
  } catch (error) { failed = true; throw error; }
  finally { try { await access.close(); } catch (error) { if (!failed) throw error; console.error('Skill evidence/merge authority restoration failed after original failure'); } finally { await foreign.close(); } }
});
