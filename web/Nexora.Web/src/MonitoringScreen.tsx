import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { monitorCapabilities, monitorCreate, monitorEnabled, monitorGet, monitorList, monitorUpdate, type MonitorMetadata, type MonitorRecord } from './monitorApi';

type Draft = { title: string; target: string; interval: string; expected: string; enabled: '' | 'true' | 'false' };
const empty: Draft = { title: '', target: '', interval: '', expected: '', enabled: '' };
const draftOf = (item: MonitorRecord): Draft => ({ title: item.metadata.title, target: item.metadata.target, interval: String(item.metadata.intervalSeconds), expected: item.metadata.expectedStatus === null ? '' : String(item.metadata.expectedStatus), enabled: String(item.enabled) as 'true' | 'false' });
const message = (error: unknown) => error instanceof Error ? error.message : 'Không thể hoàn tất thao tác.';

export function MonitoringScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const prefix = useId(); const epoch = useRef(0), mounted = useRef(true), flight = useRef(false);
  const writeKey = useRef(crypto.randomUUID());
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [items, setItems] = useState<MonitorRecord[]>([]);
  const [filter, setFilter] = useState({ query: '', state: '' }), [query, setQuery] = useState(''), [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ item?: MonitorRecord; baseline: string } | null>(null), [draft, setDraft] = useState<Draft>(empty);
  const [formError, setFormError] = useState<string | null>(null), [uncertain, setUncertain] = useState(false);
  const [conflict, setConflict] = useState<MonitorRecord | null>(null), [conflictLocked, setConflictLocked] = useState(false);
  const [pending, setPending] = useState<{ item: MonitorRecord; enabled: boolean; key: string; uncertain?: boolean } | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const currentOperations = useRef({ editor, pending }); currentOperations.current = { editor, pending };
  const pendingUncertain = pending?.uncertain === true;
  const clear = useCallback(() => { setItems([]); setCursor(null); setEditor(null); setDraft(empty); setPending(null); setConflict(null); setConflictLocked(false); setUncertain(false); setFormError(null); setLeave(null); setQuery(''); }, []);
  const fail = useCallback((e: unknown) => {
    const status = (e as { status?: number }).status; setError(message(e));
    if (status === 401 || status === 403 || status === 404) { epoch.current++; clear(); if (status !== 404) setCaps({}); setLoading(false); if (status === 401) onAuthLost(); }
  }, [clear, onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++epoch.current; setLoading(true); setError(null);
    try {
      const allowed = await monitorCapabilities();
      if (!mounted.current || stamp !== epoch.current) return;
      setCaps(allowed);
      const editing = currentOperations.current.editor, confirming = currentOperations.current.pending;
      if ((editing && (!allowed[editing.item ? 'monitoring.monitor.update' : 'monitoring.monitor.create'] || (editing.item && !allowed['monitoring.monitor.read']))) || (confirming && !allowed[confirming.enabled ? 'monitoring.monitor.resume' : 'monitoring.monitor.pause'])) clear();
      const page = allowed['monitoring.monitor.read'] ? await monitorList(filter.query, filter.state) : null;
      if (!mounted.current || stamp !== epoch.current) return;
      setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
      if (!allowed['monitoring.monitor.read']) clear();
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { if (mounted.current && stamp === epoch.current) setLoading(false); }
  }, [filter, clear, fail]);
  useEffect(() => { mounted.current = true; void load(); return () => { mounted.current = false; epoch.current++; }; }, [load]);
  const dirty = editor !== null && editor.baseline !== JSON.stringify(draft);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !busy && !uncertain && !pendingUncertain) return false; setLeave({ proceed }); return true; }), [dirty, busy, uncertain, pendingUncertain]);
  useEffect(() => { if (!dirty && !busy && !uncertain && !pendingUncertain) return; const prevent = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ''; }; window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent); }, [dirty, busy, uncertain, pendingUncertain]);
  const can = (name: string) => caps['monitoring.monitor.' + name] === true;
  function begin(item?: MonitorRecord) {
    const next = item ? draftOf(item) : empty; setEditor({ item, baseline: JSON.stringify(next) }); setDraft(next);
    setConflict(null); setConflictLocked(false); setFormError(null); setUncertain(false); writeKey.current = crypto.randomUUID();
  }
  function change(field: keyof Draft, value: string) {
    if (uncertain || flight.current) return; writeKey.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: value }));
  }
  async function persist() {
    if (!editor || flight.current || conflictLocked || !can(editor.item ? 'update' : 'create') || (editor.item && !can('read'))) return false;
    const interval = Number(draft.interval), expected = draft.expected === '' ? null : Number(draft.expected);
    if (!draft.interval || !Number.isInteger(interval) || interval < 1 || interval > 2147483647 || (expected !== null && (!Number.isInteger(expected) || expected < 100 || expected > 599)) || (!editor.item && draft.enabled === '')) { setFormError('Enter an explicit positive interval, optional status100–599, and initial enabled choice.'); return false; }
    const metadata: MonitorMetadata = { schemaVersion: 1, title: draft.title, kind: 'Http', target: draft.target, intervalSeconds: interval, expectedStatus: expected };
    const stamp = epoch.current; flight.current = true; setBusy(true); setFormError(null);
    try {
      if (editor.item) await monitorUpdate(editor.item, metadata, writeKey.current); else await monitorCreate(metadata, draft.enabled === 'true', writeKey.current);
      if (!mounted.current || stamp !== epoch.current) return false;
      setEditor(null); setDraft(empty); setUncertain(false); setConflict(null); await load(); return true;
    } catch (e) {
      if (!mounted.current || stamp !== epoch.current) return false;
      setFormError(message(e)); const status = (e as { status?: number }).status;
      if (status === 0 || status === 503 || (status === 409 && (e as { code?: string }).code === 'RequestInProgress')) setUncertain(true);
      if (status === 412 && editor.item) { setUncertain(false); setConflictLocked(true); try { const current = await monitorGet(editor.item.id); if (mounted.current && stamp === epoch.current) setConflict(current); } catch (readError) { if (mounted.current && stamp === epoch.current) fail(readError); } }
      if (status === 401 || status === 403 || status === 404) fail(e); return false;
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function recover(reapply: boolean) {
    if (!editor?.item || flight.current || !can('read') || !can('update')) return; const stamp = epoch.current; flight.current = true; setBusy(true);
    try {
      const current = await monitorGet(editor.item.id); if (!mounted.current || stamp !== epoch.current) return;
      if (reapply && current.etag !== conflict?.etag) { setConflict(current); setFormError('Revision changed again. Compare the new current version before reapplying.'); return; }
      const next = draftOf(current); setEditor({ item: current, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(null); setConflictLocked(false); setUncertain(false); writeKey.current = crypto.randomUUID(); setFormError(reapply ? 'Draft retained. Review and choose Save to apply it to the current version.' : null);
    } catch (e) { if (mounted.current && stamp === epoch.current) { if ((e as { status?: number }).status === 404) { setEditor(null); setDraft(empty); } fail(e); } }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function commit() {
    if (!pending || flight.current || !can(pending.enabled ? 'resume' : 'pause')) return false; const stamp = epoch.current; flight.current = true; setBusy(true);
    try { await monitorEnabled(pending.item, pending.enabled, pending.key); if (!mounted.current || stamp !== epoch.current) return false; setPending(null); await load(); return true as const; }
    catch (e) { if (!mounted.current || stamp !== epoch.current) return false; const status = (e as { status?: number }).status;
      if (status === 0 || status === 503 || (status === 409 && (e as { code?: string }).code === 'RequestInProgress')) setPending(old => old ? { ...old, uncertain: true } : null);
      if (status === 412) { setPending(null); await load(); } fail(e); return { error: message(e) }; }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  const blocked = loading || busy;
  return <section className="content-section monitoring-screen" aria-labelledby={`${prefix}-title`}>
    <div className="content-heading"><div><p className="eyebrow">FX36 / MONITORING</p><h1 id={`${prefix}-title`}>Monitoring</h1><p>Private HTTP configuration. Network checks and observations are unavailable in this local slice.</p></div><button className="secondary-button" disabled={blocked || editor !== null || pending !== null} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    {can('create') && <button className="primary-button" disabled={blocked || editor !== null} onClick={() => begin()}>New Monitor</button>}
    {can('read') && <><form className="resource-form" onSubmit={e => { e.preventDefault(); setFilter(old => ({ ...old, query })); }}>
      <label htmlFor={`${prefix}-query`}>Title search</label><input id={`${prefix}-query`} maxLength={200} value={query} disabled={blocked} onChange={e => setQuery(e.target.value)} /><button disabled={blocked}>Search Monitor</button>
      <label htmlFor={`${prefix}-state`}>State</label><select id={`${prefix}-state`} value={filter.state} disabled={blocked} onChange={e => setFilter(old => ({ ...old, state: e.target.value }))}><option value="">All states</option><option>Unknown</option><option>Paused</option></select>
      <label htmlFor={`${prefix}-kind`}>Type</label><select id={`${prefix}-kind`} value="Http" disabled><option>Http</option></select>
      <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ query: '', state: '' }); }}>Clear filters</button>
    </form>
    {!loading && !error && items.length === 0 && <p>No saved configuration matches this filter.</p>}
    <div className="resource-cards">{items.map(item => <article key={item.id} className="resource-card"><div><h2>{item.metadata.title}</h2><p>{item.state} · Http · No observations</p><dl><div><dt>Target</dt><dd>{item.metadata.target}</dd></div><div><dt>Interval seconds</dt><dd>{item.metadata.intervalSeconds}</dd></div><div><dt>Expected status</dt><dd>{item.metadata.expectedStatus ?? 'No explicit rule'}</dd></div></dl></div><div className="resource-actions">
      {can('update') && <button disabled={blocked} onClick={() => begin(item)}>Edit Monitor</button>}
      {can(item.enabled ? 'pause' : 'resume') && <button disabled={blocked} onClick={() => setPending({ item, enabled: !item.enabled, key: crypto.randomUUID() })}>{item.enabled ? 'Pause Monitor' : 'Resume Monitor'}</button>}
    </div></article>)}</div>
    {cursor && <button disabled={blocked} onClick={async () => { if (flight.current) return; flight.current = true; const stamp = epoch.current; setLoading(true); try { const next = await monitorList(filter.query, filter.state, cursor); if (mounted.current && stamp === epoch.current) { setItems(old => [...old, ...next.items]); setCursor(next.nextCursor); } } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); } finally { flight.current = false; if (mounted.current && stamp === epoch.current) setLoading(false); } }}>Load more Monitor items</button>}</>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.item ? 'Edit Monitor' : 'New Monitor'} busy={busy} dirty={dirty || uncertain} onClose={() => { setEditor(null); setDraft(empty); setConflict(null); setConflictLocked(false); setUncertain(false); void load(); }}>
      <form className="resource-form" onSubmit={(e: FormEvent) => { e.preventDefault(); void persist(); }}>{formError && <p role="alert">{formError}</p>}
        {uncertain && <p role="status">The save outcome is unknown. Retry the same request to recover its acknowledgement; discarding may leave a saved server record.</p>}
        {conflictLocked && <div><p>Revision changed. Compare the current saved configuration before replacing or reapplying your draft.</p>{conflict && <div aria-label="Current Monitor version"><h3>Current version</h3><p>{conflict.state} · {conflict.etag}</p><dl><div><dt>Title</dt><dd>{conflict.metadata.title}</dd></div><div><dt>Target</dt><dd>{conflict.metadata.target}</dd></div><div><dt>Interval seconds</dt><dd>{conflict.metadata.intervalSeconds}</dd></div><div><dt>Expected status</dt><dd>{conflict.metadata.expectedStatus ?? 'No explicit rule'}</dd></div></dl></div>}<button type="button" disabled={busy} onClick={() => void recover(false)}>Reload current</button><button type="button" disabled={busy || conflict === null} onClick={() => void recover(true)}>Reapply draft</button></div>}
        <p>Use a public target URL without credentials, tokens, private query parameters or fragments. Format validation does not perform a network check.</p><fieldset className="monitoring-form-fields" disabled={busy || uncertain}><legend>HTTP configuration</legend>
          <label htmlFor={`${prefix}-form-kind`}>Type</label><select id={`${prefix}-form-kind`} value="Http" disabled><option>Http</option></select>
          <label htmlFor={`${prefix}-form-title`}>Title</label><input id={`${prefix}-form-title`} required maxLength={200} value={draft.title} onChange={e => change('title', e.target.value)} />
          <label htmlFor={`${prefix}-target`}>Target URL</label><input id={`${prefix}-target`} required maxLength={2048} type="url" value={draft.target} onChange={e => change('target', e.target.value)} />
          <label htmlFor={`${prefix}-interval`}>Interval seconds</label><input id={`${prefix}-interval`} required type="number" min={1} max={2147483647} step={1} value={draft.interval} onChange={e => change('interval', e.target.value)} />
          <label htmlFor={`${prefix}-expected`}>Expected HTTP status (optional)</label><input id={`${prefix}-expected`} type="number" min={100} max={599} step={1} value={draft.expected} onChange={e => change('expected', e.target.value)} />
          {!editor?.item && <><label htmlFor={`${prefix}-enabled`}>Initial preference</label><select id={`${prefix}-enabled`} required value={draft.enabled} onChange={e => change('enabled', e.target.value)}><option value="">Choose explicitly</option><option value="true">Enabled — no network execution</option><option value="false">Paused</option></select></>}
        </fieldset><button className="primary-button" disabled={busy || conflictLocked || !can(editor?.item ? 'update' : 'create') || (!!editor?.item && !can('read'))}>{uncertain ? 'Retry same save' : 'Save Monitor'}</button>
      </form>
    </ResourceFormDialog>
    {pending && leave === null && <ActionDialog title={pending.enabled ? 'Resume Monitor' : 'Pause Monitor'} description={<span>{pending.item.metadata.title}. Change only the saved preference; no probe or network request will run.{pending.uncertain && ' The outcome is unknown. Retry this exact request to recover its acknowledgement.'}</span>} confirmLabel={pending.enabled ? 'Resume Monitor' : 'Pause Monitor'} confirmDisabled={busy || !can(pending.enabled ? 'resume' : 'pause')} onClose={() => { if (flight.current) return; if (pending.uncertain) setLeave({ proceed: () => { setPending(null); void load(); } }); else setPending(null); }} onConfirm={commit} />}
    {leave && <ActionDialog title="Bỏ thay đổi chưa lưu?" description={uncertain || pendingUncertain ? 'The operation outcome is unknown. Discarding leaves any committed server change intact.' : 'Bản nháp Monitor chưa được lưu.'} confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => { if (!flight.current) setLeave(null); }} onConfirm={() => { if (flight.current) return false; const proceed = leave.proceed; setEditor(null); setDraft(empty); setConflict(null); setConflictLocked(false); setUncertain(false); setPending(null); setLeave(null); proceed(); return true; }}><button disabled={busy || conflictLocked || editor === null} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button></ActionDialog>}
  </section>;
}
