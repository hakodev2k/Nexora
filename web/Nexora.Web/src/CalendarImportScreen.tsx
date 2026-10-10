import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { apiFetch, completeFileUpload, initiateFileUpload, type FileRecord, type FileUploadSession } from './api';
const getFile = (id: string) => apiFetch<FileRecord>('/api/v1/files/' + id);
import { changeCalendarImport, importBatch, importBatches, importCapabilities, importRows, previewCalendarImport, type ImportBatch, type ImportRow } from './calendarImportApi';

export function CalendarImportScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}); const [batches, setBatches] = useState<ImportBatch[]>([]); const [cursor, setCursor] = useState<string | null>(null);
  const [state, setState] = useState(''); const [batch, setBatch] = useState<ImportBatch | null>(null); const [rows, setRows] = useState<ImportRow[]>([]); const [rowCursor, setRowCursor] = useState<number | null>(null); const [outcome, setOutcome] = useState('');
  const [editor, setEditor] = useState(false); const [selected, setSelected] = useState<File | null>(null); const [file, setFile] = useState<FileRecord | null>(null);
  const [pending, setPending] = useState<'commit' | 'cancel' | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const [error, setError] = useState<string | null>(null); const [busy, setBusy] = useState(false); const [loading, setLoading] = useState(true);
  const key = useRef(crypto.randomUUID()); const flight = useRef(false); const sequence = useRef(0);
  const prepared = useRef<{ selection: File; initiateKey: string; contentKey: string; session?: FileUploadSession; clean?: FileRecord } | null>(null);
  const can = (action: string) => caps['transfer.import.' + action] === true;
  const clearEditor = () => { setEditor(false); setSelected(null); prepared.current = null; setFile(null); };
  const fail = useCallback((e: unknown) => {
    setError(e instanceof Error ? e.message : 'Không thể hoàn tất import.'); const status = (e as { status?: number }).status;
    if (status === 401 || status === 403 || (e as { code?: string }).code === 'ModuleUnavailable') { sequence.current++; flight.current = false; setCaps({}); setBatches([]); setCursor(null); setBatch(null); setRows([]); setRowCursor(null); setSelected(null); setFile(null); prepared.current = null; setEditor(false); setPending(null); setLeave(null); setBusy(false); setLoading(false); if (status === 401) onAuthLost(); }
  }, [onAuthLost]);
  const refresh = useCallback(async (stamp: number) => {
    const current = await importCapabilities(); if (stamp !== sequence.current) return;
    const page = current['transfer.import.read'] ? await importBatches(state) : { items: [], nextCursor: null };
    if (stamp !== sequence.current) return;
    setCaps(current); setBatches(page.items); setCursor(page.nextCursor as string | null);
    if (!current['transfer.import.read']) { setBatch(null); setRows([]); setRowCursor(null); clearEditor(); setPending(null); }
  }, [state]);
  const load = useCallback(async () => {
    if (flight.current) return; flight.current = true;
    const stamp = ++sequence.current; setLoading(true); setError(null);
    try { await refresh(stamp); } catch (e) { if (stamp === sequence.current) fail(e); }
    finally { if (stamp === sequence.current) { flight.current = false; setLoading(false); } }
  }, [refresh, fail]);
  useEffect(() => { void load(); }, [load]);
  useEffect(() => () => { sequence.current++; flight.current = false; }, []);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (selected || batch?.state === 'PreviewReady') { setLeave({ proceed }); return true; } return false; }), [selected, batch]);
  useEffect(() => { const protect = (event: BeforeUnloadEvent) => { if (selected || batch?.state === 'PreviewReady') { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect); }, [selected, batch]);
  async function open(id: string, stamp: number, filter = '') {
    const current = await importBatch(id); if (stamp !== sequence.current) return;
    const page = await importRows(id, filter);
    if (stamp !== sequence.current) return;
    setBatch(current); setRows(page.items); setRowCursor(page.nextCursor as number | null); setOutcome(filter); key.current = crypto.randomUUID();
  }
  async function run(work: (stamp: number) => Promise<void>) {
    if (flight.current) return; flight.current = true;
    const stamp = ++sequence.current; setBusy(true); setError(null);
    try { await work(stamp); } catch (e) { if (stamp === sequence.current) fail(e); }
    finally { if (stamp === sequence.current) { flight.current = false; setBusy(false); } }
  }
  async function preview(event: FormEvent) {
    event.preventDefault(); if (!selected) return;
    await run(async stamp => {
      if (!selected.name.toLowerCase().endsWith('.ics') || selected.size < 1 || selected.size > 1048576) throw new Error('Chọn file .ics từ 1 byte đến 1 MiB.');
      const exactFile = new File([selected], selected.name, { type: 'text/calendar' });
      if (prepared.current?.selection !== selected) prepared.current = { selection: selected, initiateKey: crypto.randomUUID(), contentKey: crypto.randomUUID() };
      const source = prepared.current;
      source.session ??= await initiateFileUpload(exactFile.name, 'text/calendar', exactFile.size, source.initiateKey); if (stamp !== sequence.current) return;
      source.clean ??= await completeFileUpload(source.session, exactFile, source.contentKey); if (stamp !== sequence.current) return;
      const clean = source.clean; setFile(clean);
      const result = await previewCalendarImport(clean.id, clean.etag, key.current); if (stamp !== sequence.current) return; await open(result.batchId, stamp); if (stamp !== sequence.current) return; clearEditor(); await refresh(stamp);
    });
  }
  async function change() {
    if (!batch || !pending) return false;
    let success = false;
    await run(async stamp => { await changeCalendarImport(batch, pending, key.current); if (stamp !== sequence.current) return; await open(batch.id, stamp); if (stamp !== sequence.current) return; await refresh(stamp); if (stamp !== sequence.current) return; setPending(null); success = true; });
    return success;
  }
  return <section className="content-section calendar-import-screen" aria-labelledby="calendar-import-title">
    <div className="content-heading"><div><p className="eyebrow">FX10 / CALENDAR ICS</p><h1 id="calendar-import-title">Calendar ICS Import</h1><p>Import thủ công Event Scheduled. Preview không tạo Event. Chọn ICS Export để xuất Calendar; DOCX/MD và backup/restore chưa có trong phần này.</p></div><div className="resource-actions"><button className="secondary-button" disabled={busy || loading} onClick={() => void load()}>Tải lại</button><button className="primary-button" disabled={busy || loading || !can('preview') || !can('read')} onClick={() => { clearEditor(); key.current = crypto.randomUUID(); setEditor(true); }}>Chọn file ICS</button></div></div>
    <p>File UTF-8 đã scan; tối đa 1 MiB, 1.000 VEVENT. Recurrence, entry lỗi hoặc UID trùng được bỏ qua; reminder và source status không được nhập. Giữ ngày all-day và báo timezone cho giờ floating.</p>
    {!loading && !can('read') && <p role="status">Cần quyền import, đọc report và quyền Files/Calendar nguồn hiện tại.</p>}
    {error && <p className="feedback error" role="alert">{error} Nếu preview đổi, tạo preview mới trước khi commit.</p>}{loading && <p role="status">Đang tải các operation được phép…</p>}
    {can('read') && <><div className="field-group"><label htmlFor="import-state">Operation state</label><select id="import-state" value={state} disabled={busy || loading} onChange={e => setState(e.target.value)}><option value="">Tất cả</option>{['PreviewReady', 'Completed', 'Canceled'].map(value => <option key={value}>{value}</option>)}</select></div><div className="resource-cards">{batches.map(item => <article className="resource-card" key={item.id}><div><h2>ICS operation</h2><p>{item.state} · {item.timeZoneId}</p><p>Total {item.totalCount} · Initially valid {item.acceptedCount} · Skipped {item.skippedCount} · Applied {item.appliedCount}</p></div><button className="secondary-button" disabled={busy || loading} onClick={() => void run(stamp => open(item.id, stamp))}>Mở report</button></article>)}</div>{!loading && batches.length === 0 && <p>Không có operation trong bộ lọc này.</p>}{cursor && <button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const page = await importBatches(state, cursor); if (stamp !== sequence.current) return; setBatches(items => [...items, ...page.items]); setCursor(page.nextCursor as string | null); })}>Tải thêm operations</button>}</>}
    {batch && <article className="resource-detail-card" aria-label="ICS report"><h2>Import report</h2><p role="status">{batch.state} · Zone {batch.timeZoneId} · Total {batch.totalCount} · Initially valid {batch.acceptedCount} · Skipped {batch.skippedCount} · Applied {batch.appliedCount}</p>
      <div className="resource-actions"><button className="secondary-button" disabled={busy || loading} onClick={() => void run(stamp => open(batch.id, stamp, outcome))}>Làm mới report</button>{can('preview') && <button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const current = await getFile(batch.fileId); if (stamp !== sequence.current) return; setFile(current); const result = await previewCalendarImport(current.id, current.etag, crypto.randomUUID()); if (stamp !== sequence.current) return; await open(result.batchId, stamp); if (stamp !== sequence.current) return; await refresh(stamp); })}>Tạo preview mới từ file</button>}{batch.state === 'PreviewReady' && <><button className="primary-button" disabled={busy || loading || !can('commit') || batch.acceptedCount === 0} onClick={() => { key.current = crypto.randomUUID(); setPending('commit'); }}>Import Valid</button><button className="secondary-button" disabled={busy || loading || !can('cancel')} onClick={() => { key.current = crypto.randomUUID(); setPending('cancel'); }}>Hủy preview</button></>}</div>
      <div className="field-group"><label htmlFor="import-outcome">Row outcome</label><select id="import-outcome" value={outcome} disabled={busy || loading} onChange={e => { const value = e.target.value; void run(stamp => open(batch.id, stamp, value)); }}><option value="">Tất cả rows</option>{['Valid', 'Invalid', 'Duplicate', 'Applied', 'Failed'].map(value => <option key={value}>{value}</option>)}</select></div>
      <div className="resource-cards">{rows.map(row => <article className="resource-card" key={row.rowNumber}><div><h3>Row {row.rowNumber} · {row.outcome}</h3>{row.reasonCode && <p>{row.reasonCode}</p>}{row.warnings.length > 0 && <p>Warnings: {row.warnings.join(', ')}</p>}{row.candidate && <details><summary>{row.candidate.title}</summary><p className="literal-text">{row.candidate.description}</p><p>{row.candidate.isAllDay ? `${row.candidate.startDate} — ${row.candidate.endDateExclusive} (end exclusive)` : `${row.candidate.startAt} — ${row.candidate.endAt}`} · {row.candidate.timeZoneId}</p><p className="literal-text">UID: {row.candidate.uid}</p></details>}</div></article>)}</div>{rows.length === 0 && <p>Không có rows trong bộ lọc này.</p>}{rowCursor !== null && <button className="secondary-button" disabled={busy || loading} onClick={() => void run(async stamp => { const page = await importRows(batch.id, outcome, rowCursor); if (stamp !== sequence.current) return; setRows(items => [...items, ...page.items]); setRowCursor(page.nextCursor as number | null); })}>Tải thêm rows</button>}
    </article>}
    <ResourceFormDialog open={editor} title="Chọn file ICS" busy={busy} dirty={selected !== null} onClose={clearEditor}><form className="resource-form calendar-import-form" onSubmit={e => void preview(e)}><label htmlFor="ics-file">ICS file</label><input id="ics-file" type="file" accept=".ics,text/calendar" disabled={busy || loading} onChange={e => { setSelected(e.target.files?.[0] ?? null); key.current = crypto.randomUUID(); }} required /><p>Scan và validate trước preview. Nội dung được giữ private trong Files; không tải URL hoặc chạy reminder.</p>{file && <p>{file.scanState} · {file.byteLength} bytes</p>}{error && <p role="alert">{error}</p>}<button className="primary-button" type="submit" disabled={busy || loading || !selected}>{busy ? 'Đang scan / validate…' : 'Validate / Preview'}</button></form></ResourceFormDialog>
    {pending && batch && <ActionDialog title={pending === 'commit' ? 'Import Valid Events?' : 'Hủy preview này?'} description={pending === 'commit' ? `Áp dụng ${batch.acceptedCount} entry hợp lệ thành Scheduled. Bạn xác nhận các cảnh báo overlap, timezone và phần bị bỏ qua trong report; không tạo reminder.` : 'Xóa candidate staging; giữ report và provenance. Không tạo Event.'} confirmLabel={pending === 'commit' ? 'Xác nhận Import' : 'Xác nhận hủy'} tone={pending === 'commit' ? 'primary' : 'danger'} onConfirm={change} onClose={() => setPending(null)} />}
    {leave && <ActionDialog title="Rời import đang chuẩn bị?" description="Selection sẽ bị bỏ; preview đã lưu vẫn còn trong Operations để mở lại hoặc hủy." confirmLabel="Rời trang" cancelLabel="Tiếp tục import" onConfirm={() => { clearEditor(); setBatch(null); setRows([]); setLeave(null); leave.proceed(); return true; }} onClose={() => setLeave(null)} />}
  </section>;
}
