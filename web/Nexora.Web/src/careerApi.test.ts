import { http, HttpResponse } from 'msw';
import { expect, it } from 'vitest';
import { careerAppointments } from './careerApi';
import { server } from './test/server';

it('appointment first page omits the nullable UUID cursor and continuation forwards the exact cursor', async () => {
  const id = 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa', cursor = 'bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb';
  const requests: URL[] = [];
  server.use(http.get('*/api/v1/career/jobs/:id/events', ({ request }) => {
    const url = new URL(request.url); requests.push(url);
    // Mirrors the ASP.NET Guid? query boundary: omitted is valid, empty is not.
    if (url.searchParams.has('cursor') && !url.searchParams.get('cursor')) return HttpResponse.json({ code: 'InvalidRequest', title: 'The request body is invalid.' }, { status: 400 });
    return HttpResponse.json({ items: [], nextCursor: null });
  }));
  expect((await careerAppointments(id)).items).toEqual([]);
  expect((await careerAppointments(id, '')).items).toEqual([]);
  expect((await careerAppointments(id, cursor)).items).toEqual([]);
  expect(requests[0].searchParams.has('cursor')).toBe(false); expect(requests[1].searchParams.has('cursor')).toBe(false);
  expect(requests[2].searchParams.get('cursor')).toBe(cursor);
});
