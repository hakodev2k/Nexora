import { useEffect, useRef, useState } from 'react';
import { getDocumentVersion, listDocumentVersions, NexoraApiError, type DocumentVersionSummary, type DocumentVersionPreview } from './api';

export function DocumentHistory({ pageId, revision, disabled, onAuthLost, formatTime }: {
  pageId: string; revision: string; disabled: boolean; onAuthLost: () => Promise<void>; formatTime: (value: string) => string;
}) {
  const [open, setOpen] = useState(false), [items, setItems] = useState<DocumentVersionSummary[]>([]);
  const [cursor, setCursor] = useState<string | null>(null), [preview, setPreview] = useState<DocumentVersionPreview | null>(null);
  const [loaded, setLoaded] = useState(false), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null);
  const epoch = useRef(0), flight = useRef(false), mounted = useRef(false), request = useRef<AbortController | null>(null);
  const disabledRef = useRef(disabled); disabledRef.current = disabled;
  const authLostRef = useRef(onAuthLost); authLostRef.current = onAuthLost;
  function clear() {
    epoch.current++; request.current?.abort(); request.current = null; flight.current = false;
    setItems([]); setCursor(null); setPreview(null); setLoaded(false); setBusy(false); setError(null);
  }
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; epoch.current++; request.current?.abort(); flight.current = false; }; }, []);
  useEffect(() => { clear(); setOpen(false); }, [pageId, revision, disabled]);
  async function read(version?: number, append = false) {
    if (disabledRef.current || flight.current || (append && !cursor)) return;
    const stamp = ++epoch.current, controller = new AbortController(); request.current?.abort(); request.current = controller;
    flight.current = true; setBusy(true); setError(null); setPreview(null); setOpen(true);
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
  return <section className="form-panel" aria-label="Lịch sử phiên bản Document">
    <div className="section-heading"><h2>Page versions</h2><button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read()}>Đọc lịch sử</button>{open && <button type="button" className="link-button" onClick={() => { clear(); setOpen(false); }}>Đóng lịch sử</button>}</div>
    {open && !disabled && <>
      <p className="field-help">Preview chỉ đọc bản đã Save; draft hiện tại giữ nguyên.</p>
      {busy && <p role="status">Đang đọc phiên bản…</p>}
      {error && <div role="alert" className="notice notice-error">Không thể đọc lịch sử: {error}</div>}
      {loaded && !error && items.length === 0 && <p>Chưa có phiên bản được lưu.</p>}
      {items.length > 0 && <ol className="resource-cards">{items.map(item => <li className="resource-card" key={item.id}><div><h3>Version {item.versionNumber} — {item.title}</h3><p>{formatTime(item.createdAt)}</p><p>{!item.tagSnapshotCaptured ? 'Tag chưa được ghi nhận trong snapshot cũ' : item.tagName ?? 'Không có Tag'}</p></div><button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read(item.versionNumber)}>Preview version {item.versionNumber}</button></li>)}</ol>}
      {cursor && <button type="button" className="secondary-button" disabled={disabled || busy} onClick={() => void read(undefined, true)}>Tải thêm phiên bản</button>}
      {preview && <article className="conflict-panel" aria-label={`Preview version ${preview.snapshot.versionNumber}`}><h3>Version {preview.snapshot.versionNumber} — {preview.snapshot.title}</h3><dl><dt>Type / Editor mode</dt><dd>{preview.snapshot.documentType} / {preview.snapshot.editorMode}</dd><dt>Trạng thái tại Save</dt><dd>{preview.snapshot.savedStatus}</dd><dt>Thời gian Save</dt><dd>{formatTime(preview.snapshot.createdAt)}</dd><dt>Actor</dt><dd>{preview.snapshot.createdByUserId ?? 'Không có thông tin actor'}</dd><dt>Tag tại Save</dt><dd>{!preview.snapshot.tagSnapshotCaptured ? 'Chưa được ghi nhận trong snapshot cũ' : preview.snapshot.tagName ?? 'Không có Tag'}</dd><dt>Change note</dt><dd>{preview.snapshot.changeNote ?? 'Không có'}</dd></dl><pre className="conflict-preview">{preview.body}</pre></article>}
    </>}
  </section>;
}
