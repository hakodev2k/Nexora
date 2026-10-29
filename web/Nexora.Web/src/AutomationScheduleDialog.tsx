import { useEffect, useRef, useState } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationScheduleGet, automationSchedulePreview, automationScheduleSave, type AutomationScheduleConfig, type AutomationScheduleSnapshot } from './automationApi';

const canonicalETag = (value: string) => {
  if (!/^"[A-Za-z0-9+/]{11}="$/.test(value)) return false;
  const encoded = value.slice(1, -1), decoded = atob(encoded);
  return decoded.length === 8 && btoa(decoded) === encoded;
};
export function AutomationScheduleDialog({ id, onClose, onDenied }: { id: string; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const generation = useRef(0), flight = useRef(false), acknowledged = useRef(false);
  const pending = useRef<{ etag: string; key: string; body: AutomationScheduleConfig } | null>(null);
  const [snapshot, setSnapshot] = useState<AutomationScheduleSnapshot | null>(null);
  const [form, setForm] = useState<AutomationScheduleConfig>({ frequency: 'Daily', timeZoneId: 'UTC', hour: 9, minute: 0, weekdays: 127, startAt: '', endAt: null, missedPolicy: 'Skip', enabled: false });
  const [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false), [dirty, setDirty] = useState(false), [error, setError] = useState('');
  const [runs, setRuns] = useState<string[]>([]);
  function unavailable(e: unknown) {
    if ((e as { status?: number }).status === 404) { onClose(); return true; }
    return onDenied(e);
  }
  function accept(value: AutomationScheduleSnapshot) {
    if (value.definitionId !== id || typeof value.etag !== 'string' || !canonicalETag(value.etag) || !Array.isArray(value.nextRuns)) throw new Error('Schedule response không hợp lệ.');
    setSnapshot(value); setRuns(value.nextRuns); if (value.configuration) setForm(value.configuration); setDirty(false);
  }
  async function load() {
    if (flight.current || uncertain || (dirty && !window.confirm('Bỏ thay đổi schedule để tải lại?'))) return;
    const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const value = await automationScheduleGet(id); if (stamp === generation.current) accept(value); }
    catch (e) { if (stamp === generation.current && !unavailable(e)) setError(e instanceof Error ? e.message : 'Không tải được schedule.'); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  useEffect(() => { generation.current++; flight.current = false; void load(); return () => { generation.current++; flight.current = false; }; }, [id]);
  useEffect(() => registerDirtyLeaveGuard(() => {
    if (busy || uncertain) { setError('Đợi ACK hoặc retry cùng command trước khi rời trang.'); return true; }
    return dirty && !window.confirm('Bỏ thay đổi schedule chưa lưu?');
  }), [busy, uncertain, dirty]);
  useEffect(() => { const guard = (e: BeforeUnloadEvent) => { if (busy || uncertain || dirty) { e.preventDefault(); e.returnValue = ''; } }; window.addEventListener('beforeunload', guard); return () => window.removeEventListener('beforeunload', guard); }, [busy, uncertain, dirty]);
  function change(update: Partial<AutomationScheduleConfig>) { setForm(old => ({ ...old, ...update })); setDirty(true); setRuns([]); }
  function body(): AutomationScheduleConfig {
    const offsetTimestamp = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$/;
    if (!offsetTimestamp.test(form.startAt) || (form.endAt && !offsetTimestamp.test(form.endAt))) throw new Error('Start/end phải có explicit UTC offset.');
    const start = new Date(form.startAt), end = form.endAt ? new Date(form.endAt) : null;
    if (!Number.isFinite(start.getTime()) || (end && !Number.isFinite(end.getTime()))) throw new Error('Start/end phải là ISO timestamp có UTC offset.');
    return { ...form, startAt: form.startAt, endAt: form.endAt || null, weekdays: form.frequency === 'Weekly' ? form.weekdays : 127,
      cronExpression: form.frequency === 'Cron' ? form.cronExpression?.trim() || null : null,
      intervalMinutes: form.frequency === 'Interval' ? form.intervalMinutes ?? null : null };
  }
  async function preview() {
    if (flight.current || uncertain) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const value = await automationSchedulePreview(id, body()); if (stamp === generation.current) { setRuns(value.nextRuns); if (value.nextRuns.length === 0) setError('Không có occurrence tương lai trong start/end đã chọn.'); } }
    catch (e) { if (stamp === generation.current && !unavailable(e)) { setRuns([]); setError(e instanceof Error ? e.message : 'Preview thất bại.'); } }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function save() {
    if (flight.current || !snapshot) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      if (!pending.current) pending.current = { etag: snapshot.etag, key: createIdempotencyKey(), body: body() };
      const command = pending.current, ack = await automationScheduleSave(id, command.etag, command.body, command.key);
      if (stamp !== generation.current) return;
      if (!ack || ack.id !== id || !canonicalETag(ack.etag)) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      acknowledged.current = true; const value = await automationScheduleGet(id); if (stamp !== generation.current) return;
      accept(value); pending.current = null; acknowledged.current = false; setUncertain(false); setError('Schedule đã lưu. Lưu không tự Enable definition; scheduler vẫn kiểm tra authority và operator gates.');
    } catch (e) {
      if (stamp !== generation.current || unavailable(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (pending.current && (acknowledged.current || status === undefined || status === 0 || status === 408 || status >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict')) { setUncertain(true); setError('Chưa rõ kết quả. Retry giữ nguyên schedule/ETag/key; không tạo request mới.'); }
      else { pending.current = null; setUncertain(false); setError(status === 412 ? 'Revision đã thay đổi. Tải lại schedule trước khi lưu lại.' : e instanceof Error ? e.message : 'Save bị từ chối.'); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title="Schedule configuration" busy={busy} dirty={dirty || uncertain} onClose={() => { if (uncertain) setError('Retry cùng command trước khi đóng.'); else onClose(); }}>
    <p>Daily/Weekly/Cron theo IANA wall-clock; interval là elapsed UTC từ Start, không đổi do DST/restart. Missed runs Skip; DST nonexistent Skip, repeated Once. Lịch local chỉ enqueue khi owner opt-in, definition Enabled và operator flags bật; session hết hạn/revoke sẽ Skip.</p>
    {busy && <p role="status">Đang xử lý…</p>}{error && <p role="alert">{error}</p>}
    {snapshot && <p>Schedule version {snapshot.scheduleVersion} · Definition version {snapshot.definitionVersion}{snapshot.bindingStale ? ' · Binding stale: Save để bind lại version hiện tại.' : ''}</p>}
    <fieldset disabled={busy || uncertain || !snapshot}>
      <label><input type="checkbox" checked={form.enabled} disabled={!snapshot?.executionAvailable && !form.enabled} onChange={e => change({ enabled: e.target.checked })} />Opt-in local scheduled runs</label>
      <label>Frequency<select value={form.frequency} onChange={e => change({ frequency: e.target.value as AutomationScheduleConfig['frequency'], cronExpression: null, intervalMinutes: null })}><option>Daily</option><option>Weekly</option><option>Cron</option><option>Interval</option></select></label>
      <label>IANA timezone<input value={form.timeZoneId} onChange={e => change({ timeZoneId: e.target.value })} placeholder="Asia/Ho_Chi_Minh" maxLength={100} /></label>
      {(form.frequency === 'Daily' || form.frequency === 'Weekly') && <><label>Hour<input type="number" min={0} max={23} value={form.hour} onChange={e => change({ hour: Number(e.target.value) })} /></label>
      <label>Minute<input type="number" min={0} max={59} value={form.minute} onChange={e => change({ minute: Number(e.target.value) })} /></label></>}
      {form.frequency === 'Cron' && <><label>Cron 5-field<input value={form.cronExpression ?? ''} maxLength={256} placeholder="*/15 9-17 * * 1-5" onChange={e => change({ cronExpression: e.target.value })} /></label><p>minute hour day-of-month month day-of-week. Numeric *, lists, ranges, /step trên * hoặc range; Sunday 0/7. DOM/DOW đều restricted dùng OR. Không seconds/year, names, macros, L/W/#. Preview tối đa 5 occurrences trong 32 năm.</p></>}
      {form.frequency === 'Interval' && <label>Interval minutes (1–1440)<input type="number" min={1} max={1440} step={1} value={form.intervalMinutes ?? ''} onChange={e => change({ intervalMinutes: e.target.value ? Number(e.target.value) : null })} /></label>}
      {form.frequency === 'Weekly' && <div>{['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((day, index) => <label key={day}><input type="checkbox" checked={(form.weekdays & (1 << index)) !== 0} onChange={() => change({ weekdays: form.weekdays ^ (1 << index) })} />{day}</label>)}</div>}
      <label>Start ISO timestamp<input value={form.startAt} onChange={e => change({ startAt: e.target.value })} placeholder="2026-10-12T00:00:00+07:00" /></label>
      <label>End ISO timestamp (optional)<input value={form.endAt ?? ''} onChange={e => change({ endAt: e.target.value || null })} /></label>
    </fieldset>
    <p>Operator execution gate: {snapshot?.executionAvailable ? 'Available; còn cần owner opt-in và definition Enabled' : 'Inactive'}</p>
    <ol aria-label="Next scheduled occurrences">{runs.map(run => <li key={run}>{run} · {new Date(run).toLocaleString()}</li>)}</ol>
    <section aria-label="Recent 5 occurrence evaluations"><h3>5 occurrence evaluations gần nhất</h3>{snapshot?.recentOccurrences?.map(item => <p key={item.dueAt + ':' + item.scheduleVersion}>{item.dueAt} · schedule version {item.scheduleVersion} · {item.state} · {item.code}{item.runId ? ' · run ' + item.runId : ''}</p>)}</section>
    <button disabled={busy || uncertain || !snapshot} onClick={() => void preview()}>Preview 5 next runs</button>
    <button disabled={busy || !snapshot} onClick={() => void save()}>{uncertain ? 'Retry cùng command' : 'Save schedule'}</button>
    <button disabled={busy || uncertain} onClick={() => void load()}>Tải lại schedule</button>
  </ResourceFormDialog>;
}
