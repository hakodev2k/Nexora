import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationGet, automationLifecycle, type AutomationDefinition } from './automationApi';

export function AutomationLifecycleDialog({ item, onClose, onDenied }: { item: AutomationDefinition; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const mounted = useRef(true);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  const restore = item.lifecycle === 'Trash';
  const pending = useRef<{ id: string; etag: string; batch: string | null; key: string } | null>(null);
  const flight = useRef(false), committed = useRef(false);
  const [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false), [done, setDone] = useState(false), [error, setError] = useState('');
  async function submit() {
    if (flight.current || done) return; flight.current = true; setBusy(true); setError('');
    try {
      if (!pending.current) {
        const current = (await automationGet(item.id)).definition;
        if (!mounted.current) return;
        if (current.etag !== item.etag || current.lifecycle !== item.lifecycle || current.deletionBatchId !== item.deletionBatchId) {
          setDone(true); setError('Revision hoặc deletion cohort đã thay đổi. Đóng và tải lại trước khi tạo command mới.'); return;
        }
        if (restore && !current.deletionBatchId) { setDone(true); setError('Deletion cohort không khả dụng.'); return; }
        pending.current = { id: current.id, etag: current.etag, batch: restore ? current.deletionBatchId : null, key: createIdempotencyKey() };
      }
      const command = pending.current;
      const ack = await automationLifecycle(command.id, command.etag, command.batch, command.key);
      if (!mounted.current) return;
      if (!ack || ack.id !== command.id || typeof ack.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(ack.etag) || !Number.isSafeInteger(ack.versionNumber) || ack.versionNumber < 1) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      const bytes = atob(ack.etag.slice(1, -1));
      if (bytes.length !== 8 || '"' + btoa(bytes) + '"' !== ack.etag) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      committed.current = true;
      const current = (await automationGet(command.id)).definition;
      if (!mounted.current) return;
      const expected = restore ? 'Active' : 'Trash';
      setError(current.etag === ack.etag && current.lifecycle === expected
        ? (restore ? 'Đã khôi phục. Workflow vẫn Disabled.' : 'Đã chuyển vào Trash; versions được giữ nguyên.')
        : `Command trước đã hoàn tất; revision hiện đã thay đổi, lifecycle hiện tại: ${current.lifecycle}. Retry không áp dụng lại hiệu ứng.`);
      pending.current = null; setUncertain(false); setDone(true);
    } catch (e) {
      if (!mounted.current || onDenied(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (status === 404) { pending.current = null; setUncertain(false); setDone(true); setError(committed.current ? 'Command trước đã hoàn tất; resource hiện không còn khả dụng.' : 'Resource hiện không còn khả dụng. Đóng và tải lại danh sách.'); return; }
      if (committed.current || (pending.current && (status === undefined || status === 0 || status === 408 || status >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict'))) {
        setUncertain(true); setError('Chưa đối chiếu được kết quả. Retry giữ nguyên ETag, cohort và key.');
      } else { pending.current = null; setUncertain(false); setDone(true); setError(e instanceof Error ? e.message : 'Thao tác thất bại. Tải lại revision.'); }
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  return <ResourceFormDialog open title={restore ? 'Khôi phục workflow' : 'Chuyển workflow vào Trash'} busy={busy} dirty={uncertain} onClose={() => { if (uncertain || flight.current) setError('Retry cùng command trước khi đóng.'); else onClose(); }}>
    <p>{item.title}</p><p>{restore ? 'Khôi phục đúng deletion cohort và giữ workflow Disabled.' : 'Workflow ngừng khả dụng; immutable versions vẫn được giữ để khôi phục.'}</p>
    {error && <p role="status">{error}</p>}
    {!done && <button type="button" disabled={busy} onClick={() => void submit()}>{busy ? 'Đang xử lý…' : uncertain ? 'Retry cùng command' : restore ? 'Khôi phục' : 'Chuyển vào Trash'}</button>}
  </ResourceFormDialog>;
}
