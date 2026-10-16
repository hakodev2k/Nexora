import { useEffect, useRef, useState } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { apiFetch, createIdempotencyKey, type FileRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

type Preview = { file: FileRecord; canTrash: boolean; blockCode: string | null };
export function FileTrashDialog({ fileId, onClose, onDone, onDenied }: {
  fileId: string; onClose: () => void; onDone: () => void; onDenied: () => void;
}) {
  const generation = useRef(0), flight = useRef(false);
  const request = useRef<{ etag: string; key: string } | null>(null);
  const [preview, setPreview] = useState<Preview | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false), [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  const denied = (e: unknown) => { const failure = e as { status?: number; code?: string };
    return [401, 403, 404].includes(failure.status ?? 0) || failure.code === 'ModuleUnavailable'; };
  useEffect(() => {
    const stamp = ++generation.current; setPreview(null); setLoading(true); setError('');
    void apiFetch<Preview>(`/api/v1/files/${encodeURIComponent(fileId)}/trash-preview`).then(value => {
      if (stamp !== generation.current) return;
      if (!value || value.file?.id !== fileId || typeof value.file.originalName !== 'string' ||
          typeof value.canTrash !== 'boolean' || typeof value.file.etag !== 'string' ||
          !/^"[A-Za-z0-9+/]{11}="$/.test(value.file.etag) ||
          (value.canTrash && (value.file.lifecycle !== 'Active' || value.file.scanState !== 'Clean' || value.blockCode !== null)) ||
          (!value.canTrash && !['LifecycleLocked', 'DependencyUnavailable'].includes(value.blockCode ?? '')))
        throw new Error('Invalid preview');
      setPreview(value);
    }).catch(e => { if (stamp === generation.current) { if (denied(e)) onDenied(); else setError('Không thể tải dependency preview. Hãy thử lại.'); } })
      .finally(() => { if (stamp === generation.current) setLoading(false); });
    return () => { generation.current++; };
  }, [fileId, reload]);
  useEffect(() => registerDirtyLeaveGuard(() => {
    if (!busy && !uncertain) return false;
    setError('Đợi kết quả hoặc retry cùng request trước khi rời trang.'); return true;
  }), [busy, uncertain]);
  useEffect(() => {
    const protect = (event: BeforeUnloadEvent) => { if (busy || uncertain) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect);
  }, [busy, uncertain]);
  async function commit() {
    if (flight.current || loading || !preview?.canTrash) return;
    if (!request.current) {
      try { request.current = { etag: preview.file.etag, key: createIdempotencyKey() }; }
      catch { setError('Không thể tạo Idempotency-Key an toàn. Request chưa được gửi.'); return; }
    }
    const command = request.current, stamp = generation.current;
    flight.current = true; setBusy(true); setError('');
    try {
      const ack = await apiFetch<void>(`/api/v1/files/${encodeURIComponent(fileId)}/trash`, {
        method: 'POST', headers: { 'If-Match': command.etag, 'Idempotency-Key': command.key }
      }, 204);
      if (stamp !== generation.current) return;
      if (ack !== undefined) throw Object.assign(new Error('Invalid acknowledgement'), { status: 0 });
      onDone();
    } catch (e) {
      if (stamp !== generation.current) return;
      if (denied(e)) { onDenied(); return; }
      const failure = e as { status?: number; code?: string };
      if (failure.status === undefined || failure.status === 0 || failure.status >= 500 || failure.code === 'RequestInProgress') {
        setUncertain(true); setError('Chưa rõ kết quả. Retry giữ nguyên file, ETag và Idempotency-Key; không đóng hoặc tạo request mới.');
      } else {
        request.current = null; setUncertain(false); setPreview(null);
        setError(failure.status === 412 ? 'File đã thay đổi. Tải lại preview và xác nhận lại với revision mới.'
          : 'Không thể Move to Trash. Tải lại preview để kiểm tra lifecycle hoặc dependencies hiện tại.');
      }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title="Move to Trash?" busy={busy} dirty={uncertain}
    onClose={() => { if (uncertain) setError('Kết quả chưa rõ; hãy retry cùng request. Hủy không hoàn tác request đã gửi.'); else onClose(); }}>
    <p>File sẽ rời Active views. Binary không bị purge; khôi phục là thao tác riêng theo policy.</p>
    {loading && <p role="status">Đang kiểm tra file, references và retention…</p>}
    {error && <p role="alert">{error}</p>}
    {preview && <section aria-label="Affected file">
      <p>{preview.file.originalName} · {preview.file.scanState} · {preview.file.lifecycle}</p>
      <p>{preview.canTrash ? 'Affected: 1 file; không có reference hoặc retention đang chặn.'
        : preview.blockCode === 'DependencyUnavailable' ? 'Bị chặn bởi references/history pins hoặc retention. Không tự detach hoặc purge.'
        : 'Lifecycle hiện tại không cho phép Move to Trash.'}</p>
    </section>}
    {busy && <p role="status">Đang xử lý Move to Trash…</p>}
    <button className="danger-button" type="button" disabled={loading || busy || !preview?.canTrash}
      onClick={() => void commit()}>{uncertain ? 'Retry cùng request' : 'Move to Trash'}</button>
    <button className="secondary-button" type="button" disabled={loading || busy || uncertain}
      onClick={() => { request.current = null; setPreview(null); setReload(value => value + 1); }}>Tải lại preview</button>
  </ResourceFormDialog>;
}
