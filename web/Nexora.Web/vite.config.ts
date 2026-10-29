import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    // Chromium locks partial downloads on Windows. Generated QA output is not
    // application source and must not enter Vite's source watcher.
    watch: {
      ignored: ['**/artifacts/**', '**/test-results/**', '**/playwright-report/**']
    },
    proxy: {
      '/api': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false
      },
      '/health': {
        target: 'https://localhost:5001',
        changeOrigin: true,
        secure: false
      }
    }
  }
});
