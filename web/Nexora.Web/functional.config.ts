import { defineConfig } from '@playwright/test';
import original from './playwright.config';
if (!process.env.NEXORA_E2E_ACCOUNTS || !process.env.NEXORA_E2E_SQL_OPERATOR) throw new Error('Isolated SQL operator and account manifest are required.');
export default defineConfig({
  ...original, testDir: './functional', timeout: 90_000, workers: 1,
  reporter: [['list'], ['json', { outputFile: 'artifacts/functional-results.json' }], ['junit', { outputFile: 'artifacts/functional-junit.xml' }]],
  use: { ...original.use, channel: undefined, launchOptions: process.env.NEXORA_E2E_EXECUTABLE ? { executablePath: process.env.NEXORA_E2E_EXECUTABLE } : {} },
  projects: [
    { name: 'desktop-large', use: { viewport: { width: 1920, height: 1080 } } },
    { name: 'desktop', use: { viewport: { width: 1366, height: 768 } } },
    { name: 'tablet', use: { viewport: { width: 768, height: 1024 } } },
    { name: 'mobile', use: { viewport: { width: 390, height: 844 } } },
    { name: 'mobile-small', use: { viewport: { width: 320, height: 568 } } }
  ]
});
