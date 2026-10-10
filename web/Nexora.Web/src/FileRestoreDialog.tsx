import { useEffect, useRef, useState } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { apiFetch, createIdempotencyKey, type FileRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

type Preview = { file: FileRecord; deletionBatchId: string; canRestore: boolean; blockCode: string | null };
export function FileRestoreDialog({ fileId, onClose, onDone, onDenied }: {
  fileId: string; onClose: () => void; onDone: () => void; onDenied: () => void;
}) {
  const generation = useRef(0), flight = useRef(false);
  const request = useRef<{ etag: string; key: string; batchId: string } | null>(null);
  const [preview, setPreview] = useState<Preview | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false);
  const [uncertain, setUncertain] = useState(false), [error, setError] = useState('');
  const [reload, setReload] = useState(0);
  const denied = (e: unknown) => { const failure = e as { status?: number; code?: string };
    return [401, 403, 404].includes(failure.status ?? 0) || failure.code === 'ModuleUnavailable'; };
  useEffect(() => {
    const stamp = ++generation.current; setPreview(null); setLoading(true); setError('');
    void apiFetch<Preview>(`/api/v1/files/${encodeURIComponent(fileId)}/restore-preview`).then(value => {
      if (stamp !== generation.current) return;
      if (!value || value.file?.id !== fileId || typeof value.file.originalName !== 'string' ||
          typeof value.canRestore !== 'boolean' || typeof value.file.etag !== 'string' ||
          !/^"[A-Za-z0-9+/]{11}="$/.test(value.file.etag) ||
          typeof value.deletionBatchId !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.deletionBatchId) ||
          value.deletionBatchId === '00000000-0000-0000-0000-000000000000' ||
          (value.canRestore && (value.file.lifecycle !== 'Trash' || value.file.scanState !== 'Clean' || value.blockCode !== null)) ||
          (!value.canRestore && !['LifecycleLocked', 'StorageUnavailable'].includes(value.blockCode ?? '')))
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
    if (flight.current || loading || !preview?.canRestore) return;
    if (!request.current) {
      try { request.current = { etag: preview.file.etag, key: createIdempotencyKey(), batchId: preview.deletionBatchId }; }
      catch { setError('Không thể tạo Idempotency-Key an toàn. Request chưa được gửi.'); return; }
    }
    const command = request.current, stamp = generation.current;
    flight.current = true; setBusy(true); setError('');
    try {
      const ack = await apiFetch<FileRecord>(`/api/v1/files/${encodeURIComponent(fileId)}/restore`, {
        method: 'POST', headers: { 'If-Match': command.etag, 'Idempotency-Key': command.key },
        body: JSON.stringify({ deletionBatchId: command.batchId })
      }, 200);
      if (stamp !== generation.current) return;
      if (!ack || ack.id !== fileId || typeof ack.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(ack.etag) || !['Active', 'Trash'].includes(ack.lifecycle)) throw Object.assign(new Error('Invalid acknowledgement'), { status: 0 });
      const encoded = ack.etag.slice(1, -1), decoded = atob(encoded);
      if (decoded.length !== 8 || btoa(decoded) !== encoded) throw Object.assign(new Error('Invalid acknowledgement'), { status: 0 });
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
          : 'Không thể Restore. Tải lại preview để kiểm tra lifecycle hoặc dependencies hiện tại.');
      }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title="Restore?" busy={busy} dirty={uncertain}
    onClose={() => { if (uncertain) setError('Kết quả chưa rõ; hãy retry cùng request. Hủy không hoàn tác request đã gửi.'); else onClose(); }}>
    <p>Restore đúng một file trong deletion cohort được server xác nhận. Giữ nguyên binary; không restore source hoặc cohort khác.</p>
    {loading && <p role="status">Đang kiểm tra deletion cohort và binary…</p>}
    {error && <p role="alert">{error}</p>}
    {preview && <section aria-label="Affected file">
      <p>{preview.file.originalName} · {preview.file.scanState} · {preview.file.lifecycle}</p>
      <p>Deletion cohort: {preview.deletionBatchId}</p>
      <p>{preview.canRestore ? 'Affected: 1 file trong đúng deletion cohort; binary còn hiện hữu.'
        : preview.blockCode === 'StorageUnavailable' ? 'Binary giữ lại không khả dụng; không thể phục hồi bằng metadata hoặc tạo nội dung giả.'
        : 'Lifecycle hiện tại không cho phép Restore.'}</p>
    </section>}
    {busy && <p role="status">Đang xử lý Restore…</p>}
    <button className="danger-button" type="button" disabled={loading || busy || !preview?.canRestore}
      onClick={() => void commit()}>{uncertain ? 'Retry cùng request' : 'Restore'}</button>
    <button className="secondary-button" type="button" disabled={loading || busy || uncertain}
      onClick={() => { request.current = null; setPreview(null); setReload(value => value + 1); }}>Tải lại preview</button>
  </ResourceFormDialog>;
}
