import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { exportCapabilities, exportContent, exportJob, exportJobs, exportPreview, requestExport, type ExportAck, type ExportFilter, type ExportJob, type ExportPreview } from './calendarExportApi';
const initial = (): ExportFilter => ({ schemaVersion: 1, sourceKinds: ['Manual'], manualStatuses: ['Scheduled'], taskStatuses: [], range: null, containmentMode: 'FullyContained' });
type Prepared = { filter: ExportFilter; preview: ExportPreview; key: string };
export function CalendarExportScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [jobs, setJobs] = useState<ExportJob[]>([]); const [cursor, setCursor] = useState<string | null>(null); const [state, setState] = useState('');
  const [filter, setFilter] = useState<ExportFilter>(initial); const [prepared, setPrepared] = useState<Prepared | null>(null); const [job, setJob] = useState<ExportJob | null>(null); const [ack, setAck] = useState<ExportAck | null>(null);
  const [editor, setEditor] = useState(false); const [confirm, setConfirm] = useState(false); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null); const [error, setError] = useState<string | null>(null); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const flight = useRef(false); const epoch = useRef(0); const baseline = useRef(''); const ackFilter = useRef<ExportFilter | null>(null);
  const can = (action: string) => caps['transfer.export.' + action] === true;
  const dirty = editor && JSON.stringify(filter) !== baseline.current || prepared !== null || busy;
  const clear = useCallback(() => { setCaps({}); setJobs([]); setCursor(null); setPrepared(null); setJob(null); setAck(null); ackFilter.current = null; setFilter(initial()); setEditor(false); setConfirm(false); setLeave(null); }, []);
  const fail = useCallback((value: unknown) => {
    setError(value instanceof Error ? value.message : 'Không thể hoàn tất export.'); const status = (value as { status?: number }).status;
    if (status === 401 || status === 403 || (value as { code?: string }).code === 'ModuleUnavailable') { epoch.current++; flight.current = false; clear(); setBusy(false); setLoading(false); if (status === 401) onAuthLost(); }
    else if (status === 404) { setJobs([]); setCursor(null); setJob(null); setAck(null); ackFilter.current = null; setPrepared(null); setConfirm(false); }
    else if (status === 409) { setPrepared(null); setConfirm(false); }
    else if (status === 410) { setJob(current => current ? { ...current, state: 'Expired' } : null); setAck(current => current ? { ...current, state: 'Expired' } : null); }
  }, [clear, onAuthLost]);
  const refresh = useCallback(async (stamp: number) => {
    const current = await exportCapabilities(); if (stamp !== epoch.current) return;
    const page = current['transfer.export.read'] ? await exportJobs(state) : { items: [], nextCursor: null }; if (stamp !== epoch.current) return;
    setCaps(current); setJobs(page.items); setCursor(page.nextCursor);
    const sourceAllowed = (selected: ExportFilter) => selected.sourceKinds.every(kind => current['source.' + kind] === true);
    // A readable source kind does not prove that a saved job's individual sources still exist.
    setJob(null); setAck(null); ackFilter.current = null;
    setPrepared(selected => selected && current['transfer.export.request'] && sourceAllowed(selected.filter) ? selected : null);
    if (ackFilter.current && !sourceAllowed(ackFilter.current)) { setAck(null); ackFilter.current = null; }
    if (!current['transfer.export.request']) { setPrepared(null); setEditor(false); setConfirm(false); }
    return current;
  }, [state]);
  const load = useCallback(async () => {
    if (flight.current) return; flight.current = true; const stamp = ++epoch.current; setLoading(true); setError(null);
    try { await refresh(stamp); } catch (value) { if (stamp === epoch.current) fail(value); }
    finally { if (stamp === epoch.current) { flight.current = false; setLoading(false); } }
  }, [refresh, fail]);
  useEffect(() => { void load(); }, [load]); useEffect(() => () => { epoch.current++; flight.current = false; }, []);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (dirty) { setLeave({ proceed }); return true; } return false; }), [dirty]);
  useEffect(() => { const protect = (event: BeforeUnloadEvent) => { if (dirty) { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect); }, [dirty]);
  async function run(work: (stamp: number) => Promise<void>) {
    if (flight.current) return false; flight.current = true; const stamp = ++epoch.current; setBusy(true); setError(null); let success = false;
    try { await work(stamp); success = stamp === epoch.current; } catch (value) { if (stamp === epoch.current) fail(value); }
    finally { if (stamp === epoch.current) { flight.current = false; setBusy(false); } } return success;
  }
  function change(next: ExportFilter) { setFilter(next); setPrepared(null); setAck(null); setConfirm(false); }
  function toggle(kind: string, checked: boolean) {
    change({ ...filter, sourceKinds: checked ? [...filter.sourceKinds, kind] : filter.sourceKinds.filter(x => x !== kind),
      manualStatuses: kind === 'Manual' ? checked ? ['Scheduled'] : [] : filter.manualStatuses,
      taskStatuses: kind === 'Task' ? checked ? ['NotStarted'] : [] : filter.taskStatuses });
  }
  async function preview(event: FormEvent) {
    event.preventDefault(); await run(async stamp => { const snapshot = structuredClone(filter); const result = await exportPreview(snapshot); if (stamp !== epoch.current) return; setPrepared({ filter: snapshot, preview: result, key: crypto.randomUUID() }); setEditor(false); setJob(null); setAck(null); });
  }
  async function generate() {
    if (!prepared) return false;
    return run(async stamp => {
      const result = await requestExport(prepared.filter, prepared.preview.previewToken, prepared.key); if (stamp !== epoch.current) return;
      const current = await refresh(stamp); if (stamp !== epoch.current || !current || !prepared.filter.sourceKinds.every(kind => current['source.' + kind])) return;
      if (current['transfer.export.read']) { const report = await exportJob(result.jobId); if (stamp !== epoch.current) return; setJob(report); }
      ackFilter.current = prepared.filter; setAck(result); setPrepared(null); setConfirm(false);
    });
  }
  async function download(id: string) {
    await run(async stamp => { const blob = await exportContent(id); if (stamp !== epoch.current) return; const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = 'calendar.ics'; link.click(); queueMicrotask(() => URL.revokeObjectURL(url)); });
  }
  return <section className="content-section calendar-export-screen" aria-labelledby="calendar-export-title">
    <div className="content-heading"><div><p className="eyebrow">FX10 / CALENDAR ICS</p><h1 id="calendar-export-title">Calendar ICS Export</h1><p>Chọn Manual Events, Task Events hoặc cả hai; preview không tạo file. Generate chỉ xuất dữ liệu đã lưu và được phép đọc.</p></div><div className="resource-actions"><button className="secondary-button" disabled={busy || loading} onClick={() => void load()}>Tải lại</button><button className="primary-button" disabled={busy || loading || !can('request')} onClick={() => { baseline.current = JSON.stringify(filter); setEditor(true); }}>Chọn phạm vi export</button></div></div>
    <p>ICS UTF-8 giữ instant/ngày all-day, trạng thái và metadata Event hỗ trợ. Không xuất reminder, history, audit, reason hay ID nội bộ. Download hết hạn sau 15 phút; quyền và nguồn được kiểm tra lại mỗi lần tải.</p>
    {loading && <p role="status">Đang tải operations được phép…</p>}{error && <p role="alert" className="feedback error">{error}</p>}
    <label className="field-group">Operation state<select value={state} disabled={busy || loading} onChange={e => setState(e.target.value)}><option value="">Tất cả</option><option>Ready</option><option>Expired</option></select></label>
    {!loading && can('read') && jobs.length === 0 && <p>Không có operation được phép trong bộ lọc này.</p>}
    <div className="resource-cards">{jobs.map(item => <article className="resource-card" key={item.id}><div><h3>ICS · {item.count} Events</h3><p>{item.state} · {item.timeZoneId} · {item.createdAt}</p></div><button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const current = await exportJob(item.id); if (stamp !== epoch.current) return; setJob(current); setAck(null); })}>Mở export report</button></article>)}</div>
    {cursor && <button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const page = await exportJobs(state, cursor); if (stamp !== epoch.current) return; setJobs(items => [...items, ...page.items]); setCursor(page.nextCursor); })}>Tải thêm exports</button>}
    {prepared && <article className="resource-detail-card" aria-label="Export preview"><h2>Export preview</h2><p role="status">{prepared.preview.count} Events · Zone {prepared.preview.timeZoneId} · Preview hết hạn {prepared.preview.expiresAt}</p><p>{prepared.filter.sourceKinds.join(', ')} · {prepared.filter.manualStatuses.concat(prepared.filter.taskStatuses).join(', ')} · {prepared.filter.range ? `${prepared.filter.range.start} — ${prepared.filter.range.end} (end exclusive)` : 'All selected Calendar'}</p><button className="primary-button" disabled={busy || loading || !can('request')} onClick={() => setConfirm(true)}>Generate export</button></article>}
    {(job || ack) && <article className="resource-detail-card" aria-label="Export report"><h2>Export report</h2><p role="status">{job?.state ?? ack?.state} · {job?.count ?? ack?.count} Events · Download đến {job?.expiresAt ?? ack?.expiresAt}</p>{job && <p>Zone {job.timeZoneId} · Snapshot {job.createdAt} · {job.filter.sourceKinds.join(', ')}</p>}<div className="resource-actions">{can('read') && <button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const current = await exportJob(job?.id ?? ack!.jobId); if (stamp !== epoch.current) return; setJob(current); })}>Làm mới export report</button>}<button className="primary-button" disabled={busy || loading || !can('download') || (job?.state ?? ack?.state) !== 'Ready'} onClick={() => void download(job?.id ?? ack!.jobId)}>Download ICS</button></div></article>}
    <ResourceFormDialog open={editor} title="Phạm vi export ICS" busy={busy} dirty={JSON.stringify(filter) !== baseline.current} onClose={() => { setFilter(JSON.parse(baseline.current) as ExportFilter); setEditor(false); }}><form className="resource-form calendar-export-form" onSubmit={e => void preview(e)}>
      <fieldset disabled={busy || loading}><legend>Source kinds</legend>{['Manual', 'Task'].map(kind => <label key={kind}><input type="checkbox" checked={filter.sourceKinds.includes(kind)} disabled={!caps['source.' + kind] && !filter.sourceKinds.includes(kind)} onChange={e => toggle(kind, e.target.checked)} />{kind === 'Manual' ? 'Manual Events' : 'Task Events'}</label>)}</fieldset>
      {filter.sourceKinds.includes('Manual') && <fieldset disabled={busy || loading}><legend>Manual statuses</legend>{['Scheduled', 'Completed', 'Canceled'].map(value => <label key={value}><input type="checkbox" checked={filter.manualStatuses.includes(value)} onChange={e => change({ ...filter, manualStatuses: e.target.checked ? [...filter.manualStatuses, value] : filter.manualStatuses.filter(x => x !== value) })} />{value}</label>)}</fieldset>}
      {filter.sourceKinds.includes('Task') && <fieldset disabled={busy || loading}><legend>Task statuses</legend>{['NotStarted', 'InProgress', 'Completed', 'Skipped'].map(value => <label key={value}><input type="checkbox" checked={filter.taskStatuses.includes(value)} onChange={e => change({ ...filter, taskStatuses: e.target.checked ? [...filter.taskStatuses, value] : filter.taskStatuses.filter(x => x !== value) })} />{value}</label>)}</fieldset>}
      <label>Range<select value={filter.range ? 'Custom' : 'All'} disabled={busy || loading} onChange={e => change({ ...filter, range: e.target.value === 'All' ? null : { start: '', end: '' } })}><option>All</option><option>Custom</option></select></label>
      {filter.range && <><label>Start date<input type="date" required disabled={busy || loading} value={filter.range.start} onChange={e => change({ ...filter, range: { ...filter.range!, start: e.target.value } })} /></label><label>End date (exclusive)<input type="date" required disabled={busy || loading} value={filter.range.end} onChange={e => change({ ...filter, range: { ...filter.range!, end: e.target.value } })} /></label><p>Chỉ lấy Event nằm hoàn toàn trong khoảng. All-day so ngày trực tiếp; giờ dùng timezone hiện tại của tài khoản. Midnight không resolve an toàn sẽ báo lỗi khi có Event timed.</p></>}
      {error && <p role="alert">{error}</p>}<button className="primary-button" type="submit" disabled={busy || loading || !filter.sourceKinds.length || filter.sourceKinds.includes('Manual') && !filter.manualStatuses.length || filter.sourceKinds.includes('Task') && !filter.taskStatuses.length}>Preview export</button>
    </form></ResourceFormDialog>
    {confirm && prepared && <ActionDialog title="Generate ICS export?" description={`Tạo snapshot ${prepared.preview.count} Events đã lưu. Bạn xác nhận phạm vi, trạng thái và các trường bị loại; file không có reminder và hết quyền tải sau 15 phút.`} confirmLabel="Xác nhận Generate" onConfirm={generate} onClose={() => setConfirm(false)} />}
    {leave && <ActionDialog title="Rời export đang chuẩn bị?" description="Bỏ draft và preview đang mở; operations đã lưu vẫn giữ nguyên." confirmLabel="Rời trang" cancelLabel="Tiếp tục export" onConfirm={() => { if (flight.current) return false; setPrepared(null); setEditor(false); setLeave(null); leave.proceed(); return true; }} onClose={() => setLeave(null)} />}
  </section>;
}
