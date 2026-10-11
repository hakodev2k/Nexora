import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { automationRuns, automationRunGet, automationCancel, automationRetryStep, type AutomationRun, type AutomationRunDetail, type AutomationRunFilters, type AutomationExecutionPolicy } from './automationApi';

const emptyFilters: AutomationRunFilters = { state: '', version: '', createdFrom: '', createdBefore: '' };

export function AutomationRunPanel({ definitionId, canCancel, canRetry, onClose, onDenied }: { definitionId: string; canCancel: boolean; canRetry: boolean; onClose: () => void; onDenied: (e: unknown) => boolean }) {
  const generation = useRef(0), flight = useRef(false), cancelCommitted = useRef(false);
  const pending = useRef<{ id: string; etag: string; key: string; stepId?: number } | null>(null);
  const [filters, setFilters] = useState<AutomationRunFilters>(emptyFilters), [draft, setDraft] = useState<AutomationRunFilters>(emptyFilters);
  const [items, setItems] = useState<AutomationRun[]>([]), [cursor, setCursor] = useState<string | null>(null);
  const [detail, setDetail] = useState<AutomationRunDetail | null>(null), [busy, setBusy] = useState(false), [error, setError] = useState(''), [uncertain, setUncertain] = useState(false);
  const [policy, setPolicy] = useState<AutomationExecutionPolicy | null>(null);
  const states = ['Queued', 'Running', 'Succeeded', 'PartiallySucceeded', 'Failed', 'Cancelled', 'TimedOut', 'Skipped'];
  async function load(more = false) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const page = await automationRuns(definitionId, filters.state, more ? cursor ?? undefined : undefined, filters); if (stamp === generation.current) { setItems(old => more ? [...new Map([...old, ...page.items].map(r => [r.id, r])).values()] : page.items); setCursor(page.nextCursor); setPolicy(page.executionPolicy); } }
    catch (e) { if (stamp === generation.current && !onDenied(e)) { if ((e as { status?: number }).status === 404) { setItems([]); setDetail(null); setPolicy(null); } setError(e instanceof Error ? e.message : 'Không tải được runs.'); setCursor(null); } }
    finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  useEffect(() => { generation.current++; flight.current = false; setPolicy(null); setItems([]); setCursor(null); setDetail(null); void load(); return () => { generation.current++; }; }, [filters, definitionId]);
  function applyFilters(event: React.FormEvent) {
    event.preventDefault(); if (flight.current || uncertain) return;
    const next = { ...draft, version: draft.version.trim(), createdFrom: draft.createdFrom.trim(), createdBefore: draft.createdBefore.trim() };
    if (next.version && (!/^[1-9]\d{0,14}$/.test(next.version) || !Number.isSafeInteger(Number(next.version)))) { setError('Version phải là số nguyên dương.'); return; }
    const validInstant = (value: string) => !value || (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})$/.test(value) && Number.isFinite(Date.parse(value)));
    // SQL/server compares the full timestamp precision; JS Date truncates sub-millisecond fractions.
    if (!validInstant(next.createdFrom) || !validInstant(next.createdBefore)) { setError('Dùng ISO timestamp có Z/offset; server kiểm tra thứ tự các thời điểm.'); return; }
    setDraft(next); setFilters(next);
  }
  async function inspect(id: string) {
    if (flight.current) return; const stamp = generation.current; flight.current = true; setBusy(true); setError('');
    try { const value = await automationRunGet(id); if (stamp === generation.current) { if (value.run.definitionId !== definitionId) throw new Error('Run không thuộc definition này.'); setDetail(value); setPolicy(value.executionPolicy); } }
    catch (e) { if (stamp === generation.current && !onDenied(e)) { if ((e as { status?: number }).status === 404) { setDetail(null); setItems([]); setCursor(null); } setError(e instanceof Error ? e.message : 'Không tải được run.'); } }
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
      setDetail(current); setPolicy(current.executionPolicy); pending.current = null; cancelCommitted.current = false; setUncertain(false); setError('Command đã được nhận; trạng thái hiện tại hiển thị bên dưới. Task đã tạo được giữ nguyên.');
    } catch (e) {
      if (stamp !== generation.current || onDenied(e)) return;
      const status = (e as { status?: number }).status, code = (e as { code?: string }).code;
      if (status === 404) { pending.current = null; cancelCommitted.current = false; setUncertain(false); setDetail(null); setItems([]); setCursor(null); setError('Run hiện không còn khả dụng. Tải lại lịch sử.'); return; }
      if (cancelCommitted.current || status === undefined || status === 0 || status >= 500 || code === 'RequestInProgress' || code === 'ConcurrencyConflict') { setUncertain(true); setError('Chưa rõ kết quả. Retry giữ nguyên run/step/ETag/key.'); }
      else { pending.current = null; setUncertain(false); setError(e instanceof Error ? e.message : 'Hủy bị từ chối; tải lại revision.'); }
    } finally { if (stamp === generation.current) { flight.current = false; setBusy(false); } }
  }
  return <ResourceFormDialog open title={detail ? 'Run / step detail' : 'Run history'} busy={busy} dirty={uncertain} onClose={() => { if (flight.current || uncertain) setError('Retry cùng command trước khi đóng.'); else onClose(); }}>
    {policy && <p role="status">Local execution: {policy.localExecutionEnabled ? "operator đã bật" : "đang tắt"}. Giới hạn Running: hệ thống {policy.systemConcurrency}, mỗi PersonalSpace {policy.ownerConcurrency}, mỗi definition {policy.definitionConcurrency}. Tối đa {policy.maxStepAttempts} attempts/step (gồm lần đầu); retry exponential với jitter, base {policy.retryBaseSeconds}s/cap {policy.retryMaxSeconds}s. Run retry/backoff giữ slot; run Queued đợi capacity. Deadline mới kiểm tra giữa các bước (run đã bắt đầu giữ deadline đã lưu): {policy.runDeadlineSeconds}s từ lần bắt đầu đầu tiên. Run đợi ít nhất {policy.queueMaxAgeMinutes} phút bị từ chối khi worker nhận; không tự hoàn tác Task đang chạy. Quyền và lifecycle vẫn được kiểm tra trước mỗi effect.</p>}
    {error && <p role="alert">{error}</p>}
    {detail ? <section><h3>{detail.run.id}</h3><p>Version {detail.run.versionNumber} · {detail.run.state} · {detail.run.code}</p><p>{new Date(detail.run.createdAt).toLocaleString()} · {detail.run.startedAt ? new Date(detail.run.startedAt).toLocaleString() : 'Chưa bắt đầu'} · {detail.run.finishedAt ? new Date(detail.run.finishedAt).toLocaleString() : 'Chưa kết thúc'}</p>
      {detail.run.effectUncertain && <p role="alert">Có hiệu ứng chưa đối chiếu được. Kiểm tra Task trước khi tạo run mới; Retry terminal run hiện chưa khả dụng.</p>}
      {detail.failureNotice && <section aria-label="Final failure notification"><h4>Thông báo kết quả cuối</h4><p>Một logical alert trong inbox. Email/Push chưa có provider được duyệt.</p><div className="delivery-summary">{detail.failureNotice.deliveries.map(delivery => <span key={delivery.channel}>{delivery.channel}: {delivery.state}{delivery.lastErrorCode ? ` · ${delivery.lastErrorCode}` : ''}</span>)}</div></section>}
      <ol>{detail.steps.map(step => <li key={step.stepId}>Step {step.stepId} · {step.state} · {step.attempts} attempts · {step.code}{step.taskId && <p>Task ID: {step.taskId}</p>}{step.effectUncertain && <p>Hiệu ứng chưa rõ; không giả định rollback.</p>}</li>)}</ol>
      <button disabled={busy || uncertain} onClick={() => void inspect(detail.run.id)}>Tải lại run</button><button disabled={busy || uncertain} onClick={() => { setDetail(null); void load(); }}>Về run history</button>
      {(uncertain || (canCancel && ['Queued', 'Running'].includes(detail.run.state))) && <div><p>Hủy dừng các bước tiếp theo; Task đã tạo được giữ nguyên.</p><button disabled={busy} onClick={() => void cancel()}>{uncertain ? 'Retry cùng command' : 'Xác nhận hủy run'}</button></div>}
      {canRetry && !uncertain && !detail.run.effectUncertain && ["Failed", "PartiallySucceeded"].includes(detail.run.state) && <div><p>Chỉ retry failure đã biết chưa tạo effect. Giữ nguyên version, command key và attempts; các bước thành công không chạy lại. Các bước chưa chạy sau failure sẽ tiếp tục.</p>{detail.steps.filter(step => step.state === "Failed" && !step.effectUncertain && step.taskId === null && step.attempts > 0 && step.attempts < (policy?.maxStepAttempts ?? 1)).map(step => <button key={step.stepId} disabled={busy} onClick={() => void cancel(step.stepId)}>Xác nhận retry step {step.stepId} và các bước chưa chạy phía sau</button>)}</div>}
    </section> : <><form onSubmit={applyFilters} noValidate><fieldset disabled={busy || uncertain}><legend>Bộ lọc run history</legend><div className="form-grid"><label>State<select value={draft.state} onChange={e => setDraft({ ...draft, state: e.target.value })}><option value="">Tất cả</option>{states.map(value => <option key={value}>{value}</option>)}</select></label><label>Definition version<input value={draft.version} inputMode="numeric" maxLength={15} placeholder="Tất cả versions" onChange={e => setDraft({ ...draft, version: e.target.value })} /></label><label>Created từ (bao gồm)<input value={draft.createdFrom} maxLength={40} placeholder="2026-10-11T00:00:00+07:00" onChange={e => setDraft({ ...draft, createdFrom: e.target.value })} /></label><label>Created trước (không bao gồm)<input value={draft.createdBefore} maxLength={40} placeholder="2026-10-12T00:00:00+07:00" onChange={e => setDraft({ ...draft, createdBefore: e.target.value })} /></label></div><p>Timestamp có Z hoặc offset rõ ràng. Bộ lọc dùng thời điểm tạo run, không phải thời điểm bắt đầu/kết thúc.</p><button type="submit">Áp dụng bộ lọc</button><button type="button" onClick={() => { setDraft(emptyFilters); setFilters({ ...emptyFilters }); }}>Xóa bộ lọc</button></fieldset></form><p className="muted">Đang áp dụng: {filters.state || 'mọi state'} · version {filters.version || 'tất cả'} · {filters.createdFrom || 'không giới hạn từ'} → {filters.createdBefore || 'không giới hạn trước'}</p><button disabled={busy} onClick={() => void load()}>Tải lại</button>
      {busy && <p role="status">Đang tải…</p>}{!busy && !error && items.length === 0 && <p>Chưa có run khớp bộ lọc.</p>}{items.map(run => <article key={run.id}><p>{run.id} · version {run.versionNumber} · {run.state} · {new Date(run.createdAt).toLocaleString()}</p><button disabled={busy} onClick={() => void inspect(run.id)}>Xem run</button></article>)}{cursor && !error && <button disabled={busy} onClick={() => void load(true)}>Tải thêm 25 runs</button>}</>}
  </ResourceFormDialog>;
}
