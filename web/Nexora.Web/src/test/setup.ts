import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { randomUUID } from 'node:crypto';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { server } from './server';

const nativeFetch = globalThis.fetch.bind(globalThis);
globalThis.fetch = ((input: RequestInfo | URL, init?: RequestInit) => {
  const resolved = typeof input === 'string' && input.startsWith('/')
    ? new URL(input, window.location.origin).toString()
    : input;
  return nativeFetch(resolved, init);
}) as typeof fetch;
window.fetch = globalThis.fetch;

if (!globalThis.crypto.randomUUID) {
  Object.defineProperty(globalThis.crypto, 'randomUUID', { value: randomUUID });
}

Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    addListener: () => undefined,
    removeListener: () => undefined,
    dispatchEvent: () => false
  })) as unknown as typeof window.matchMedia
});

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  cleanup();
  server.resetHandlers();
});
afterAll(() => server.close());
