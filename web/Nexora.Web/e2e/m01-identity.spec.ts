import { execFileSync } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const operatorCli = process.env.NEXORA_E2E_OPERATOR_CLI;
if (!operatorCli) {
  throw new Error('E2E_BLOCKED: an operator CLI is required to read the approved local capture boundary.');
}

function readSyntheticCodes(purpose: string): string[] {
  try {
    const output = execFileSync(operatorCli, ['read-account-messages'], {
      encoding: 'utf8',
      stdio: ['ignore', 'pipe', 'pipe'],
      env: process.env,
      maxBuffer: 64 * 1024
    });

    const prefix = purpose + ' ';
    const tokenMarker = ' token ';
    return output.split(/\r?\n/)
      .filter((line) => line.startsWith(prefix) && line.includes(tokenMarker))
      .map((line) => line.slice(line.indexOf(tokenMarker) + tokenMarker.length).trim())
      .filter(Boolean);
  } catch {
    // CLI output can contain a synthetic secret. Never surface it in a test error.
    return [];
  }
}

async function waitForSyntheticCode(purpose: string, previous: Set<string>): Promise<string> {
  let captured = '';
  await expect.poll(() => {
    const value = readSyntheticCodes(purpose).find((code) => !previous.has(code));
    if (value) {
      captured = value;
    }

    return Boolean(value);
  }, {
    timeout: 15_000,
    intervals: [100, 250, 500, 1_000]
  }).toBe(true);

  return captured;
}

function syntheticEmail(): string {
  return 'e2e-' + randomUUID().replaceAll('-', '') + '@example.invalid';
}

function syntheticPassword(replacement = false): string {
  return (replacement ? 'y' : 'x').repeat(18);
}

async function registerVerifyAndLogin(page: Page) {
  const email = syntheticEmail();
  const password = syntheticPassword();

  await page.goto('/register');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Mật khẩu').fill(password);
  await page.getByLabel('Timezone IANA').fill('Asia/Ho_Chi_Minh');
  const previousCodes = new Set(readSyntheticCodes('EmailVerification'));
  await page.getByRole('button', { name: 'Tạo tài khoản' }).click();
  await expect(page).toHaveURL(/verify-email/);

  const verificationCode = await waitForSyntheticCode('EmailVerification', previousCodes);
  await page.getByLabel('Mã xác minh').fill(verificationCode);
  await page.getByRole('button', { name: 'Xác minh email' }).click();
  await expect(page.getByRole('heading', { name: 'Email đã được xác minh' })).toBeVisible();
  await page.getByRole('button', { name: 'Tới đăng nhập' }).click();

  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Mật khẩu').fill(password);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL('/');

  return { email };
}

test('M01 register, local capture, verify, login, and keyboard-visible controls', async ({ page }, testInfo) => {
  await page.goto('/register');
  await expect(page.getByRole('heading', { name: 'Tạo tài khoản' })).toBeVisible();
  await page.keyboard.press('Tab');
  await expect(page.locator(':focus')).toBeVisible();

  const result = await new AxeBuilder({ page }).include('main').analyze();
  expect(result.violations).toEqual([]);

  await registerVerifyAndLogin(page);
  await expect(page.getByText('PERSONAL SPACE')).toBeVisible();
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('home.png'), fullPage: true });
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);

  await page.goto('/search');
  await expect(page.getByRole('heading', { name: 'Bắt đầu tìm kiếm', level: 2 })).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);

  await page.goto('/modules/FX22');
  await page.getByLabel('Tiêu đề', { exact: true }).fill('Synthetic contrast regression');
  await page.getByLabel('Language', { exact: true }).fill('javascript');
  await page.getByLabel('Source code / text', { exact: true }).fill('const answer = 42;');
  await page.getByRole('button', { name: 'Tạo snippet', exact: true }).click();
  await expect(page.locator('.snippet-code code')).toHaveText('const answer = 42;');
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);

  await page.goto('/settings/security');
  await expect(page.locator('table tbody tr')).toHaveCount(1);
  await page.setViewportSize({ width: 360, height: 800 });
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  for (const { route, width, height } of [
    { route: '/modules/FX24', width: 320, height: 568 },
    { route: '/modules/FX11', width: 768, height: 1024 },
    { route: '/modules/FX12', width: 768, height: 1024 },
    { route: '/modules/FX13', width: 768, height: 1024 }
  ]) {
    await page.setViewportSize({ width, height });
    await page.goto(route);
    await expect(page.locator('main h1')).toBeVisible();
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
});

test('M01 password reset uses the local operator boundary and does not auto-login', async ({ page }) => {
  const account = await registerVerifyAndLogin(page);

  await page.getByRole('button', { name: 'Đăng xuất', exact: true }).click();
  await expect(page).toHaveURL('/login');

  await page.goto('/password/forgot');
  await page.getByLabel('Email').fill(account.email);
  const previousCodes = new Set(readSyntheticCodes('PasswordReset'));
  await page.getByRole('button', { name: 'Gửi yêu cầu reset' }).click();
  await expect(page.getByText('Đã tiếp nhận yêu cầu')).toBeVisible();

  const resetCode = await waitForSyntheticCode('PasswordReset', previousCodes);
  await page.getByRole('button', { name: 'Nhập mã reset' }).click();
  const replacementPassword = syntheticPassword(true);
  await page.getByLabel('Mã reset').fill(resetCode);
  await page.getByLabel('Mật khẩu mới', { exact: true }).fill(replacementPassword);
  await page.getByLabel('Nhập lại mật khẩu mới').fill(replacementPassword);
  await page.getByRole('button', { name: 'Đặt mật khẩu mới' }).click();

  await expect(page).toHaveURL('/login');
  await expect(page.getByRole('heading', { name: 'Đăng nhập' })).toBeVisible();
  await page.getByLabel('Email').fill(account.email);
  await page.getByLabel('Mật khẩu').fill(replacementPassword);
  await page.getByRole('button', { name: 'Đăng nhập', exact: true }).click();
  await expect(page).toHaveURL('/');
});
