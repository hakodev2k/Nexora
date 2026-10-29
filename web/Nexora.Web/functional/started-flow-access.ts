import type { Browser, Page } from '@playwright/test';
import { writeFileSync } from 'node:fs';
import { api, expect, login } from './fixtures';

// Only normal preview/commit APIs on already Ready modules; restore exact prestate.
export async function startedAccess(browser: Browser, page: Page, codes: string[]) {
  const context = await browser.newContext({ baseURL: process.env.NEXORA_E2E_BASE_URL });
  const admin = await context.newPage(); await login(admin, 'SuperAdmin');
  const me = await api(page, 'getMe');
  const modules = (await api(admin, 'listAdminModules')).items.filter((m: any) => codes.includes(m.code));
  expect(modules).toHaveLength(codes.length);
  for (const module of modules) expect(module.state, module.code + ' remains Ready').toBe('Ready');
  const originals = new Map<string, { userId: string; moduleId: string; enabled: boolean }>();
  const checkpointPath = `artifacts/started-access-${Date.now()}-${me.id}.json`;
  function checkpoint(restored = false) {
    // Synthetic IDs and exact policy/grant prestate only, never cookies/passwords.
    writeFileSync(checkpointPath, JSON.stringify({ restored, modules: modules.map((m: any) => ({ id: m.id, code: m.code, state: m.state, systemEnabled: m.systemEnabled, registrationEnabled: m.registrationEnabled })), grants: [...originals.values()] }, null, 2), { mode: 0o600 });
  }
  checkpoint();
  async function grant(userId: string, code: string, enabled: boolean) {
    const current = (await api(admin, 'getAdminUserAccess', [userId])).moduleGrants.find((g: any) => g.code === code);
    const key = userId + ':' + code;
    if (!originals.has(key)) { originals.set(key, { userId, moduleId: current.moduleId, enabled: current.enabled }); checkpoint(); }
    if (current.enabled === enabled) return;
    const changes = [{ moduleId: current.moduleId, enabled }];
    const preview = await api(admin, 'previewAdminAccess', [userId, { kind: 'modules', changes }]); expect(preview.blockers).toEqual([]);
    await api(admin, 'commitAdminModuleGrant', [userId, preview.etag, changes, preview.previewToken]);
  }
  async function policy(module: any, systemEnabled: boolean, registrationEnabled: boolean) {
    const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.id === module.id);
    if (current.systemEnabled === systemEnabled && current.registrationEnabled === registrationEnabled) return;
    const change = { systemEnabled, registrationEnabled }, preview = await api(admin, 'previewModulePolicy', [module.id, change]);
    expect(preview.blockers).toEqual([]); await api(admin, 'commitModulePolicy', [module.id, preview.etag, change, preview.previewToken]);
  }
  async function close() {
    const failures: unknown[] = [];
    async function attempt(action: () => Promise<unknown>) {
      try { await action(); } catch (error) { failures.push(error); }
    }
    try {
      for (const [key, original] of [...originals].reverse()) await attempt(() => grant(original.userId, key.split(':')[1], original.enabled));
      for (const module of [...modules].reverse()) await attempt(() => policy(module, module.systemEnabled, module.registrationEnabled));
      for (const module of modules) {
        await attempt(async () => {
        const current = (await api(admin, 'listAdminModules')).items.find((m: any) => m.id === module.id);
        expect([current.state, current.systemEnabled, current.registrationEnabled]).toEqual([module.state, module.systemEnabled, module.registrationEnabled]);
        });
      }
      for (const original of originals.values()) await attempt(async () => { expect((await api(admin, 'getAdminUserAccess', [original.userId])).moduleGrants.find((g: any) => g.moduleId === original.moduleId).enabled).toBe(original.enabled); });
      if (!failures.length) checkpoint(true);
    } finally { await attempt(() => context.close()); }
    if (failures.length) throw new AggregateError(failures, 'Synthetic access restoration failed; inspect the pre-mutation checkpoint.');
  }
  try { for (const module of modules) { await policy(module, true, module.registrationEnabled); await grant(me.id, module.code, true); } }
  catch (error) {
    try { await close(); } catch (cleanupError) { throw new AggregateError([error, cleanupError], 'Access setup and restoration failed.'); }
    throw error;
  }
  return { me, grant, close };
}
