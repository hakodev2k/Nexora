import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationRuns, automationRunGet, automationCancel, automationRetryStep, type AutomationRun, type AutomationRunDetail } from './automationApi';

export function AutomationRunPanel({ definitionId, canCancel, canRetry, onClose, onDenied }: { definitionId: string; canCancel: boolean; canRetry: boolean; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const generation = useRef(0), flight = useRef(false), cancelCommitted = useRef(false);
  const pending = useRef<{ id: string; etag: string; key: string; stepId?: number } | null>(null);
  const [state, setState] = useState(''), [items, setItems] = useState<AutomationRun[]>([]), [cursor, setCursor] = useState<string | null>(null);
  const [detail, setDetail] = useState<AutomationRunDetail | null>(null), [busy, setBusy] = useState(false), [error, setError] = useState(''), [uncertain, setUncertain] = useState(false);
  const states = ['Queued', 'Running', 'Succeeded', 'PartiallySucceeded', 'Failed', 'Cancelled', 'TimedOut', 'Skipped'];
  async function load(more = false) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const page = await automationRuns(definitionId, state, more ? cursor ?? undefined : undefined); if (stamp === generation.current) { setItems(old => more ? [...new Map([...old, ...page.items].map(r => [r.id, r])).values()] : page.items); setCursor(page.nextCursor); } }
    catch (e) { if (stamp === generation.current && !onDenied(e)) { setError(e instanceof Error ? e.message : 'Không tải được runs.'); setCursor(null); } }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  useEffect(() => { generation.current++; flight.current = false; setItems([]); setCursor(null); void load(); return () => { generation.current++; }; }, [state]);
  async function inspect(id: string) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const value = await automationRunGet(id); if (stamp === generation.current) { if (value.run.definitionId !== definitionId) throw new Error('Run không thuộc definition này.'); setDetail(value); } }
    catch (e) { if (stamp === generation.current && !onDenied(e)) setError(e instanceof Error ? e.message : 'Không tải được run.'); }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  async function cancel(stepId?: number) {
    if (!detail || flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try {
      if (!pending.current) pending.current = { id: detail.run.id, etag: detail.run.etag, key: createIdempotencyKey(), stepId };
      const command = pending.current, ack = command.stepId === undefined ? await automationCancel(command.id, command.etag, command.key) : await automationRetryStep(command.id, command.stepId, command.etag, command.key);
      if (stamp !== generation.current) return;
      if (!ack || ack.id !== command.id) throw Object.assign(new Error('Command ACK không hợp lệ.'), { status: 0 });
      cancelCommitted.current = true;
      const current = await automationRunGet(ack.id); if (stamp !== generation.current) return;
      setDetail(current); pending.current = null; cancelCommitted.current = false; setUncertain(false); setError('Command đã được nhận; trạng thái hiện tại hiển thị bên dưới. Task đã tạo được giữ nguyên.');
    } catch (e) {
      if (stamp !== generation.current || onDenied(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (status === 404) { pending.current = null; cancelCommitted.current = false; setUncertain(false); setDetail(null); setItems([]); setCursor(null); setError('Run hiện không còn khả dụng. Tải lại lịch sử.'); return; }
      if (cancelCommitted.current || status === undefined || status === 0 || status >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict') { setUncertain(true); setError('Chưa rõ kết quả. Retry giữ nguyên run/step/ETag/key.'); }
      else { pending.current = null; setUncertain(false); setError(e instanceof Error ? e.message : 'Hủy bị từ chối; tải lại revision.'); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title={detail ? 'Run / step detail' : 'Run history'} busy={busy} dirty={uncertain} onClose={() => { if (flight.current || uncertain) setError('Retry cùng command trước khi đóng.'); else onClose(); }}>
    {error && <p role="alert">{error}</p>}
    {detail ? <section><h3>{detail.run.id}</h3><p>Version {detail.run.versionNumber} · {detail.run.state} · {detail.run.code}</p><p>{new Date(detail.run.createdAt).toLocaleString()} · {detail.run.startedAt ? new Date(detail.run.startedAt).toLocaleString() : 'Chưa bắt đầu'} · {detail.run.finishedAt ? new Date(detail.run.finishedAt).toLocaleString() : 'Chưa kết thúc'}</p>
      {detail.run.effectUncertain && <p role="alert">Có hiệu ứng chưa đối chiếu được. Kiểm tra Task trước khi tạo run mới; Retry terminal run hiện chưa khả dụng.</p>}
      {detail.failureNotice && <section aria-label="Final failure notification"><h4>Thông báo kết quả cuối</h4><p>Một logical alert trong inbox. Email/Push chưa có provider được duyệt.</p><div className="delivery-summary">{detail.failureNotice.deliveries.map(delivery => <span key={delivery.channel}>{delivery.channel}: {delivery.state}{delivery.lastErrorCode ? ` · ${delivery.lastErrorCode}` : ''}</span>)}</div></section>}
      <ol>{detail.steps.map(step => <li key={step.stepId}>Step {step.stepId} · {step.state} · {step.attempts} attempts · {step.code}{step.taskId && <p>Task ID: {step.taskId}</p>}{step.effectUncertain && <p>Hiệu ứng chưa rõ; không giả định rollback.</p>}</li>)}</ol>
      <button disabled={busy || uncertain} onClick={() => void inspect(detail.run.id)}>Tải lại run</button><button disabled={busy || uncertain} onClick={() => { setDetail(null); void load(); }}>Về run history</button>
      {(uncertain || (canCancel && ['Queued', 'Running'].includes(detail.run.state))) && <div><p>Hủy dừng các bước tiếp theo; Task đã tạo được giữ nguyên.</p><button disabled={busy} onClick={() => void cancel()}>{uncertain ? 'Retry cùng command' : 'Xác nhận hủy run'}</button></div>}
      {canRetry && !uncertain && !detail.run.effectUncertain && ["Failed", "PartiallySucceeded"].includes(detail.run.state) && <div><p>Chỉ retry failure đã biết chưa tạo effect. Giữ nguyên version, command key và attempts; các bước thành công không chạy lại. Các bước chưa chạy sau failure sẽ tiếp tục.</p>{detail.steps.filter(step => step.state === "Failed" && !step.effectUncertain && step.taskId === null && step.attempts > 0 && step.attempts < 4).map(step => <button key={step.stepId} disabled={busy} onClick={() => void cancel(step.stepId)}>Xác nhận retry step {step.stepId} và các bước chưa chạy phía sau</button>)}</div>}
    </section> : <><label>State<select value={state} disabled={busy || uncertain} onChange={e => setState(e.target.value)}><option value="">Tất cả</option>{states.map(value => <option key={value}>{value}</option>)}</select></label><button disabled={busy} onClick={() => void load()}>Tải lại</button>
      {busy && <p role="status">Đang tải…</p>}{!busy && !error && items.length === 0 && <p>Chưa có run khớp bộ lọc.</p>}{items.map(run => <article key={run.id}><p>{run.id} · version {run.versionNumber} · {run.state} · {new Date(run.createdAt).toLocaleString()}</p><button disabled={busy} onClick={() => void inspect(run.id)}>Xem run</button></article>)}{cursor && !error && <button disabled={busy} onClick={() => void load(true)}>Tải thêm 25 runs</button>}</>}
  </ResourceFormDialog>;
}
