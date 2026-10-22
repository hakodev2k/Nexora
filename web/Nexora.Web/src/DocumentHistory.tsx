import { useEffect, useRef, useState } from 'react';
import { DocumentBodyView } from './DocumentBodyEditor';
import { DocumentExportLauncher } from './DocumentExportDialog';
import { DocumentVersionDiff } from './DocumentVersionDiff';
import { compareDocumentVersions, getDocumentVersion, listDocumentVersions, NexoraApiError, type DocumentVersionSummary, type DocumentVersionPreview } from './api';

export function DocumentHistory({ pageId, revision, disabled, onAuthLost, formatTime, restoreDisabled = true, exportDisabled = false, onSourceUnavailable, currentTagId = null, currentTagName = null, onRestore }: {
  pageId: string; revision: string; disabled: boolean; onAuthLost: () => Promise<void>; formatTime: (value: string) => string;
  restoreDisabled?: boolean; currentTagId?: string | null; currentTagName?: string | null;
  exportDisabled?: boolean; onSourceUnavailable?: () => void;
  onRestore?: (version: number, tag?: { id: string | null }) => Promise<boolean>;
}) {
  const [open, setOpen] = useState(false), [items, setItems] = useState<DocumentVersionSummary[]>([]);
  const [cursor, setCursor] = useState<string | null>(null), [preview, setPreview] = useState<DocumentVersionPreview | null>(null);
  const [loaded, setLoaded] = useState(false), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null);
  const epoch = useRef(0), flight = useRef(false), mounted = useRef(false), request = useRef<AbortController | null>(null);
  const disabledRef = useRef(disabled); disabledRef.current = disabled;
  const [before, setBefore] = useState(''), [after, setAfter] = useState('');
  const [comparison, setComparison] = useState<Awaited<ReturnType<typeof compareDocumentVersions>> | null>(null);
  const [tagChoice, setTagChoice] = useState('snapshot'), [confirmRestore, setConfirmRestore] = useState(false);
  const authLostRef = useRef(onAuthLost); authLostRef.current = onAuthLost;
  function clear() {
    epoch.current++; request.current?.abort(); request.current = null; flight.current = false;
    setItems([]); setCursor(null); setPreview(null); setLoaded(false); setBusy(false); setError(null);
    setBefore(''); setAfter(''); setComparison(null); setConfirmRestore(false); setTagChoice('snapshot');
  }
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; epoch.current++; request.current?.abort(); flight.current = false; }; }, []);
  useEffect(() => { clear(); setOpen(false); }, [pageId, revision, disabled]);
  async function read(version?: number, append = false) {
    if (disabledRef.current || flight.current || (append && !cursor)) return;
    const stamp = ++epoch.current, controller = new AbortController(); request.current?.abort(); request.current = controller;
    flight.current = true; setBusy(true); setError(null); setPreview(null); setComparison(null); setConfirmRestore(false); setTagChoice('snapshot'); setOpen(true);
    if (version === undefined && !append) { setItems([]); setCursor(null); setLoaded(false); }
    try {
      if (version === undefined) {
        const page = await listDocumentVersions(pageId, append ? cursor! : '', controller.signal);
        if (!mounted.current || stamp !== epoch.current || controller.signal.aborted || disabledRef.current) return;
        setItems(previous => append ? [...new Map([...previous, ...page.items].map(item => [item.id, item])).values()] : page.items);
        setCursor(page.nextCursor); setLoaded(true);
      } else {
        const result = await getDocumentVersion(pageId, version, controller.signal);
        if (!mounted.current || stamp !== epoch.current || controller.signal.aborted || disabledRef.current) return;
        setPreview(result);
      }
    } catch (failure) {
      if (!mounted.current || stamp !== epoch.current || controller.signal.aborted) return;
      setItems([]); setCursor(null); setPreview(null); setLoaded(false);
      setError(failure instanceof Error ? failure.message : 'Không thể đọc lịch sử Document.');
      if (failure instanceof NexoraApiError && failure.status === 401) await authLostRef.current();
    } finally {
      if (mounted.current && stamp === epoch.current) { flight.current = false; request.current = null; setBusy(false); }
    }
  }
  async function compare() {
    if (disabledRef.current || flight.current || !before || !after) return;
    const stamp = ++epoch.current, controller = new AbortController(); request.current?.abort(); request.current = controller;
    flight.current = true; setBusy(true); setError(null); setComparison(null);
    try {
      const result = await compareDocumentVersions(pageId, Number(before), Number(after), controller.signal);
      if (mounted.current && stamp === epoch.current && !disabledRef.current) setComparison(result);
    } catch (failure) {
      if (!mounted.current || stamp !== epoch.current) return;
      setItems([]); setPreview(null); setComparison(null); setError(failure instanceof Error ? failure.message : 'Không thể so sánh phiên bản.');
      if (failure instanceof NexoraApiError && failure.status === 401) await authLostRef.current();
    } finally { if (mounted.current && stamp === epoch.current) { flight.current = false; setBusy(false); request.current = null; } }
  }
  return <section className="form-panel" aria-label="Lịch sử phiên bản Document">
    <div className="section-heading"><h2>Page versions</h2><button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read()}>Đọc lịch sử</button>{open && <button type="button" className="link-button" onClick={() => { clear(); setOpen(false); }}>Đóng lịch sử</button>}</div>
    {open && !disabled && <>
      <p className="field-help">Preview chỉ đọc bản đã Save; draft hiện tại giữ nguyên.</p>
      {busy && <p role="status">Đang đọc phiên bản…</p>}
      {error && <div role="alert" className="notice notice-error">Không thể đọc lịch sử: {error}</div>}
      {loaded && !error && items.length === 0 && <p>Chưa có phiên bản được lưu.</p>}
      {items.length > 0 && <ol className="resource-cards">{items.map(item => <li className="resource-card" key={item.id}><div><h3>Version {item.versionNumber} — {item.title}</h3><p>{formatTime(item.createdAt)}</p><p>{!item.tagSnapshotCaptured ? 'Tag chưa được ghi nhận trong snapshot cũ' : item.tagName ?? 'Không có Tag'}</p></div><button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read(item.versionNumber)}>Preview version {item.versionNumber}</button></li>)}</ol>}
      {cursor && <button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read(undefined, true)}>Tải thêm phiên bản</button>}
      {items.length > 0 && <div className="form-panel"><h3>So sánh snapshot</h3><div className="form-grid">
        <label>Phiên bản trước<select value={before} onChange={event => { setBefore(event.target.value); setComparison(null); }} disabled={disabled || busy}><option value="">Chọn phiên bản</option>{items.map(item => <option key={item.id} value={item.versionNumber}>v{item.versionNumber} — {item.title}</option>)}</select></label>
        <label>Phiên bản sau<select value={after} onChange={event => { setAfter(event.target.value); setComparison(null); }} disabled={disabled || busy}><option value="">Chọn phiên bản</option>{items.map(item => <option key={item.id} value={item.versionNumber}>v{item.versionNumber} — {item.title}</option>)}</select></label>
        </div><button type="button" disabled={disabled || busy || !before || !after} onClick={() => void compare()}>So sánh hai phiên bản</button></div>}
      {comparison && <DocumentVersionDiff before={comparison.before} after={comparison.after} />}
      {preview && <DocumentExportLauncher key={pageId + '|export-history|' + revision + '|' + preview.snapshot.versionNumber} target={{ id: pageId, etag: revision, versionNumber: preview.snapshot.versionNumber }} historical disabled={disabled || busy || exportDisabled} onUnavailable={() => { clear(); onSourceUnavailable?.(); }} onAuthLost={onAuthLost} />}
      {preview && <article className="conflict-panel" aria-label={`Preview version ${preview.snapshot.versionNumber}`}><h3>Version {preview.snapshot.versionNumber} — {preview.snapshot.title}</h3><dl><dt>Type / Editor mode</dt><dd>{preview.snapshot.documentType} / {preview.snapshot.editorMode}</dd><dt>Trạng thái tại Save</dt><dd>{preview.snapshot.savedStatus}</dd><dt>Thời gian Save</dt><dd>{formatTime(preview.snapshot.createdAt)}</dd><dt>Actor</dt><dd>{preview.snapshot.createdByUserId ?? 'Không có thông tin actor'}</dd><dt>Tag tại Save</dt><dd>{!preview.snapshot.tagSnapshotCaptured ? 'Chưa được ghi nhận trong snapshot cũ' : preview.snapshot.tagName ?? 'Không có Tag'}</dd><dt>Change note</dt><dd>{preview.snapshot.changeNote ?? 'Không có'}</dd></dl><DocumentBodyView mode={preview.snapshot.editorMode} body={preview.body} /></article>}
      {preview && onRestore && <div className="form-panel"><h3>Restore thành version mới</h3><p>Giữ nguyên type, mode, vị trí và lifecycle hiện tại. Snapshot cũ không bị sửa.</p>
        <label>Tag cho version mới<select value={tagChoice} onChange={event => { setTagChoice(event.target.value); setConfirmRestore(false); }} disabled={disabled || busy || restoreDisabled}>
          <option value="snapshot">Dùng Tag của snapshot nếu vẫn còn nguyên</option><option value="current">Xác nhận Tag hiện tại: {currentTagName ?? 'Không có Tag'}</option><option value="none">Xác nhận bỏ Tag</option>
        </select></label><label className="check-row"><input type="checkbox" checked={confirmRestore} onChange={event => setConfirmRestore(event.target.checked)} disabled={disabled || busy || restoreDisabled} />Tôi đã xem snapshot và lựa chọn Tag.</label>
        <button type="button" className="primary-button" disabled={disabled || busy || restoreDisabled || !confirmRestore} onClick={() => { setConfirmRestore(false); void onRestore(preview.snapshot.versionNumber, tagChoice === 'snapshot' ? undefined : { id: tagChoice === 'none' ? null : currentTagId }); }}>Tạo version mới từ snapshot này</button>
        {restoreDisabled && <p>Save hoặc bỏ draft trước; chỉ page Draft/Published được restore.</p>}</div>}
    </>}
  </section>;
}
