import { apiFetch, jsonMutationHeaders } from './api';
export type FocusPreferences = { focusMinutes: number; shortBreakMinutes: number; longBreakMinutes: number; cycleLength: number; etag: string };
export type FocusSession = { id: string; phase: string; state: string; plannedSeconds: number; elapsedMilliseconds: number; remainingMilliseconds: number; startedAt: string; lastRunAt: string | null; completedAt: string | null; etag: string };
export const focusCapabilities = () => apiFetch<Record<string, boolean>>('/api/v1/focus/capabilities');
export const focusList = (activeOnly = false, cursor?: string) => apiFetch<{ items: FocusSession[]; nextCursor: string | null }>(`/api/v1/focus/sessions?activeOnly=${activeOnly}${cursor ? `&cursor=${cursor}` : ''}`);
export const focusPreferences = () => apiFetch<FocusPreferences>('/api/v1/focus/preferences');
export const focusSavePreferences = (body: FocusPreferences, key: string) => apiFetch<FocusPreferences>('/api/v1/focus/preferences', { method: 'PUT', headers: jsonMutationHeaders(key, { 'If-Match': body.etag }), body: JSON.stringify({ focusMinutes: body.focusMinutes, shortBreakMinutes: body.shortBreakMinutes, longBreakMinutes: body.longBreakMinutes, cycleLength: body.cycleLength }) });
export const focusStart = (phase: string, key: string) => apiFetch<FocusSession>('/api/v1/focus/sessions', { method: 'POST', headers: jsonMutationHeaders(key), body: JSON.stringify({ phase }) });
export const focusTransition = (session: FocusSession, action: 'pause' | 'resume' | 'cancel', key: string) => apiFetch<FocusSession>(`/api/v1/focus/sessions/${session.id}/${action}`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': session.etag }) });

export const focusRecordTime = (session: FocusSession, confirmOverlap: boolean, key: string) => apiFetch<{ sessionId: string; entryId: string }>(`/api/v1/focus/sessions/${session.id}/record-time`, { method: 'POST', headers: jsonMutationHeaders(key, { 'If-Match': session.etag }), body: JSON.stringify({ confirmOverlap }) });
