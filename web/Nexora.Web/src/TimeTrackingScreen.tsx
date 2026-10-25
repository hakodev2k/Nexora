import { useCallback, useEffect, useRef, useState } from 'react';
import { ActionDialog } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { timeCapabilities, timeEntries, timeHistory, timeReport, timeSave, timeStart, timeStop, timeTimer, timeTransition, timePreviewPurge, timePurge, type TimePurgePreview, type TimeEntry, type TimeCorrection } from './timeApi';

const localInput = (instant: string) => { const d = new Date(instant); return new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16); };
const errorStatus = (e: unknown) => (e as { status?: number })?.status;
const uncertainSave = (e: unknown) => { const status = errorStatus(e); return status === undefined || status === 0 || status === 408 || status >= 500 || (status === 409 && (e as { code?: string }).code === 'RequestInProgress'); };
const safeError = (e: unknown) => (e as { title?: string })?.title ?? (e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.');

export function TimeTrackingScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({});
  const [items, setItems] = useState<TimeEntry[]>([]); const [timer, setTimer] = useState<TimeEntry | null>(null);
  const [report, setReport] = useState<Awaited<ReturnType<typeof timeReport>> | null>(null);
  const [cursor, setCursor] = useState<string | null>(null); const [trash, setTrash] = useState(false);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true); const [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<{ entry?: TimeEntry; timer: boolean } | null>(null);
  const [saveUnknown, setSaveUnknown] = useState(false); const [abandonSave, setAbandonSave] = useState(false);
  const saveCommand = useRef<(() => Promise<unknown>) | null>(null); const saving = useRef(false);
  const [description, setDescription] = useState(''); const [category, setCategory] = useState('');
  const [start, setStart] = useState(''); const [end, setEnd] = useState(''); const [overlap, setOverlap] = useState(false);
  const [pending, setPending] = useState<TimeEntry | null>(null);
  const [history, setHistory] = useState<{ entry: TimeEntry; items: TimeCorrection[]; cursor: string | null } | null>(null);
  const [purge, setPurge] = useState<TimePurgePreview | null>(null);
  const key = useRef(crypto.randomUUID()); const loadSequence = useRef(0); const historySequence = useRef(0); const purgeSequence = useRef(0); const formBaseline = useRef('');
  const formValue = () => JSON.stringify({ description, category, start, end, overlap });
  const selection = useRef({ editor, pending }); selection.current = { editor, pending };
  const clear = useCallback(() => { loadSequence.current++; historySequence.current++; purgeSequence.current++; saveCommand.current = null; setSaveUnknown(false); setAbandonSave(false); setLoading(false); setCursor(null); setDescription(''); setCategory(''); setStart(''); setEnd(''); setOverlap(false); setItems([]); setTimer(null); setReport(null); setHistory(null); setEditor(null); setPending(null); setPurge(null); setCaps({}); }, []);
  const fail = useCallback((e: unknown) => { setError(safeError(e)); if (errorStatus(e) === 401 || errorStatus(e) === 403) { clear(); if (errorStatus(e) === 401) onAuthLost(); } }, [clear, onAuthLost]);
  const load = useCallback(async () => {
    const sequence = ++loadSequence.current; setLoading(true); setError(null);
    try {
      const allowed = await timeCapabilities();
      if (sequence !== loadSequence.current) return;
      if (!allowed['time.entry.read'] || !allowed['time.entry.history']) { historySequence.current++; setHistory(null); }
      if (!allowed['time.entry.read'] || !allowed['time.entry.purge']) { purgeSequence.current++; setPurge(null); }
      if (selection.current.editor && (!allowed[selection.current.editor.timer ? 'time.timer.start' : selection.current.editor.entry ? 'time.entry.update' : 'time.entry.create'] || (selection.current.editor.entry && !allowed['time.entry.read']))) setEditor(null);
      if (selection.current.pending && (!allowed['time.entry.read'] || !allowed[selection.current.pending.status === 'Trash' ? 'time.entry.restore' : 'time.entry.trash'])) setPending(null);
      setCaps(allowed);
      if (!allowed['time.entry.read']) { setItems([]); setCursor(null); setEditor(null); setPending(null); setDescription(''); setCategory(''); setStart(''); setEnd(''); setOverlap(false); }
      if (!allowed['time.timer.read']) setTimer(null);
      if (!allowed['time.report.read']) setReport(null);
      const [page, running, totals] = await Promise.all([allowed['time.entry.read'] ? timeEntries(trash) : null, allowed['time.timer.read'] ? timeTimer() : null, allowed['time.report.read'] ? timeReport() : null]);
      if (sequence !== loadSequence.current) return;
      setCaps(allowed); setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null); setTimer(running); setReport(totals);
    } catch (e) { if (sequence === loadSequence.current) fail(e); } finally { if (sequence === loadSequence.current) setLoading(false); }
  }, [trash, fail]);
  useEffect(() => { void load(); return () => { loadSequence.current++; }; }, [load]);
  const can = (action: string) => caps[`time.${action}`] === true;
  function open(entry?: TimeEntry, isTimer = false) {
    if (saving.current || saveCommand.current) return;
    const a = localInput(entry?.startAt ?? new Date(Date.now() - 3600000).toISOString()), b = localInput(entry?.endAt ?? new Date().toISOString());
    setDescription(entry?.description ?? ''); setCategory(entry?.category ?? ''); setStart(a); setEnd(b);
    formBaseline.current = JSON.stringify({ description: entry?.description ?? '', category: entry?.category ?? '', start: a, end: b, overlap: false });
    setOverlap(false); key.current = crypto.randomUUID(); setEditor({ entry, timer: isTimer });
  }
  function changed() { key.current = crypto.randomUUID(); }
  async function save() {
    if (!editor || saving.current) return false;
    if (!saveCommand.current) {
      const a = new Date(start), b = new Date(end);
      if (!editor.timer && (!Number.isFinite(a.getTime()) || !Number.isFinite(b.getTime()) || b <= a)) { setError('Giờ kết thúc phải sau giờ bắt đầu.'); return false; }
      const submittedKey = key.current, submittedDescription = description || null, submittedCategory = category || null;
      const submittedEntry = editor.entry ? { ...editor.entry } : undefined;
      if (editor.timer) saveCommand.current = () => timeStart(submittedDescription, submittedCategory, submittedKey);
      else {
        const body = { startAt: a.toISOString(), endAt: b.toISOString(), description: submittedDescription, category: submittedCategory, confirmOverlap: overlap };
        saveCommand.current = () => timeSave(body, submittedKey, submittedEntry);
      }
    }
    saving.current = true;
    const sequence = loadSequence.current; setBusy(true); setError(null);
    try {
      await saveCommand.current();
      if (sequence !== loadSequence.current) return false;
      saveCommand.current = null; setSaveUnknown(false); setAbandonSave(false); setEditor(null); await load(); return true;
    } catch (e) {
      if (sequence === loadSequence.current) {
        const unknown = uncertainSave(e); setSaveUnknown(unknown);
        if (!unknown) { saveCommand.current = null; setAbandonSave(false); }
        fail(e);
      }
      return false;
    } finally { saving.current = false; setBusy(false); }
  }
  async function mutate(work: () => Promise<unknown>) { const sequence = loadSequence.current; setBusy(true); try { await work(); if (sequence !== loadSequence.current) return false; await load(); return true as const; } catch (e) { if (sequence === loadSequence.current) fail(e); return { error: safeError(e) }; } finally { setBusy(false); } }
  return <section aria-labelledby="time-title" className="content-panel">
    <div className="content-heading"><div><p className="eyebrow">FX18 / TIME TRACKING</p><h1 id="time-title">Time Tracking</h1><p>Thời gian cá nhân từ SQL. Nhập theo múi giờ trình duyệt; lưu UTC. Liên kết Task/Project chưa có trong phần này.</p></div><button className="secondary-button" disabled={loading || busy} onClick={() => void load()}>Tải lại</button></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Đang tải…</p>}
    {can('timer.read') && <div className="resource-card"><h2>Timer</h2>{timer ? <><p>{timer.description ?? 'Timer'} · Running · {new Date(timer.startAt).toLocaleString()} · {Math.floor(timer.durationMilliseconds / 1000)} giây tại lần tải</p><button disabled={!can('timer.stop') || loading || busy} onClick={() => void mutate(() => timeStop(timer, key.current))}>Stop timer</button></> : <p>Không có timer đang chạy.</p>}
      <button disabled={!can('timer.start') || !!timer || loading || busy} onClick={() => open(undefined, true)}>Start timer</button></div>}
    <button className="primary-button" disabled={!can('entry.create') || loading || busy} onClick={() => open()}>Manual entry</button>
    {can('entry.read') && <><label><input type="checkbox" checked={trash} onChange={e => setTrash(e.target.checked)} /> Thùng rác</label>
      {!loading && !error && items.length === 0 && <p>Chưa có entry trong chế độ này.</p>}
      {items.map(row => <article className="resource-card" key={row.id}><h2>{row.description ?? 'Time entry'}</h2><p>{row.status} · {new Date(row.startAt).toLocaleString()} → {row.endAt && new Date(row.endAt).toLocaleString()} · {Math.floor(row.durationMilliseconds / 1000)} giây · {row.category}</p>
        {row.status === 'Stopped' && <><button disabled={!can('entry.update') || loading || busy} onClick={() => open(row)}>Edit entry</button><button disabled={!can('timer.resume') || !!timer || loading || busy} onClick={() => { key.current = crypto.randomUUID(); void mutate(() => timeStart(row.description, row.category, key.current, row.id)); }}>Resume as new timer</button></>}
        <button disabled={!can(row.status === 'Trash' ? 'entry.restore' : 'entry.trash') || loading || busy} onClick={() => { key.current = crypto.randomUUID(); setPending(row); }}>{row.status === 'Trash' ? 'Restore' : 'Trash'}</button>
        {row.status === 'Trash' && <button disabled={!can('entry.purge') || loading || busy} onClick={async () => { const seq = loadSequence.current, selected = ++purgeSequence.current; try { const preview = await timePreviewPurge(row.id); if (seq !== loadSequence.current || selected !== purgeSequence.current) return; key.current = crypto.randomUUID(); setPurge(preview); } catch (e) { if (seq === loadSequence.current && selected === purgeSequence.current) fail(e); } }}>Delete permanently</button>}
        <button disabled={!can('entry.history') || loading || busy} onClick={async () => { const seq = loadSequence.current, selected = ++historySequence.current; try { const page = await timeHistory(row.id); if (seq !== loadSequence.current || selected !== historySequence.current) return; setHistory({ entry: row, items: page.items, cursor: page.nextCursor }); } catch (e) { if (seq === loadSequence.current && selected === historySequence.current) fail(e); } }}>History</button>
      </article>)}
      {cursor && <button disabled={loading || busy} onClick={async () => { setLoading(true); const seq = loadSequence.current; try { const next = await timeEntries(trash, cursor); if (seq !== loadSequence.current) return; setItems(old => [...old, ...next.items]); setCursor(next.nextCursor); } catch (e) { if (seq === loadSequence.current) fail(e); } finally { if (seq === loadSequence.current) setLoading(false); } }}>Load more entries</button>}
    </>}
    {report && <div><h2>Gross duration</h2><p>{Math.floor(report.grossDurationMilliseconds / 1000)} giây · {report.entryCount} entries</p>{report.hasOverlaps && <p role="status">Có overlap. Tổng là gross duration, không phải net time.</p>}</div>}
    {editor && <ResourceFormDialog open title={editor.timer ? 'Start timer' : editor.entry ? 'Edit entry' : 'Manual entry'} busy={busy} closeBlocked={saveUnknown} dirty={formValue() !== formBaseline.current} onClose={() => setEditor(null)}><form className="resource-form" onSubmit={event => { event.preventDefault(); void save(); }} noValidate><p>Dữ liệu chỉ được lưu sau khi SQL xác nhận.</p>{error && <p role="alert">{error}</p>}
      {saveUnknown && <div role="alert"><p>Chưa xác định được kết quả lưu. Nội dung đã được giữ nguyên để thử lại cùng thao tác, tránh tạo bản ghi trùng.</p><p>Nếu dừng đối chiếu, thao tác trước vẫn có thể đã lưu. Kiểm tra timer và danh sách trước khi tạo lại.</p>
        {!abandonSave ? <button type="button" disabled={busy} onClick={() => setAbandonSave(true)}>Dừng đối chiếu…</button> : <div className="dialog-actions"><button type="button" disabled={busy} onClick={() => setAbandonSave(false)}>Tiếp tục đối chiếu</button><button type="button" className="danger-button" disabled={busy} onClick={() => { saveCommand.current = null; setSaveUnknown(false); setAbandonSave(false); setEditor(null); setDescription(''); setCategory(''); setStart(''); setEnd(''); setOverlap(false); void load(); }}>Tôi hiểu, kiểm tra dữ liệu đã lưu</button></div>}
      </div>}
      <fieldset className="resource-dialog-fields" disabled={busy || saveUnknown}>
      <label htmlFor="time-entry-description">Description</label><textarea id="time-entry-description" maxLength={2000} value={description} onChange={e => { changed(); setDescription(e.target.value); }} />
      <label htmlFor="time-entry-category">Category</label><input id="time-entry-category" maxLength={200} value={category} onChange={e => { changed(); setCategory(e.target.value); }} />
      {!editor.timer && <><label htmlFor="time-entry-start">Start (múi giờ trình duyệt)</label><input id="time-entry-start" type="datetime-local" value={start} onChange={e => { changed(); setStart(e.target.value); }} /><label htmlFor="time-entry-end">End (múi giờ trình duyệt)</label><input id="time-entry-end" type="datetime-local" value={end} onChange={e => { changed(); setEnd(e.target.value); }} /><label><input type="checkbox" checked={overlap} onChange={e => { changed(); setOverlap(e.target.checked); }} /> Tôi xác nhận nếu entry này overlap và được cộng vào gross duration.</label></>}
      </fieldset><button type="submit" disabled={busy}>{saveUnknown ? 'Thử lại đúng thao tác' : 'Save'}</button></form>
    </ResourceFormDialog>}
    {pending && <ActionDialog title={pending.status === 'Trash' ? 'Restore entry' : 'Trash entry'} description={<span>{pending.description ?? 'Time entry'} · {new Date(pending.startAt).toLocaleString()}. Trash giữ dữ liệu và lịch sử.</span>} confirmLabel={pending.status === 'Trash' ? 'Restore' : 'Trash'} onConfirm={() => mutate(() => timeTransition(pending, pending.status === 'Trash', key.current))} onClose={() => setPending(null)} />}
    {purge && <ActionDialog title="Delete entry permanently" description={<span>Entry {purge.entryId} và {purge.correctionCount} corrections sẽ bị xóa vĩnh viễn. Không thể hoàn tác.{purge.hasConversionPin && ' Focus conversion đang giữ reference; không thể purge.'}</span>} confirmLabel="Delete permanently" confirmDisabled={purge.hasConversionPin} onClose={() => { purgeSequence.current++; setPurge(null); }} onConfirm={() => purge.hasConversionPin ? { error: 'Focus conversion still references this entry.' } : mutate(() => timePurge(purge, key.current))} />}
    {history && <ActionDialog title="Entry history" description={history.entry.description ?? 'Time entry'} confirmLabel="Close" tone="primary" onConfirm={() => true} onClose={() => { historySequence.current++; setHistory(null); }}>{history.items.length === 0 ? <p>Chưa có correction.</p> : history.items.map(h => <p key={h.id}>{new Date(h.at).toLocaleString()} · {h.action} · trước: {h.before.description} · {h.before.startAt} → {h.before.endAt}</p>)}{history.cursor && <button onClick={async () => { const seq = loadSequence.current, selected = historySequence.current; try { const next = await timeHistory(history.entry.id, history.cursor!); if (seq !== loadSequence.current || selected !== historySequence.current) return; setHistory({ ...history, items: [...history.items, ...next.items], cursor: next.nextCursor }); } catch (e) { if (seq === loadSequence.current && selected === historySequence.current) fail(e); } }}>Load more history</button>}</ActionDialog>}
  </section>;
}
