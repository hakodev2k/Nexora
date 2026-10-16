import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationPurge, automationPurgePreview, type AutomationDefinition, type AutomationPurgePreview } from './automationApi';

export function AutomationPurgeDialog({ item, onClose, onDenied }: { item: AutomationDefinition; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const generation = useRef(0), flight = useRef(false), cancelButton = useRef<HTMLButtonElement>(null);
  const pending = useRef<{ id: string; etag: string; batch: string; key: string } | null>(null);
  const [preview, setPreview] = useState<AutomationPurgePreview | null>(null), [confirmed, setConfirmed] = useState(false);
  const [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false), [done, setDone] = useState(false), [error, setError] = useState('');
  function close() { if (flight.current || uncertain) setError('Retry cùng purge command trước khi đóng.'); else onClose(); }
  async function load() {
    if (flight.current || pending.current) return;
    const stamp = generation.current; flight.current = true; setBusy(true); setError(''); setPreview(null); setConfirmed(false);
    try {
      const value = await automationPurgePreview(item.id); if (stamp !== generation.current) return;
      if (!value || value.id !== item.id || value.etag !== item.etag || value.deletionBatchId !== item.deletionBatchId) { setDone(true); setError('Revision/cohort đã thay đổi. Đóng và tải lại danh sách.'); return; }
      if (!Number.isSafeInteger(value.definitionVersions) || value.definitionVersions < 1 || !Number.isSafeInteger(value.scheduleVersions) || value.scheduleVersions < 0 || !Array.isArray(value.blockers) || value.blockers.some(code => typeof code !== 'string')) throw new Error('Purge preview không hợp lệ.');
      setPreview(value);
    } catch (e) {
      if (stamp !== generation.current || onDenied(e)) return;
      if ((e as { status?: number }).status === 404) { setDone(true); setPreview(null); }
      setError(e instanceof Error ? e.message : 'Không tải được purge preview.');
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); cancelButton.current?.focus(); } }
  }
  useEffect(() => { generation.current++; flight.current = false; void load(); return () => { generation.current++; }; }, [item.id]);
  async function submit() {
    if (flight.current || done || (!pending.current && (!preview || preview.blockers.length > 0 || !confirmed))) return;
    const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      if (!pending.current) pending.current = { id: preview!.id, etag: preview!.etag, batch: preview!.deletionBatchId, key: createIdempotencyKey() };
      const command = pending.current, ack = await automationPurge(command.id, command.etag, command.batch, command.key);
      if (stamp !== generation.current) return;
      if (!ack || ack.id !== command.id || ack.deletionBatchId !== command.batch || ack.purged !== true) throw Object.assign(new Error('Purge ACK không hợp lệ.'), { status: 0 });
      pending.current = null; setPreview(null); setUncertain(false); setDone(true); setError('Đã purge definition và versions. Audit/tombstone được giữ; không thể Restore.');
    } catch (e) {
      if (stamp !== generation.current || onDenied(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (pending.current && (status === undefined || status === 0 || (status ?? 0) >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict')) { setUncertain(true); setError('Chưa rõ kết quả purge. Retry giữ nguyên preview ETag, cohort và key.'); }
      else { pending.current = null; setPreview(null); setUncertain(false); setDone(true); setError(status === 404 ? 'Resource hiện không khả dụng; không suy ra command này đã thành công. Đóng và tải lại.' : e instanceof Error ? e.message : 'Purge bị từ chối. Tải lại preview.'); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  const blockerMessage: Record<string, string> = { RetainedOperationalHistory: 'Có run/occurrence history cần giữ; retention chưa cho phép xóa.', ResourceReferences: 'Có resource/file references chưa detach.', DeletionCohortUnavailable: 'Deletion cohort không hợp lệ hoặc đã thay đổi.' };
  return <ResourceFormDialog open title="Purge workflow vĩnh viễn" busy={busy} dirty={uncertain} onClose={close}>
    <button ref={cancelButton} type="button" disabled={busy || uncertain} onClick={close}>{done ? 'Đóng và tải lại' : 'Hủy'}</button>
    <p>{item.title}</p><p>Chỉ áp dụng đúng definition trong Trash và deletion cohort hiện tại. Content/versions bị xóa vĩnh viễn; audit không bị xóa. History đang được giữ hoặc references sẽ chặn purge.</p>
    {error && <p role="alert">{error}</p>}{busy && <p role="status">Đang xử lý…</p>}
    {preview && <section aria-label="Purge impact"><p>{preview.definitionVersions} definition versions · {preview.scheduleVersions} schedule versions sẽ bị xóa.</p><ul>{preview.blockers.map(code => <li key={code}>{blockerMessage[code] ?? 'Dependency chưa cho phép purge.'}</li>)}</ul>
      {!preview.blockers.length && <label><input type="checkbox" checked={confirmed} disabled={busy || uncertain} onChange={e => setConfirmed(e.target.checked)} />Tôi xác nhận xóa vĩnh viễn; không thể khôi phục trong ứng dụng.</label>}</section>}
    {!done && !uncertain && <button type="button" disabled={busy} onClick={() => void load()}>Tải lại preview</button>}
    {!done && <button type="button" className="danger-button" disabled={busy || (!uncertain && (!preview || preview.blockers.length > 0 || !confirmed))} onClick={() => void submit()}>{uncertain ? 'Retry cùng purge command' : 'Xác nhận purge vĩnh viễn'}</button>}
  </ResourceFormDialog>;
}
