import { expect, test, vi } from 'vitest';
import { runBrowserJwt } from './ToolboxBrowserJwt';

const token = 'eyJhbGciOiJub25lIn0.eyJzdWIiOiJzeW50aGV0aWMifQ.';

test('decodes browser text only after a payload-free authority check and never claims verification', async () => {
  const authorize = vi.fn(async () => undefined);
  const fetchSpy = vi.spyOn(globalThis, 'fetch');
  const result = await runBrowserJwt(token, authorize);
  expect(authorize).toHaveBeenCalledWith();
  expect(fetchSpy).not.toHaveBeenCalled();
  expect(JSON.parse(result.output)).toEqual({ header: { alg: 'none' }, payload: { sub: 'synthetic' }, signatureVerified: false });
  expect(result.warning).toContain('NOT verified');
});

test('denial prevents decode and malformed UTF-8, compact syntax and oversized inputs fail safely', async () => {
  const denial = new Error('Denied');
  await expect(runBrowserJwt(token, async () => { throw denial; })).rejects.toBe(denial);
  for (const invalid of ['a.b.c.d', 'a.b.c', 'eyJhbGciOiJub25lIn0._w.', 'x'.repeat(32769)]) {
    await expect(runBrowserJwt(invalid, async () => undefined)).rejects.toThrow('JWT input');
  }
});
