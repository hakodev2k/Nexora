import { defineConfig } from '@playwright/test';
import isolated from './functional.config';

// A separately owned local runtime must opt into the already contracted SQL
// Task worker and scheduler. Default configuration tests remain in functional/.
// This suite never changes operator flags or activates external providers.
export default defineConfig({
  ...isolated,
  testDir: './automation-functional',
  reporter: [
    ['list'],
    ['json', { outputFile: 'artifacts/automation-functional-results.json' }],
    ['junit', { outputFile: 'artifacts/automation-functional-junit.xml' }],
  ],
});
