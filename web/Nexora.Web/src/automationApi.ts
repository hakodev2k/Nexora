import { apiFetch } from './api';
export type AutomationStep = { id: number; action: { key: 'tasks.task.create'; version: 1 }; inputs: { projectId: string; title: string; description: string | null; startAt: string; endAt: string; priority: string } };
export type AutomationShape = { schemaVersion: 1; trigger: { key: 'manual'; version: 1 }; steps: AutomationStep[] };
export type AutomationDefinition = { id: string; title: string; currentVersion: number; state: string; enabled: boolean; updatedAt: string; etag: string };
export type AutomationVersion = { definitionId: string; versionNumber: number; title: string; definitionJson: string; createdAt: string };
export type AutomationDetail = { definition: AutomationDefinition; version: AutomationVersion };
export type AutomationAck = { id: string; versionNumber: number; etag: string };
export type AutomationCommand = { title: string; definitionJson: string };
const root = '/api/v1/automation/definitions';
export const automationCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export const automationList = (query: string, cursor?: string) => apiFetch<{ items: AutomationDefinition[]; nextCursor: string | null }>(root + '/?' + new URLSearchParams({ query, ...(cursor ? { cursor } : {}) }));
export const automationGet = (id: string) => apiFetch<AutomationDetail>(root + '/' + encodeURIComponent(id));
export const automationHistory = (id: string, beforeVersion?: number) => apiFetch<{ items: AutomationVersion[]; nextVersion: number | null }>(root + '/' + encodeURIComponent(id) + '/versions' + (beforeVersion ? '?beforeVersion=' + beforeVersion : ''));
export const automationValidate = (body: AutomationCommand) => apiFetch<{ shapeAccepted: boolean; code: string; executionAvailable: boolean; verifiedSimulation: boolean }>(root + '/validate', { method: 'POST', body: JSON.stringify(body) });
export const automationSave = (id: string | null, etag: string | null, body: AutomationCommand, key: string) => apiFetch<AutomationAck>(root + '/' + (id ? encodeURIComponent(id) : ''), {
  method: id ? 'PUT' : 'POST', headers: { 'Idempotency-Key': key, ...(etag ? { 'If-Match': etag } : {}) }, body: JSON.stringify(body)
}, id ? 200 : undefined);
