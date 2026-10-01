import { apiFetch, jsonMutationHeaders } from './api';

export type TimeEntry = { id: string; startAt: string; endAt: string | null; description: string | null; category: string | null; status: string; durationMilliseconds: number; updatedAt: string; eTag: string };
export type TimePage = { items: TimeEntry[]; nextCursor: string | null };
export type TimeCommand = { startAt: string; endAt: string; description: string | null; category: string | null; confirmOverlap: boolean };
export type TimeCorrection = { id: string; action: string; at: string; before: TimeEntry };
export const timeCapabilities = () => apiFetch<Record<string, boolean>>('/api/v1/time/capabilities');
export const timeEntries = (trash: boolean, cursor?: string) => apiFetch<TimePage>(`/api/v1/time/entries?trash=${trash}${cursor ? `&cursor=${encodeURIComponent(cursor)}` : ''}`);
export const timeTimer = () => apiFetch<TimeEntry | null>('/api/v1/time/timer');
export const timeReport = () => apiFetch<{ grossDurationMilliseconds: number; entryCount: number; hasOverlaps: boolean }>('/api/v1/time/report');
export const timeHistory = (id: string, cursor?: string) => apiFetch<{ items: TimeCorrection[]; nextCursor: string | null }>(`/api/v1/time/entries/${encodeURIComponent(id)}/history${cursor ? `?cursor=${encodeURIComponent(cursor)}` : ''}`);
export const timeSave = (body: TimeCommand, key: string, entry?: TimeEntry) => apiFetch<TimeEntry>(`/api/v1/time/entries${entry ? `/${entry.id}` : ''}`, { method: entry ? 'PUT' : 'POST', headers: jsonMutationHeaders(key, entry ? { 'If-Match': entry.eTag } : {}), body: JSON.stringify(body) });
export const timeStart = (description: string | null, category: string | null, key: string, resumeEntryId?: string) => apiFetch<TimeEntry>('/api/v1/time/timer', { method: 'POST', headers: jsonMutationHeaders(key), body: JSON.stringify({ description, category, resumeEntryId: resumeEntryId ?? null }) });
export const timeStop = (entry: TimeEntry, key: string) => apiFetch<TimeEntry>(`/api/v1/time/timer/${entry.id}/stop`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': entry.eTag }) });
export const timeTransition = (entry: TimeEntry, restore: boolean, key: string) => apiFetch<TimeEntry>(`/api/v1/time/entries/${entry.id}/${restore ? 'restore' : 'trash'}`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': entry.eTag }) });
