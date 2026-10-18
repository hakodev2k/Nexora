import { apiFetch, jsonMutationHeaders, NexoraApiError } from './api';
import { recentTypes, type RecentType } from './recentApi';

export type CommandScreen = 'home' | 'search' | 'favorites' | 'recents' | 'notifications' | 'module' | 'finance' | 'resource';
export type CommandHandoff = { screen: CommandScreen; moduleCode: string | null; resourceType: RecentType | null; resourceId: string | null; mode: 'create' | 'clear' | null };
const targets: Record<string, { screen: CommandScreen; moduleCode?: string; mode?: 'create' | 'clear' }> = {
  home: { screen: 'home' }, search: { screen: 'search' }, favorites: { screen: 'favorites' }, recents: { screen: 'recents' }, notifications: { screen: 'notifications' },
  documents: { screen: 'module', moduleCode: 'FX20' }, tasks: { screen: 'module', moduleCode: 'FX12' }, calendar: { screen: 'module', moduleCode: 'FX13' }, finance: { screen: 'finance' },
  'create-project': { screen: 'module', moduleCode: 'FX11', mode: 'create' }, 'create-task': { screen: 'module', moduleCode: 'FX12', mode: 'create' }, 'create-event': { screen: 'module', moduleCode: 'FX13', mode: 'create' },
  'create-document': { screen: 'module', moduleCode: 'FX20', mode: 'create' }, 'create-finance-record': { screen: 'finance', mode: 'create' }, 'clear-recents': { screen: 'recents', mode: 'clear' }
};
export type PaletteCommand = { key: string; title: string; group: 'Navigate' | 'Create' | 'Actions' };
export type CommandPage = { items: PaletteCommand[]; canSearch: boolean; canReadRecent: boolean; canExecute: boolean };
export type CommandSelection = { kind: 'Command'; key: string } | { kind: 'Resource'; resourceType: RecentType; resourceId: string };
export async function listCommands(query: string, signal?: AbortSignal) {
  const page = await apiFetch<CommandPage>('/api/v1/commands?' + new URLSearchParams({ q: query }), { signal });
  if (!page || !Array.isArray(page.items) || page.items.length > 100 || page.items.some(item => !item || !Object.hasOwn(targets, item.key) || typeof item.title !== 'string' || !['Navigate', 'Create', 'Actions'].includes(item.group))
    || typeof page.canExecute !== 'boolean' || typeof page.canSearch !== 'boolean' || typeof page.canReadRecent !== 'boolean')
    throw new NexoraApiError('Danh sách lệnh không tương thích.', 0, 'UnexpectedResponse');
  return page;
}
export async function executeCommand(selection: CommandSelection) {
  const result = await apiFetch<CommandHandoff>('/api/v1/commands/execute', { method: 'POST', headers: jsonMutationHeaders(), body: JSON.stringify(selection) }, 200);
  if (selection.kind === 'Resource') {
    if (result?.screen === 'resource' && result.resourceType === selection.resourceType && recentTypes.includes(result.resourceType)
      && typeof result.resourceId === 'string' && result.resourceId.toLowerCase() === selection.resourceId.toLowerCase() && result.moduleCode == null && result.mode == null) return result;
  } else {
    const expected = Object.hasOwn(targets, selection.key) ? targets[selection.key] : null;
    if (expected && result?.screen === expected.screen && (result.moduleCode ?? null) === (expected.moduleCode ?? null) && (result.mode ?? null) === (expected.mode ?? null)
      && result.resourceType == null && result.resourceId == null) return result;
  }
  throw new NexoraApiError('Không thể xác nhận luồng được chọn.', 0, 'UnexpectedResponse');
}
