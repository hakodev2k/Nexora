import AxeBuilder from '@axe-core/playwright';
import { expect, test } from '@playwright/test';

test('bootstrap dependency failure retains an accessible main landmark and keyboard retry', async ({ page }) => {
  // Deliberate fault injection covers the UI error contract. This test does not
  // establish SQL, authentication or business-workflow E2E correctness.
  await page.route('**/api/v1/me', (route) => route.fulfill({
    status: 503,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'Persistence is temporarily unavailable.', status: 503 })
  }));
  await page.goto('/register');
  await expect(page.getByRole('heading', { name: 'Local API chưa sẵn sàng' })).toBeVisible();
  await expect(page.getByRole('main')).toBeVisible();
  const result = await new AxeBuilder({ page }).analyze();
  expect(result.violations).toEqual([]);
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Thử lại' })).toBeFocused();
  const retryRequest = page.waitForResponse((response) => new URL(response.url()).pathname === '/api/v1/me');
  await page.keyboard.press('Enter');
  expect((await retryRequest).status()).toBe(503);
  await expect(page.getByRole('main')).toBeVisible();
  await expect(page).toHaveURL(/\/register$/);
});
