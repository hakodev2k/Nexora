import { api, expect, login } from './fixtures';
import type { Browser, Page } from '@playwright/test';
export async function moduleApi(page: Page, file: 'timeApi' | 'focusApi', name: string, args: unknown[] = []) {
  return page.evaluate(async ({ file, name, args }) => { const m = await import(`/src/${file}.ts`); return m[name](...args); }, { file, name, args });
}
export async function response(page: Page, path: string, method: string, body?: unknown, etag?: string, key = crypto.randomUUID()) {
  return page.evaluate(async ({ path, method, body, etag, key }) => {
    const apiPath = '/src/api.ts'; const a = await import(apiPath); const csrf = await a.getCsrf();
    const r = await fetch(path, { method, headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf.requestToken, 'Idempotency-Key': key, ...(etag ? { 'If-Match': etag } : {}) }, ...(body === undefined ? {} : { body: JSON.stringify(body) }) });
    return { status: r.status, body: await r.json() };
  }, { path, method, body, etag, key });
}
export async function enableForTest(browser: Browser, page: Page, code: string) {
  const admin = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL }); const p = await admin.newPage(); await login(p, 'SuperAdmin');
  const me = await api(page, 'getMe'); const before = await api(p, 'getAdminUserAccess', [me.id]); const grant = before.moduleGrants.find((g: any) => g.code === code);
  const dependencies = code === 'FX19' ? before.moduleGrants.filter((g: any) => g.code === 'FX18' && !g.enabled) : [];
  const set = async (enabled: boolean) => { const preview = await api(p, 'previewAdminAccess', [me.id, { kind: 'modules', changes: [{ moduleId: grant.moduleId, enabled }] }]); expect(preview.blockers).toEqual([]); await api(p, 'commitAdminModuleGrant', [me.id, preview.etag, [{ moduleId: grant.moduleId, enabled }], preview.previewToken]); };
  for (const dependency of dependencies) { const preview = await api(p, 'previewAdminAccess', [me.id, { kind: 'modules', changes: [{ moduleId: dependency.moduleId, enabled: true }] }]); expect(preview.blockers).toEqual([]); await api(p, 'commitAdminModuleGrant', [me.id, preview.etag, [{ moduleId: dependency.moduleId, enabled: true }], preview.previewToken]); }
  if (!grant.enabled) await set(true);
  return { admin: p, me, set, close: async () => { if (!grant.enabled) await set(false); for (const d of dependencies.reverse()) { const preview = await api(p, 'previewAdminAccess', [me.id, { kind: 'modules', changes: [{ moduleId: d.moduleId, enabled: false }] }]); expect(preview.blockers).toEqual([]); await api(p, 'commitAdminModuleGrant', [me.id, preview.etag, [{ moduleId: d.moduleId, enabled: false }], preview.previewToken]); } await admin.close(); } };
}
