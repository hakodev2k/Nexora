import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { executeDocumentTree, previewDocumentTree, type TreePreview, type TreeTarget } from './documentTopologyApi';
import { ResourceFormDialog } from './ResourceFormDialog';

export function DocumentTreeDialog({ target, onClose, onCommitted, onUnavailable, onAuthLost }: { target: TreeTarget; onClose: () => void; onCommitted: () => void; onUnavailable: () => void; onAuthLost: () => Promise<void> }) {
  const [preview, setPreview] = useState<TreePreview | null>(null), [error, setError] = useState(''), [busy, setBusy] = useState(false), [confirmation, setConfirmation] = useState(''), [confirmed, setConfirmed] = useState(false), [reload, setReload] = useState(0);
  const [intent, setIntent] = useState<{ preview: TreePreview; confirmation: string; key: string } | null>(null);
  const flight = useRef(false), callbacks = useRef({ onUnavailable, onAuthLost }); callbacks.current = { onUnavailable, onAuthLost };
  useEffect(() => {
    const controller = new AbortController(); setPreview(null); setError(''); setConfirmed(false); setConfirmation(''); setBusy(true);
    previewDocumentTree(target, controller.signal).then(value => { if (!controller.signal.aborted) setPreview(value); }).catch(value => {
      if (controller.signal.aborted) return; setError(value instanceof Error ? value.message : 'Không thể đọc phạm vi.');
      if ([401, 403, 404].includes(value.status)) { callbacks.current.onUnavailable(); if (value.status === 401) void callbacks.current.onAuthLost(); }
    }).finally(() => { if (!controller.signal.aborted) setBusy(false); });
    return () => controller.abort();
  }, [target.id, target.kind, target.operation, reload]);
  async function submit() {
    if (flight.current || (!intent && (!preview || !confirmed || (target.operation === 'purge' && confirmation !== 'PURGE')))) return;
    const command = intent ?? { preview: preview!, confirmation, key: createIdempotencyKey() };
    flight.current = true; setIntent(command); setBusy(true); setError('');
    try { await executeDocumentTree(command.preview, command.confirmation, command.key); setIntent(null); onCommitted(); }
    catch (value) {
      const failure = value as { status?: number; code?: string; message?: string }; setError(failure.message ?? 'Kết quả chưa rõ.');
      if (failure.status && failure.status < 500 && !['RequestInProgress', 'IdempotencyConflict'].includes(failure.code ?? '')) { setIntent(null); setPreview(null); setConfirmed(false); }
      if ([401, 403, 404].includes(failure.status ?? 0)) { setPreview(null); callbacks.current.onUnavailable(); if (failure.status === 401) void callbacks.current.onAuthLost(); }
    } finally { flight.current = false; setBusy(false); }
  }
  return <ResourceFormDialog open title={`Documents · ${target.operation}`} busy={busy} dirty={intent !== null || confirmed || Boolean(confirmation)} onClose={onClose}>
    {busy && <p role="status">Đang xử lý…</p>}{error && <p role="alert">{error}</p>}
    {preview && <><p>{preview.detailsHidden ? 'Phạm vi gồm các mục bạn không có quyền đọc. Chi tiết và số lượng được ẩn.' : `${preview.affectedCount} mục sẽ bị ảnh hưởng.`}</p>{!preview.detailsHidden && <ul>{preview.members?.slice(0, 100).map(item => <li key={item.kind + item.id}>{item.title ?? 'Không khả dụng'} · {item.kind} · {item.state}</li>)}</ul>}{(preview.members?.length ?? 0) > 100 && <p>Chỉ hiển thị 100 mục đầu.</p>}
      <p>{target.operation === 'purge' ? 'Xóa vĩnh viễn các mục trong phạm vi. Không thể hoàn tác.' : target.operation === 'trash' ? 'Các mục trong phạm vi sẽ vào Thùng rác. Liên kết chia sẻ bị thu hồi.' : target.operation === 'restore' ? 'Khôi phục đúng phạm vi đã đưa vào Thùng rác. Liên kết chia sẻ không được bật lại.' : 'Thay đổi áp dụng cho phạm vi được liệt kê; các lần lưu phiên bản được giữ nguyên.'}</p>
      <label><input type="checkbox" checked={confirmed} disabled={busy || intent !== null} onChange={event => setConfirmed(event.target.checked)} /> Tôi xác nhận phạm vi thao tác.</label>
      {target.operation === 'purge' && <label>Nhập PURGE<input value={confirmation} disabled={busy || intent !== null} onChange={event => setConfirmation(event.target.value)} /></label>}</>}
    {intent && <p role="alert">Giữ nguyên yêu cầu để đối chiếu. Bỏ yêu cầu tại đây không hoàn tác thao tác đã gửi.</p>}
    <div className="form-actions"><button type="button" className="danger-button" disabled={busy || (!intent && (!preview || !confirmed || (target.operation === 'purge' && confirmation !== 'PURGE')))} onClick={() => void submit()}>{intent ? 'Đối chiếu cùng yêu cầu' : 'Xác nhận'}</button>{!intent && <button type="button" disabled={busy} onClick={() => setReload(value => value + 1)}>Đọc lại phạm vi</button>}</div>
  </ResourceFormDialog>;
}
