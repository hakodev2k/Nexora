import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    clearMocks: true,
    restoreMocks: true,
    reporters: ['default', ['junit', { outputFile: 'artifacts/vitest-junit.xml' }]],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'cobertura'],
      reportsDirectory: 'artifacts/coverage',
      include: ['src/App.tsx', 'src/api.ts', 'src/i18n.ts'],
      exclude: ['src/main.tsx']
    }
  }
});
