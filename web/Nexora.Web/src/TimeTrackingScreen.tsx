import { useCallback, useEffect, useRef, useState } from 'react';
import { ActionDialog } from './App';
import { timeCapabilities, timeEntries, timeHistory, timeReport, timeSave, timeStart, timeStop, timeTimer, timeTransition, timePreviewPurge, timePurge, type TimePurgePreview, type TimeEntry, type TimeCorrection } from './timeApi';

const localInput = (instant: string) => { const d = new Date(instant); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16); };
const errorStatus = (e: unknown) => (e as { status?: number })?.status;
const safeError = (e: unknown) => (e as { title?: string })?.title ?? (e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.');

export function TimeTrackingScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({});
  const [items, setItems] = useState<TimeEntry[]>([]); const [timer, setTimer] = useState<TimeEntry | null>(null);
  const [report, setReport] = useState<Awaited<ReturnType<typeof timeReport>> | null>(null);
  const [cursor, setCursor] = useState<string | null>(null); const [trash, setTrash] = useState(false);
  const [loading, setLoading] = useState(true); const [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ entry?: TimeEntry; timer: boolean } | null>(null);
  const [description, setDescription] = useState(''); const [category, setCategory] = useState('');
  const [start, setStart] = useState(''); const [end, setEnd] = useState(''); const [overlap, setOverlap] = useState(false);
  const [pending, setPending] = useState<TimeEntry | null>(null);
  const [history, setHistory] = useState<{ entry: TimeEntry; items: TimeCorrection[]; cursor: string | null } | null>(null);
  const [purge, setPurge] = useState<TimePurgePreview | null>(null);
  const key = useRef(crypto.randomUUID()); const loadSequence = useRef(0);
  const clear = useCallback(() => { setItems([]); setTimer(null); setReport(null); setHistory(null); setEditor(null); setPending(null); setPurge(null); setCaps({}); }, []);
  const fail = useCallback((e: unknown) => { setError(safeError(e)); if (errorStatus(e) === 401 || errorStatus(e) === 403) { clear(); if (errorStatus(e) === 401) onAuthLost(); } }, [clear, onAuthLost]);
  const load = useCallback(async () => {
    const sequence = ++loadSequence.current; setLoading(true); setError(null);
    try {
      const allowed = await timeCapabilities();
      const [page, running, totals] = await Promise.all([allowed['time.entry.read'] ? timeEntries(trash) : null, allowed['time.timer.read'] ? timeTimer() : null, allowed['time.report.read'] ? timeReport() : null]);
      if (sequence !== loadSequence.current) return;
      setCaps(allowed); setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null); setTimer(running); setReport(totals);
    } catch (e) { if (sequence === loadSequence.current) fail(e); } finally { if (sequence === loadSequence.current) setLoading(false); }
  }, [trash, fail]);
  useEffect(() => { void load(); return () => { loadSequence.current++; }; }, [load]);
  const can = (action: string) => caps[`time.${action}`] === true;
  function open(entry?: TimeEntry, isTimer = false) {
    setDescription(entry?.description ?? ''); setCategory(entry?.category ?? '');
    setStart(localInput(entry?.startAt ?? new Date(Date.now() - 3600000).toISOString())); setEnd(localInput(entry?.endAt ?? new Date().toISOString()));
    setOverlap(false); key.current = crypto.randomUUID(); setEditor({ entry, timer: isTimer });
  }
  function changed() { key.current = crypto.randomUUID(); }
  async function save() {
    if (!editor) return false;
    const a = new Date(start), b = new Date(end);
    if (!editor.timer && (!Number.isFinite(a.getTime()) || !Number.isFinite(b.getTime()) || b <= a)) return { error: 'Giờ kết thúc phải sau giờ bắt đầu.' };
    try {
      if (editor.timer) await timeStart(description || null, category || null, key.current);
      else await timeSave({ startAt: a.toISOString(), endAt: b.toISOString(), description: description || null, category: category || null, confirmOverlap: overlap }, key.current, editor.entry);
      await load(); return true;
    } catch (e) { if ([401, 403].includes(errorStatus(e) ?? 0)) fail(e); return { error: safeError(e) }; }
  }
  async function mutate(work: () => Promise<unknown>) { try { await work(); await load(); return true as const; } catch (e) { fail(e); return { error: safeError(e) }; } }
  return <section aria-labelledby="time-title" className="content-panel">
    <div className="content-heading"><div><p className="eyebrow">FX18 / TIME TRACKING</p><h1 id="time-title">Time Tracking</h1><p>Thời gian cá nhân từ SQL. Nhập theo múi giờ trình duyệt; lưu UTC. Liên kết Task/Project chưa có trong phần này.</p></div><button className="secondary-button" disabled={loading} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    {can('timer.read') && <div className="resource-card"><h2>Timer</h2>{timer ? <><p>{timer.description ?? 'Timer'} · Running · {new Date(timer.startAt).toLocaleString()} · {Math.floor(timer.durationMilliseconds / 1000)} giây tại lần tải</p><button disabled={!can('timer.stop') || loading} onClick={() => void mutate(() => timeStop(timer, key.current))}>Stop timer</button></> : <p>Không có timer đang chạy.</p>}
      <button disabled={!can('timer.start') || !!timer || loading} onClick={() => open(undefined, true)}>Start timer</button></div>}
    <button className="primary-button" disabled={!can('entry.create') || loading} onClick={() => open()}>Manual entry</button>
    {can('entry.read') && <><label><input type="checkbox" checked={trash} onChange={e => setTrash(e.target.checked)} /> Thùng rác</label>
      {!loading && !error && items.length === 0 && <p>Chưa có entry trong chế độ này.</p>}
      {items.map(row => <article className="resource-card" key={row.id}><h2>{row.description ?? 'Time entry'}</h2><p>{row.status} · {new Date(row.startAt).toLocaleString()} → {row.endAt && new Date(row.endAt).toLocaleString()} · {Math.floor(row.durationMilliseconds / 1000)} giây · {row.category}</p>
        {row.status === 'Stopped' && <><button disabled={!can('entry.update') || loading} onClick={() => open(row)}>Edit entry</button><button disabled={!can('timer.resume') || !!timer || loading} onClick={() => { key.current = crypto.randomUUID(); void mutate(() => timeStart(row.description, row.category, key.current, row.id)); }}>Resume as new timer</button></>}
        <button disabled={!can(row.status === 'Trash' ? 'entry.restore' : 'entry.trash') || loading} onClick={() => { key.current = crypto.randomUUID(); setPending(row); }}>{row.status === 'Trash' ? 'Restore' : 'Trash'}</button>
        {row.status === 'Trash' && <button disabled={!can('entry.purge') || loading} onClick={async () => { try { const preview = await timePreviewPurge(row.id); key.current = crypto.randomUUID(); setPurge(preview); } catch (e) { fail(e); } }}>Delete permanently</button>}
        <button disabled={!can('entry.history') || loading} onClick={async () => { try { const page = await timeHistory(row.id); setHistory({ entry: row, items: page.items, cursor: page.nextCursor }); } catch (e) { fail(e); } }}>History</button>
      </article>)}
      {cursor && <button disabled={loading} onClick={async () => { setLoading(true); try { const next = await timeEntries(trash, cursor); setItems(old => [...old, ...next.items]); setCursor(next.nextCursor); } catch (e) { fail(e); } finally { setLoading(false); } }}>Load more entries</button>}
    </>}
    {report && <div><h2>Gross duration</h2><p>{Math.floor(report.grossDurationMilliseconds / 1000)} giây · {report.entryCount} entries</p>{report.hasOverlaps && <p role="status">Có overlap. Tổng là gross duration, không phải net time.</p>}</div>}
    {editor && <ActionDialog title={editor.timer ? 'Start timer' : editor.entry ? 'Edit entry' : 'Manual entry'} description="Dữ liệu chỉ được lưu sau khi SQL xác nhận." confirmLabel="Save" tone="primary" onConfirm={save} onClose={() => setEditor(null)}>
      <label htmlFor="time-entry-description">Description</label><textarea id="time-entry-description" maxLength={2000} value={description} onChange={e => { changed(); setDescription(e.target.value); }} />
      <label htmlFor="time-entry-category">Category</label><input id="time-entry-category" maxLength={200} value={category} onChange={e => { changed(); setCategory(e.target.value); }} />
      {!editor.timer && <><label htmlFor="time-entry-start">Start (múi giờ trình duyệt)</label><input id="time-entry-start" type="datetime-local" value={start} onChange={e => { changed(); setStart(e.target.value); }} /><label htmlFor="time-entry-end">End (múi giờ trình duyệt)</label><input id="time-entry-end" type="datetime-local" value={end} onChange={e => { changed(); setEnd(e.target.value); }} /><label><input type="checkbox" checked={overlap} onChange={e => { changed(); setOverlap(e.target.checked); }} /> Tôi xác nhận nếu entry này overlap và được cộng vào gross duration.</label></>}
    </ActionDialog>}
    {pending && <ActionDialog title={pending.status === 'Trash' ? 'Restore entry' : 'Trash entry'} description={<span>{pending.description ?? 'Time entry'} · {new Date(pending.startAt).toLocaleString()}. Trash giữ dữ liệu và lịch sử.</span>} confirmLabel={pending.status === 'Trash' ? 'Restore' : 'Trash'} onConfirm={() => mutate(() => timeTransition(pending, pending.status === 'Trash', key.current))} onClose={() => setPending(null)} />}
    {purge && <ActionDialog title="Delete entry permanently" description={<span>Entry {purge.entryId} và {purge.correctionCount} corrections sẽ bị xóa vĩnh viễn. Không thể hoàn tác.{purge.hasConversionPin && ' Focus conversion đang giữ reference; không thể purge.'}</span>} confirmLabel="Delete permanently" confirmDisabled={purge.hasConversionPin} onClose={() => setPurge(null)} onConfirm={() => purge.hasConversionPin ? { error: 'Focus conversion still references this entry.' } : mutate(() => timePurge(purge, key.current))} />}
    {history && <ActionDialog title="Entry history" description={history.entry.description ?? 'Time entry'} confirmLabel="Close" tone="primary" onConfirm={() => true} onClose={() => setHistory(null)}>{history.items.length === 0 ? <p>Chưa có correction.</p> : history.items.map(h => <p key={h.id}>{new Date(h.at).toLocaleString()} · {h.action} · trước: {h.before.description} · {h.before.startAt} → {h.before.endAt}</p>)}{history.cursor && <button onClick={async () => { try { const next = await timeHistory(history.entry.id, history.cursor!); setHistory({ ...history, items: [...history.items, ...next.items], cursor: next.nextCursor }); } catch (e) { fail(e); } }}>Load more history</button>}</ActionDialog>}
  </section>;
}
