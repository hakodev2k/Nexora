/** Search only the already-authorized rows returned by the current admin API.
 * No network calls, hidden grants, cached cross-owner records, or persistence.
 */
export function normalizeAdminFilter(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/gi, 'd')
    .toLocaleLowerCase('vi').trim().replace(/\s+/g, ' ');
}

function matchesAllTokens(values: readonly string[], query: string): boolean {
  const tokens = normalizeAdminFilter(query).slice(0, 160).split(' ').filter(Boolean);
  if (!tokens.length) return true;
  const haystack = normalizeAdminFilter(values.join(' '));
  return tokens.every(token => haystack.includes(token));
}

export function filterAdminUsers<T extends { displayName: string; email: string; role: string; state: string }>(
  users: readonly T[], query: string,
): T[] {
  return users.filter(user => matchesAllTokens([user.displayName, user.email, user.role, user.state], query));
}

export function filterAdminModules<T extends { code: string; state: string }>(
  modules: readonly T[], query: string,
): T[] {
  return modules.filter(module => matchesAllTokens([module.code, module.state], query));
}

export function filterAdminActionGrants<T extends { actionKey: string; effect: string; status: string }>(
  grants: readonly T[], query: string,
): T[] {
  return grants.filter(grant => matchesAllTokens([grant.actionKey, grant.effect, grant.status], query));
}
