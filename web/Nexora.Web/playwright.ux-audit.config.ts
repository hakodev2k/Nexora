import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.NEXORA_E2E_BASE_URL;
const operator = process.env.NEXORA_E2E_OPERATOR_CLI;
const runId = process.env.NEXORA_E2E_RUN_ID;
const accounts = process.env.NEXORA_E2E_ACCOUNTS;
if (!baseURL || !operator || !runId || !accounts || !/^nexora-e2e-[a-z0-9-]{8,64}$/.test(runId)) {
  throw new Error('UIUX_AUDIT_BLOCKED: owned E2E loopback SQL/API environment and synthetic accounts are required.');
}
const target = new URL(baseURL);
if (!['localhost', '127.0.0.1', '[::1]'].includes(target.hostname) ||
    !['http:', 'https:'].includes(target.protocol) ||
    target.username || target.password || target.search || target.hash) {
  throw new Error('UIUX_AUDIT_BLOCKED: only a credential-free owned loopback target is allowed.');
}
export default defineConfig({
  testDir: './e2e',
  testMatch: 'pr4-uiux-audit.spec.ts',
  outputDir: 'artifacts/uiux-audit/playwright-private-output',
  timeout: 900_000,
  expect: { timeout: 10_000 },
  fullyParallel: false, workers: 1, retries: 0,
  reporter: [['line'], ['junit', { outputFile: 'artifacts/uiux-audit/playwright-junit.xml' }]],
  use: { baseURL, screenshot: 'off', trace: 'off', video: 'off', ignoreHTTPSErrors: false },
  projects: [
    { name: 'desktop-1440', use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 } } },
    { name: 'mobile-390', use: { ...devices['Pixel 5'], viewport: { width: 390, height: 844 } } },
  ],
});
