import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.NEXORA_E2E_BASE_URL;
const operatorCli = process.env.NEXORA_E2E_OPERATOR_CLI;
const runId = process.env.NEXORA_E2E_RUN_ID;

if (!baseURL || !operatorCli || !runId || !/^nexora-e2e-[a-z0-9-]{8,64}$/.test(runId)) {
  throw new Error(
    'E2E_BLOCKED: NEXORA_E2E_BASE_URL, NEXORA_E2E_OPERATOR_CLI, and an isolated NEXORA_E2E_RUN_ID are required. ' +
    'The target must use synthetic SQL data and an operator-only local capture directory.'
  );
}

let target: URL;
try {
  target = new URL(baseURL);
} catch {
  throw new Error('E2E_BLOCKED: NEXORA_E2E_BASE_URL must be a loopback HTTP(S) URL.');
}

if (
  !['http:', 'https:'].includes(target.protocol) ||
  !['localhost', '127.0.0.1', '::1'].includes(target.hostname) ||
  target.username ||
  target.password ||
  target.search ||
  target.hash
) {
  throw new Error('E2E_BLOCKED: browser tests only target an isolated loopback HTTP(S) URL without credentials.');
}

export default defineConfig({
  testDir: './e2e',
  outputDir: 'artifacts/playwright-output',
  timeout: 45_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  forbidOnly: Boolean(process.env.CI),
  retries: 0,
  reporter: [
    ['line'],
    ['junit', { outputFile: 'artifacts/playwright-junit.xml' }]
  ],
  use: {
    baseURL,
    ignoreHTTPSErrors: false,
    screenshot: 'off',
    video: 'off',
    trace: 'off'
  },
  projects: [
    { name: 'chromium-desktop', use: { ...devices['Desktop Chrome'] } },
    { name: 'chromium-mobile', use: { ...devices['Pixel 5'] } }
  ]
});
