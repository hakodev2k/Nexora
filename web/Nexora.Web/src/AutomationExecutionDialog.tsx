import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationGet, automationEnable, automationStart, automationRunGet, type AutomationDefinition } from './automationApi';

export function AutomationExecutionDialog({ item, operation, onClose, onDenied }: { item: AutomationDefinition; operation: 'enable' | 'disable' | 'start'; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const mounted = useRef(true), flight = useRef(false), committed = useRef(false);
  const pending = useRef<{ id: string; etag: string; key: string } | null>(null);
  const [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false), [done, setDone] = useState(false), [message, setMessage] = useState('');
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; }; }, []);
  async function execute() {
    if (flight.current || done) return; flight.current = true; setBusy(true);
    try {
      if (!pending.current) {
        const current = (await automationGet(item.id)).definition; if (!mounted.current) return;
        if (current.etag !== item.etag || current.lifecycle !== 'Active') { setDone(true); setMessage('Revision thay đổi. Đóng và so sánh lại trước khi gửi command mới.'); return; }
        pending.current = { id: item.id, etag: item.etag, key: createIdempotencyKey() };
      }
      const command = pending.current;
      const ack = operation === 'start' ? await automationStart(command.id, command.etag, command.key) : await automationEnable(command.id, command.etag, operation === 'enable', command.key);
      if (!mounted.current) return;
      if (!ack || typeof ack.id !== 'string' || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(ack.id) || ack.id === '00000000-0000-0000-0000-000000000000' || (operation !== 'start' && ack.id !== command.id)) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      if (operation !== 'start') {
        const saved = ack as { etag?: string; versionNumber?: number };
        if (typeof saved.etag !== 'string' || !/^"[A-Za-z0-9+/]{11}="$/.test(saved.etag) || !Number.isSafeInteger(saved.versionNumber) || saved.versionNumber !== item.currentVersion) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
        const bytes = atob(saved.etag.slice(1, -1)); if (bytes.length !== 8 || '"' + btoa(bytes) + '"' !== saved.etag) throw Object.assign(new Error('ACK không hợp lệ.'), { status: 0 });
      }
      committed.current = true;
      if (operation === 'start') {
        const current = (await automationRunGet(ack.id)).run; if (!mounted.current) return;
        if (current.definitionId !== item.id || current.versionNumber !== item.currentVersion) throw Object.assign(new Error('Run ACK không khớp version.'), { status: 0 });
        setMessage(`Run ${current.id} · version ${current.versionNumber} · trạng thái hiện tại ${current.state}. Xem lịch sử để theo dõi các bước.`);
      } else {
        const current = (await automationGet(command.id)).definition; if (!mounted.current) return;
        setMessage(`Command ${operation} trước đã hoàn tất. Hiện tại: ${current.lifecycle} · ${current.state} · version ${current.currentVersion}.`);
      }
      pending.current = null; setUncertain(false); setDone(true);
    } catch (e) {
      if (!mounted.current || onDenied(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (status === 404) { pending.current = null; setUncertain(false); setDone(true); setMessage(committed.current ? 'Command trước đã hoàn tất; resource hiện không còn khả dụng.' : 'Resource hiện không còn khả dụng. Đóng và tải lại danh sách.'); return; }
      if (committed.current || (pending.current && (status === undefined || status === 0 || status >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict'))) { setUncertain(true); setMessage('Chưa đối chiếu được kết quả; Retry giữ nguyên command, ETag và key.'); }
      else { pending.current = null; setDone(true); setUncertain(false); setMessage(e instanceof Error ? e.message : 'Command bị từ chối.'); }
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  return <ResourceFormDialog open title={operation === 'start' ? 'Chạy workflow' : operation === 'enable' ? 'Enable workflow' : 'Disable workflow'} busy={busy} dirty={uncertain} onClose={() => { if (uncertain || flight.current) setMessage('Retry cùng command trước khi đóng.'); else onClose(); }}>
    <p>{item.title} · version {item.currentVersion}</p><p>{operation === 'start' ? 'Run tạo Task thật trong SQL local, theo version này. Hủy không rollback Task đã tạo. Session/quyền hiện tại sẽ được kiểm tra trước mỗi bước.' : operation === 'enable' ? 'Chỉ bật trusted manual Task workflow đã validate và được operator cho phép.' : 'Dừng queued work và các bước tiếp theo; Task đã tạo được giữ nguyên.'}</p>
    {message && <p role="status">{message}</p>}{!done && <button disabled={busy} onClick={() => void execute()}>{busy ? 'Đang xử lý…' : uncertain ? 'Retry cùng command' : 'Xác nhận'}</button>}
  </ResourceFormDialog>;
}
