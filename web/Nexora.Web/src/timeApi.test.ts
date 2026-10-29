import { expect, it } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from './test/server';
import { timeTimer } from './timeApi';

it('normalizes the real empty204 timer response to the declared no-running null contract', async () => {
  server.use(http.get('*/api/v1/time/timer', () => new HttpResponse(null, { status: 204 })));
  expect(await timeTimer()).toBeNull();
});
it('preserves an actual running timer response without fabricating state', async () => {
  const row = { id: '77777777-7777-4777-8777-777777777777', startAt: '2026-10-01T10:00:00.1234567Z', endAt: null, description: 'Synthetic running timer', category: null, status: 'Running', durationMilliseconds: 1, updatedAt: '2026-10-01T10:00:00.1234567Z', etag: '"original"' };
  server.use(http.get('*/api/v1/time/timer', () => HttpResponse.json(row)));
  expect(await timeTimer()).toEqual(row);
});
