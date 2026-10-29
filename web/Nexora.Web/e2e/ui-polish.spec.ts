import { expect, test } from '@playwright/test';

test('untouched sign-in fields stay neutral, touch actions fit, and reduced motion is respected', async ({ page }, info) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('/login');
  const email = page.getByLabel('Email', { exact: true });
  await expect(email).toBeVisible();
  // Empty required controls must not look like errors before the first interaction.
  expect(await email.evaluate(element => getComputedStyle(element).borderTopColor)).not.toBe('rgb(188, 102, 112)');
  const submit = page.getByRole('button', { name: 'Đăng nhập', exact: true });
  expect((await submit.boundingBox())!.height).toBeGreaterThanOrEqual(44);
  expect(await submit.evaluate(element => getComputedStyle(element).transitionDuration)).toBe('0s');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.screenshot({ path: info.outputPath('sign-in-polish.png'), mask: [page.locator('input[type="password"]')] });
});
