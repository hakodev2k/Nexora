import { useCallback, useEffect, useRef, useState } from 'react';
import { ActionDialog } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { focusCapabilities, focusList, focusPreferences, focusSavePreferences, focusStart, focusTransition, focusRecordTime, type FocusPreferences, type FocusSession } from './focusApi';

export function FocusScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [items, setItems] = useState<FocusSession[]>([]);
  const [active, setActive] = useState<FocusSession | null>(null); const [prefs, setPrefs] = useState<FocusPreferences | null>(null);
  const [cursor, setCursor] = useState<string | null>(null); const [phase, setPhase] = useState('Focus');
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null);
  const [draft, setDraft] = useState<FocusPreferences | null>(null); const [cancel, setCancel] = useState<FocusSession | null>(null);
  const [record, setRecord] = useState<FocusSession | null>(null); const [confirmOverlap, setConfirmOverlap] = useState(false); const [conversion, setConversion] = useState<string | null>(null);
  const preferenceBaseline = useRef('');
  const key = useRef(crypto.randomUUID()); const revision = useRef(0); const startKey = useRef(crypto.randomUUID());
  const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';
  const fail = useCallback((e: unknown) => {
    setError(e instanceof Error ? e.message : 'Không thể tải Focus.');
    const status = (e as { status?: number })?.status;
    if (status === 401 || status === 403) { revision.current++; setLoading(false); setItems([]); setActive(null); setPrefs(null); setDraft(null); setCancel(null); setRecord(null); setConversion(null); setConfirmOverlap(false); setCursor(null); setCaps({}); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const load = useCallback(async () => {
    const seq = ++revision.current; setLoading(true); setError(null);
    try {
      const c = await focusCapabilities();
      if (seq !== revision.current) return;
      if (!c['focus.session.read']) { setDraft(null); setCancel(null); setRecord(null); setConversion(null); }
      if (!c['focus.preference.update']) setDraft(null);
      if (!c['focus.session.cancel']) setCancel(null);
      if (!c['focus.session.record_time']) { setRecord(null); setConversion(null); }
      setCaps(c);
      if (!c['focus.session.read']) { setItems([]); setCursor(null); setActive(null); setPrefs(null); }
      const [history, running, p] = c['focus.session.read'] ? await Promise.all([focusList(), focusList(true), focusPreferences()]) : [null, null, null];
      if (seq !== revision.current) return;
      setItems(history?.items ?? []); setCursor(history?.nextCursor ?? null); setActive(running?.items[0] ?? null); setPrefs(p);
    } catch (e) { if (seq === revision.current) fail(e); } finally { if (seq === revision.current) setLoading(false); }
  }, [fail]);
  useEffect(() => { void load(); return () => { revision.current++; }; }, [load]);
  useEffect(() => {
    if (!caps['focus.session.read']) return;
    let alive = true;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      const seq = revision.current;
      try {
        const page = await focusList(true);
        if (!alive || seq !== revision.current) return;
        setActive(page.items[0] ?? null);
        if (!page.items.length && active) await load();
      } catch (e) { if (alive && seq === revision.current) fail(e); }
      finally { if (alive) timer = setTimeout(() => void poll(), 5000); }
    }
    timer = setTimeout(() => void poll(), 5000);
    return () => { alive = false; clearTimeout(timer); };
  }, [caps, active, load, fail]);
  const can = (a: string) => caps[`focus.${a}`] === true;
  async function mutate(work: () => Promise<unknown>) { const seq = revision.current; setBusy(true); try { await work(); if (seq !== revision.current) return false; await load(); return true as const; } catch (e) { if (seq === revision.current) fail(e); return { error: message(e) }; } finally { setBusy(false); } }
  return <section className="content-panel" aria-labelledby="focus-title">
    <div className="content-heading"><div><p className="eyebrow">FX19 / FOCUS</p><h1 id="focus-title">Focus</h1><p>Một phase đang hoạt động. Thời gian do server tính; phase tiếp theo chỉ bắt đầu khi bạn chọn.</p></div><button disabled={loading || busy} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    {can('session.read') && <div className="resource-card"><h2>Active phase</h2>{active ? <><p>{active.phase} · {active.state} · {Math.ceil(active.remainingMilliseconds / 1000)} giây còn lại tại lần đồng bộ</p>
      {active.state === 'Running' && <button disabled={!can('session.pause') || busy} onClick={() => { key.current = crypto.randomUUID(); void mutate(() => focusTransition(active, 'pause', key.current)); }}>Pause</button>}
      {active.state === 'Paused' && <button disabled={!can('session.resume') || busy} onClick={() => { key.current = crypto.randomUUID(); void mutate(() => focusTransition(active, 'resume', key.current)); }}>Resume</button>}
      <button disabled={!can('session.cancel') || busy} onClick={() => { key.current = crypto.randomUUID(); setCancel(active); }}>Cancel phase</button>
    </> : <p>Chưa có phase đang hoạt động.</p>}</div>}
    <label>Phase<select value={phase} onChange={e => { startKey.current = crypto.randomUUID(); setPhase(e.target.value); }}><option value="Focus">Focus</option><option value="ShortBreak">Short break</option><option value="LongBreak">Long break</option></select></label>
    <button className="primary-button" disabled={!can('session.start') || !!active || loading || busy} onClick={() => void mutate(async () => { await focusStart(phase, startKey.current); startKey.current = crypto.randomUUID(); })}>Start phase</button>
    <button disabled={!can('preference.update') || !prefs || loading || busy} onClick={() => { key.current = crypto.randomUUID(); preferenceBaseline.current = JSON.stringify(prefs); setDraft(prefs); }}>Edit focus preferences</button>
    <p>Liên kết Task chưa có trong phần này. Email/Push completion hiển thị unavailable khi provider chưa cấu hình.</p>{conversion && <p role="status">Đã ghi Time Entry: {conversion}</p>}
    {can('session.read') && <><h2>Focus history</h2>{!loading && !error && !items.length && <p>Chưa có phiên tập trung.</p>}{items.map(item => <article key={item.id} className="resource-card"><h3>{item.phase} · {item.state}</h3><p>{new Date(item.startedAt).toLocaleString()} · {Math.floor(item.elapsedMilliseconds / 1000)} / {item.plannedSeconds} giây</p>{item.phase === 'Focus' && item.state === 'Completed' && <button disabled={!can('session.record_time') || busy} onClick={() => { key.current = crypto.randomUUID(); setConfirmOverlap(false); setRecord(item); }}>Save as Time Entry</button>}</article>)}{cursor && <button disabled={loading || busy} onClick={async () => { const seq = revision.current; setBusy(true); try { const next = await focusList(false, cursor); if (seq !== revision.current) return; setItems(old => [...old, ...next.items]); setCursor(next.nextCursor); } catch (e) { if (seq === revision.current) fail(e); } finally { setBusy(false); } }}>Load more sessions</button>}</>}
    {draft && <ResourceFormDialog open title="Focus preferences" busy={busy} dirty={JSON.stringify(draft) !== preferenceBaseline.current} onClose={() => setDraft(null)}>
      <form onSubmit={event => { event.preventDefault(); void mutate(() => focusSavePreferences(draft, key.current)).then(result => { if (result === true) setDraft(null); }); }}>
      <p>Thời lượng của phase đang chạy giữ nguyên. Các giá trị mới áp dụng cho lần Start tiếp theo.</p>
      {(['focusMinutes', 'shortBreakMinutes', 'longBreakMinutes', 'cycleLength'] as const).map((field, index) => <label key={field}>{['Focus minutes', 'Short break minutes', 'Long break minutes', 'Cycle length'][index]}<input disabled={busy} type="number" required min={1} max={[180, 60, 120, 12][index]} value={draft[field]} onChange={e => { key.current = crypto.randomUUID(); setDraft({ ...draft, [field]: Number(e.target.value) }); }} /></label>)}
      {error && <p role="alert">{error}</p>}<button type="submit" disabled={busy}>Save preferences</button>
      </form>
    </ResourceFormDialog>}
    {record && <ActionDialog title="Save as Time Entry" description="Ghi thời gian tập trung đã hoàn tất, loại thời gian Pause. Retry giữ một Entry cho mỗi phiên; break không được chuyển thành work time." confirmLabel="Record time" tone="primary" onClose={() => setRecord(null)} onConfirm={() => mutate(async () => { const seq = revision.current; const result = await focusRecordTime(record, confirmOverlap, key.current); if (seq === revision.current) setConversion(result.entryId); })}><label><input type="checkbox" checked={confirmOverlap} onChange={e => { key.current = crypto.randomUUID(); setConfirmOverlap(e.target.checked); }} /> Tôi xác nhận nếu thời gian này overlap và được cộng vào gross duration.</label></ActionDialog>}
    {cancel && <ActionDialog title="Cancel phase" description={<span>{cancel.phase} · bắt đầu {new Date(cancel.startedAt).toLocaleString()}. Phiên được giữ trong history.</span>} confirmLabel="Confirm cancel" onClose={() => setCancel(null)} onConfirm={() => mutate(() => focusTransition(cancel, 'cancel', key.current))} />}
  </section>;
}
