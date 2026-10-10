import { useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { registerDirtyLeaveGuard } from './App';
import { apiFetch, renameFile, type FileRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';

const denied = (e: unknown) => {
  const error = e as { status?: number; code?: string };
  return [401, 403, 404].includes(error.status ?? 0) || error.code === 'ModuleUnavailable';
};
const extension = (name: string) => name.includes('.') ? name.slice(name.lastIndexOf('.')).toLowerCase() : '';

export function FileRenameDialog({ item, onSaved, onClose, onDenied }: {
  item: FileRecord; onSaved: (value: FileRecord) => void; onClose: () => void; onDenied: () => void;
}) {
  const inputId = useId(); const generation = useRef(0), flight = useRef(false);
  const request = useRef<{ name: string; etag: string; key: string } | null>(null);
  const [current, setCurrent] = useState(item), [name, setName] = useState(item.originalName);
  const [ready, setReady] = useState(false), [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false);
  const [conflict, setConflict] = useState<FileRecord | null>(null), [error, setError] = useState('');
  const dirty = name !== current.originalName || uncertain;
  const read = () => apiFetch<FileRecord>(`/api/v1/files/${encodeURIComponent(item.id)}`);
  const eligible = (value: FileRecord) => value.id === item.id && value.lifecycle === 'Active' && value.scanState === 'Clean';
  useEffect(() => {
    const stamp = ++generation.current;
    void read().then(value => {
      if (stamp !== generation.current) return;
      if (!eligible(value)) { onDenied(); return; }
      setCurrent(value); setName(value.originalName); setReady(true);
    }).catch(e => { if (stamp === generation.current) { if (denied(e)) onDenied(); else setError('Không thể tải revision file. Đóng dialog và thử lại.'); } });
    return () => { generation.current++; };
  }, [item.id]);
  useEffect(() => registerDirtyLeaveGuard(() => {
    if (!dirty && !busy) return false;
    setError('Lưu hoặc hủy bản nháp trước khi rời trang. Kết quả chưa rõ cần retry cùng request.'); return true;
  }), [dirty, busy]);
  useEffect(() => {
    const protect = (event: BeforeUnloadEvent) => { if (dirty || busy) { event.preventDefault(); event.returnValue = ''; } };
    window.addEventListener('beforeunload', protect); return () => window.removeEventListener('beforeunload', protect);
  }, [dirty, busy]);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!ready || flight.current || conflict) return;
    if (!request.current) {
      const normalized = name.trim();
      if (!normalized || normalized.length > 255 || /[\x00-\x1f\x7f/\\:*?"<>|]/.test(normalized) || normalized === '.' || normalized === '..') {
        setError('Tên file cần 1–255 ký tự, không chứa path hoặc ký tự điều khiển.'); return;
      }
      if (extension(normalized) !== extension(current.originalName)) { setError('Giữ nguyên extension của file.'); return; }
      request.current = { name: normalized, etag: current.etag, key: crypto.randomUUID() };
    }
    const command = request.current, stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      const value = await renameFile(item.id, command.etag, command.name, command.key);
      if (stamp !== generation.current) return;
      if (!value || !eligible(value) || value.originalName !== command.name || typeof value.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(value.etag))
        throw Object.assign(new Error('Missing acknowledgement'), { status: 0 });
      const bytes = atob(value.etag.slice(1, -1));
      if (bytes.length !== 8 || '"' + btoa(bytes) + '"' !== value.etag) throw Object.assign(new Error('Invalid acknowledgement'), { status: 0 });
      onSaved(value);
    } catch (e) {
      if (stamp !== generation.current) return;
      if (denied(e)) { onDenied(); return; }
      const failure = e as { status?: number; code?: string };
      if (failure.status === 412 || failure.code === 'IdempotencyReplay') {
        try {
          const latest = await read(); if (stamp !== generation.current) return;
          if (!eligible(latest)) { onDenied(); return; }
          request.current = null; setUncertain(false);
          if (failure.code === 'IdempotencyReplay') { onSaved(latest); return; }
          setConflict(latest); setError('File đã thay đổi. So sánh tên hiện tại trước khi áp dụng lại.');
        } catch (readError) { if (stamp === generation.current) { if (denied(readError)) onDenied(); else { setUncertain(true); setError('Không thể đọc trạng thái hiện tại. Retry giữ nguyên request.'); } } }
      } else if (failure.status === undefined || failure.status === 0 || failure.status >= 500 || failure.code === 'RequestInProgress') {
        setUncertain(true); setError('Chưa xác định kết quả đổi tên. Retry giữ nguyên tên, ETag và Idempotency-Key.');
      } else { request.current = null; setUncertain(false); setError(e instanceof Error ? e.message : 'Không thể đổi tên file.'); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function reapply() {
    if (!conflict || flight.current) return;
    const stamp = generation.current, compared = conflict; flight.current = true; setBusy(true);
    try {
      const latest = await read(); if (stamp !== generation.current) return;
      if (!eligible(latest)) { onDenied(); return; }
      if (latest.etag !== compared.etag) { setConflict(latest); setError('Revision lại thay đổi. Hãy so sánh lại.'); return; }
      setCurrent(latest); setConflict(null); request.current = null; setError('Bản nháp được giữ lại. Bấm lưu để gửi với ETag mới.');
    } catch (e) { if (stamp === generation.current) { if (denied(e)) onDenied(); else setError('Không thể kiểm tra revision mới.'); } }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title="Đổi tên file" dirty={dirty} busy={busy} onClose={onClose}>
    <p>Chỉ đổi tên hiển thị; binary, scan state và các references được giữ nguyên. Hủy bản nháp không hoàn tác một request đã gửi.</p>
    {error && <p role="alert">{error}</p>}{!ready && !error && <p role="status">Đang tải revision file…</p>}
    {conflict && <section aria-label="Tên file hiện tại"><p>{conflict.originalName}</p><button type="button" disabled={busy} onClick={() => void reapply()}>Áp dụng lại tên mới</button></section>}
    <form onSubmit={save} noValidate><label htmlFor={inputId}>Tên file</label><input id={inputId} value={name} maxLength={255} disabled={!ready || busy || uncertain || conflict !== null} onChange={event => { setName(event.target.value); request.current = null; }} />
      <button className="primary-button" disabled={!ready || busy || conflict !== null}>{busy ? 'Đang lưu…' : uncertain ? 'Retry cùng request' : 'Lưu tên file'}</button>
    </form>
  </ResourceFormDialog>;
}
