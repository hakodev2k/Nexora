import { apiFetch } from './api';
export type AutomationStep = { id: number; action: { key: 'tasks.task.create'; version: 1 }; inputs: { projectId: string; title: string; description: string | null; startAt: string; endAt: string; priority: string } };
export type AutomationShape = { schemaVersion: 1; trigger: { key: 'manual'; version: 1 }; steps: AutomationStep[] };
export type AutomationDefinition = { id: string; title: string; currentVersion: number; state: string; enabled: boolean; updatedAt: string; etag: string; lifecycle: "Active" | "Trash"; deletionBatchId: string | null };
export type AutomationVersion = { definitionId: string; versionNumber: number; title: string; definitionJson: string; createdAt: string };
export type AutomationDetail = { definition: AutomationDefinition; version: AutomationVersion };
export type AutomationAck = { id: string; versionNumber: number; etag: string };
export type AutomationCommand = { title: string; definitionJson: string };
const root = '/api/v1/automation/definitions';
export const automationCapabilities = () => apiFetch<Record<string, boolean>>(root + '/capabilities');
export const automationList = (query: string, cursor?: string, lifecycle = "Active") => apiFetch<{ items: AutomationDefinition[]; nextCursor: string | null }>(root + '/?' + new URLSearchParams({ query, lifecycle, ...(cursor ? { cursor } : {}) }));
export const automationGet = (id: string) => apiFetch<AutomationDetail>(root + '/' + encodeURIComponent(id));
export const automationHistory = (id: string, beforeVersion?: number) => apiFetch<{ items: AutomationVersion[]; nextVersion: number | null }>(root + '/' + encodeURIComponent(id) + '/versions' + (beforeVersion ? '?beforeVersion=' + beforeVersion : ''));
export const automationValidate = (body: AutomationCommand) => apiFetch<{ shapeAccepted: boolean; code: string; executionAvailable: boolean; verifiedSimulation: boolean; referencesValid: boolean }>(root + '/validate', { method: 'POST', body: JSON.stringify(body) });
export const automationSave = (id: string | null, etag: string | null, body: AutomationCommand, key: string) => apiFetch<AutomationAck>(root + '/' + (id ? encodeURIComponent(id) : ''), {
  method: id ? 'PUT' : 'POST', headers: { 'Idempotency-Key': key, ...(etag ? { 'If-Match': etag } : {}) }, body: JSON.stringify(body)
}, id ? 200 : undefined);
export const automationExport = (id: string) => apiFetch<AutomationCommand & { schemaVersion: 1 }>(root + '/' + encodeURIComponent(id) + '/export');
export const automationImport = (body: AutomationCommand, key: string) => apiFetch<AutomationAck>(root + '/import', { method: 'POST', headers: { 'Idempotency-Key': key }, body: JSON.stringify({ schemaVersion: 1, ...body }) });
export const automationLifecycle = (id: string, etag: string, batch: string | null, key: string) => apiFetch<AutomationAck>(root + '/' + encodeURIComponent(id) + (batch ? '/restore' : '/trash'), { method: 'POST', headers: { 'If-Match': etag, 'Idempotency-Key': key }, ...(batch ? { body: JSON.stringify({ deletionBatchId: batch }) } : {}) });
