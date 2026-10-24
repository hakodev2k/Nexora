import { useEffect, useRef, useState, type FormEvent } from 'react';
import { createIdempotencyKey, NexoraApiError } from './api';
import { ResourceFormDialog, useResourceDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard } from './navigationGuard';
import { learningPlan, learningPlans, learningPlanCapabilities, learningPlanSources, learningPlanIntent, sendLearningPlan,
  type LearningPlanDetail, type LearningPlanPage, type LearningPlanSources, type LearningPlanKind, type LearningPlanIntent } from './LearningPlanApi';

const empty = () => ({ title: '', startsOn: '', endsOn: '', notes: '' });
// Main shell MUST key this component by authenticated OwnerId + session identity.
type PlanNavigation = { planId?: string; onSelectPlan?: (id?: string) => void; onAuthLost?: () => void };
export function LearningPlanScreen({ ownerContext, ...navigation }: { ownerContext: string } & PlanNavigation) {
  return <LearningPlanOwnerScreen key={ownerContext} {...navigation} />;
}
function LearningPlanOwnerScreen({ planId, onSelectPlan, onAuthLost }: PlanNavigation) {
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [status, setStatus] = useState('Planned');
  const [page, setPage] = useState<LearningPlanPage | null>(null), [detail, setDetail] = useState<LearningPlanDetail | null>(null);
  const [choices, setChoices] = useState<LearningPlanSources | null>(null), [kind, setKind] = useState<LearningPlanKind>('Goal');
  const [selected, setSelected] = useState(''), [targetOn, setTargetOn] = useState('');
  const [draft, setDraft] = useState(empty), [editing, setEditing] = useState<string | null>(null), [editTag, setEditTag] = useState<string | null>(null);
  const dialog = useResourceDialog(draft, setDraft);
  const [pending, setPending] = useState<LearningPlanIntent | null>(null), [busy, setBusy] = useState(false), [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const [committedDestination, setCommittedDestination] = useState<string | null>(null);
  const alive = useRef(true), flight = useRef(false), generation = useRef(0), currentAbort = useRef<AbortController | null>(null);
  const pendingRef = useRef<LearningPlanIntent | null>(null), unresolved = useRef(false);
  pendingRef.current = pending;
  const allowed = (op: string) => caps[`learning.plan.${op}`] === true;
  const locked = busy || pending !== null || loading;
  const editable = detail !== null && detail.plan.status !== 'Archived';
  function failure(e: unknown) {
    if (!alive.current) return;
    // Revoke clears all private summaries and source choices immediately.
    if (e instanceof NexoraApiError && [401, 403, 404].includes(e.status)) { setDetail(null); setChoices(null); setPage(null); setCaps({}); setDraft(empty()); setEditing(null); setEditTag(null); setCommittedDestination(null); dialog.finish(); }
    setError(e instanceof NexoraApiError ? e.message : 'Không thể xác nhận kết quả.');
    if (e instanceof NexoraApiError && e.status === 401) onAuthLost?.();
  }
  async function list(nextStatus = status, cursor: string | null = null) {
    const version = ++generation.current; currentAbort.current?.abort(); const abort = new AbortController(); currentAbort.current = abort;
    setLoading(true); setPage(null); setDetail(null); setChoices(null); setSelected('');
    try {
      const permissions = await learningPlanCapabilities(abort.signal);
      const value = permissions['learning.plan.read'] ? await learningPlans(nextStatus, cursor, abort.signal) : null;
      if (alive.current && version === generation.current) { setCaps(permissions); setPage(value); }
    } catch (e) { if (alive.current && version === generation.current && !abort.signal.aborted) failure(e); }
    finally { if (alive.current && version === generation.current) setLoading(false); }
  }
  async function open(id: string) {
    const version = ++generation.current; setLoading(true); setDetail(null); setChoices(null); setSelected(''); setError(null);
    try { const value = await learningPlan(id); if (alive.current && version === generation.current) setDetail(value); }
    catch (e) { if (alive.current && version === generation.current) failure(e); }
    finally { if (alive.current && version === generation.current) setLoading(false); }
  }
  useEffect(() => {
    alive.current = true;
    if (!planId) void list();
    else {
      const version = ++generation.current;
      currentAbort.current?.abort(); const abort = new AbortController(); currentAbort.current = abort;
      setLoading(true); setPage(null); setDetail(null); setChoices(null); setError(null); setSelected('');
      void (async () => {
        try {
          const permissions = await learningPlanCapabilities(abort.signal);
          const value = permissions['learning.plan.read'] ? await learningPlan(planId, abort.signal) : null;
          if (alive.current && version === generation.current) { setCaps(permissions); setDetail(value); }
        } catch (e) { if (alive.current && version === generation.current && !abort.signal.aborted) failure(e); }
        finally { if (alive.current && version === generation.current) setLoading(false); }
      })();
    }
    return () => { alive.current = false; ++generation.current; currentAbort.current?.abort(); };
  }, [planId]);
  useEffect(() => registerDirtyLeaveGuard(() => pendingRef.current !== null || flight.current, -110), []);
  // Navigate only after the confirmed write and modal have settled. The normal
  // Shell guard remains active for every uncertain or still-busy operation.
  useEffect(() => {
    if (!committedDestination || busy || dialog.open || flight.current || pendingRef.current || !onSelectPlan) return;
    setCommittedDestination(null);
    onSelectPlan(committedDestination);
  }, [committedDestination, busy, dialog.open, onSelectPlan]);
  useEffect(() => {
    function leave(e: BeforeUnloadEvent) { if (pendingRef.current !== null || flight.current) { e.preventDefault(); e.returnValue = ''; } }
    window.addEventListener('beforeunload', leave); return () => window.removeEventListener('beforeunload', leave);
  }, []);
  async function send(intent: LearningPlanIntent) {
    if (flight.current) return;
    flight.current = true; setBusy(true); setError(null); setNotice(null);
    let committed = false;
    try {
      const ack = await sendLearningPlan(intent); committed = true;
      if (!alive.current) return;
      setPending(null); pendingRef.current = null; unresolved.current = false; dialog.finish(); setChoices(null); setSelected(''); setDetail(null); setPage(null);
      setNotice('Đã nhận xác nhận lưu. Đang kiểm tra quyền đọc hiện tại.');
      const permissions = await learningPlanCapabilities();
      if (!alive.current) return;
      setCaps(permissions);
      if (permissions['learning.plan.read']) {
        if (onSelectPlan && ack.id !== planId) setCommittedDestination(ack.id);
        else await open(ack.id);
      }
      else setNotice('Đã lưu; hiện không có quyền xem chi tiết.');
    } catch (e) {
      if (!alive.current) return;
      // A valid write ACK is final; a subsequent read failure cannot make it uncertain.
      if (committed) { failure(e); return; }
      // A received domain 4xx is definitive; network/5xx/protocol uncertainty retains the frozen intent.
      const definitive = e instanceof NexoraApiError && e.status >= 400 && e.status < 500 && e.code !== 'RequestInProgress';
      if (definitive && !unresolved.current) { setPending(null); pendingRef.current = null; }
      else unresolved.current = true;
      failure(e);
    } finally { flight.current = false; if (alive.current) setBusy(false); }
  }
  function command(path: string, method: LearningPlanIntent['method'], etag: string | null, body?: unknown) {
    if (locked || flight.current) return;
    const intent = learningPlanIntent(path, method, createIdempotencyKey(), etag, body);
    unresolved.current = false; setPending(intent); pendingRef.current = intent; void send(intent);
  }
  function begin(edit = false) {
    if (locked) return;
    const value = edit && detail ? { title: detail.plan.title, startsOn: detail.plan.startsOn ?? '', endsOn: detail.plan.endsOn ?? '', notes: detail.plan.notes ?? '' } : empty();
    setEditing(edit && detail ? detail.plan.id : null); setEditTag(edit && detail ? detail.plan.etag : null); setDraft(value); dialog.begin(value);
  }
  function save(e: FormEvent) {
    e.preventDefault(); if (!draft.title.trim() || draft.title.trim().length > 200 || (draft.startsOn && draft.endsOn && draft.startsOn > draft.endsOn)) return;
    command(editing ? `/${editing}` : '', editing ? 'PUT' : 'POST', editTag,
      { title: draft.title.trim(), startsOn: draft.startsOn || null, endsOn: draft.endsOn || null, notes: draft.notes || null });
  }
  async function pick(nextKind: LearningPlanKind, cursor: string | null = null) {
    if (locked) return; setKind(nextKind); setChoices(null); setSelected(''); setLoading(true); setError(null);
    const version = ++generation.current;
    try { const value = await learningPlanSources(nextKind, cursor); if (alive.current && version === generation.current) setChoices(value); }
    catch (e) { if (alive.current && version === generation.current) failure(e); }
    finally { if (alive.current && version === generation.current) setLoading(false); }
  }
  function move(index: number, direction: number) {
    if (!detail) return; const ids = detail.items.map(i => i.id); const other = index + direction;
    [ids[index], ids[other]] = [ids[other], ids[index]];
    command(`/${detail.plan.id}/order`, 'PUT', detail.plan.etag, { itemIds: ids });
  }
  return <section aria-labelledby="learning-plan-title">
    <h1 id="learning-plan-title">Kế hoạch học tập</h1>
    {planId && onSelectPlan && <button type="button" disabled={locked} onClick={() => onSelectPlan()}>Về danh sách kế hoạch</button>}
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {pending && <div role="alert"><p>Kết quả chưa xác nhận. Giữ nguyên yêu cầu và mã gửi lại.</p>
      <button disabled={busy} onClick={() => void send(pending)}>Gửi lại yêu cầu gốc</button></div>}
    <label>Trạng thái <select disabled={locked || Boolean(planId)} value={planId && detail ? detail.plan.status : status} onChange={e => { setStatus(e.target.value); void list(e.target.value); }}>
      {['Planned', 'Active', 'Completed', 'Archived'].map(s => <option key={s}>{s}</option>)}</select></label>
    <button disabled={locked || !allowed('create')} onClick={() => begin()}>Tạo kế hoạch</button>
    <button disabled={locked} onClick={() => planId ? void open(planId) : void list()}>{planId ? 'Tải lại kế hoạch' : 'Tải lại danh sách'}</button>
    {loading && <p role="status">Đang tải…</p>}
    {page && <><ul>{page.items.map(p => <li key={p.id}><button disabled={locked} onClick={() => onSelectPlan ? onSelectPlan(p.id) : void open(p.id)}>{p.title}</button> {p.status}</li>)}</ul>
      {page.items.length === 0 && <p>Không có kế hoạch trong bộ lọc này.</p>}
      {page.nextCursor && <button disabled={locked} onClick={() => void list(status, page.nextCursor)}>Trang tiếp</button>}</>}
    {detail && <article><h2>{detail.plan.title}</h2><p>{detail.plan.status}</p><p>{detail.progressLabel}</p><p>{detail.plan.notes}</p>
      <p>{detail.plan.startsOn} — {detail.plan.endsOn}</p>
      <button disabled={locked || !editable || !allowed('update')} onClick={() => begin(true)}>Sửa kế hoạch</button>
      <ol>{detail.items.map((item, index) => <li key={item.id}>
        <span>{item.source.kind}: {item.projection.status === 'Unavailable' ? 'Không khả dụng' : item.projection.title}</span>
        <span> {item.targetOn} — {item.projection.status === 'Unavailable' ? 'Chưa biết tiến độ' : 'Không có chỉ số tiến độ trong kế hoạch'}</span>
        <button aria-label={`Lên mục ${index + 1}`} disabled={locked || !editable || !allowed('reorder') || index === 0} onClick={() => move(index, -1)}>Lên</button>
        <button aria-label={`Xuống mục ${index + 1}`} disabled={locked || !editable || !allowed('reorder') || index === detail.items.length - 1} onClick={() => move(index, 1)}>Xuống</button>
        <button disabled={locked || !editable || !allowed('unlink')} onClick={() => command(`/${detail.plan.id}/items/${item.id}`, 'DELETE', detail.plan.etag)}>Bỏ liên kết</button>
      </li>)}</ol>
      {detail.items.length === 0 && <p>Chưa có mục; không tính là hoàn tất.</p>}
      <fieldset disabled={locked || !editable || !allowed('link')}><legend>Liên kết nguồn hiện có</legend>
        <label>Loại nguồn <select value={kind} onChange={e => void pick(e.target.value as LearningPlanKind)}>
          {(['Goal', 'Task', 'Course'] as const).map(k => <option key={k}>{k}</option>)}</select></label>
        <button onClick={() => void pick(kind)}>Tải nguồn được phép</button>
        {choices && <><label>Nguồn <select value={selected} onChange={e => setSelected(e.target.value)}><option value="">Chọn nguồn</option>
          {choices.items.map(choice => <option key={choice.source.id} value={choice.source.id}>{choice.title}</option>)}</select></label>
          {choices.items.length === 0 && <p>Không có nguồn phù hợp.</p>}
          {choices.nextCursor && <button onClick={() => void pick(kind, choices.nextCursor)}>Trang nguồn tiếp</button>}</>}
        <label>Ngày mục tiêu <input type="date" value={targetOn} onChange={e => setTargetOn(e.target.value)} /></label>
        <button disabled={!selected || locked} onClick={() => command(`/${detail.plan.id}/items`, 'POST', detail.plan.etag, { source: { kind, id: selected }, targetOn: targetOn || null })}>Liên kết</button>
        <p>Ngày mục tiêu không đổi lịch của nguồn.</p>
      </fieldset>
      <button disabled={locked || !editable || !allowed('complete') || detail.plan.status === 'Completed'} onClick={() => {
        if (window.confirm(`Hoàn tất kế hoạch? ${detail.progressLabel}. Không đổi trạng thái nguồn.`)) command(`/${detail.plan.id}/complete`, 'POST', detail.plan.etag, { confirm: true });
      }}>Xác nhận hoàn tất</button>
    </article>}
    <ResourceFormDialog open={dialog.open} title={editing ? 'Sửa kế hoạch' : 'Tạo kế hoạch'} busy={busy} dirty={dialog.dirty} closeBlocked={pending !== null} onClose={dialog.cancel}>
      {pending && <div role="alert"><p>Kết quả chưa xác nhận; chỉ gửi lại yêu cầu gốc.</p><button type="button" disabled={busy} onClick={() => void send(pending)}>Gửi lại yêu cầu gốc</button></div>}
      {error && <p role="alert">{error}</p>}
      <form onSubmit={save}><label>Tên <input required maxLength={200} value={draft.title} onChange={e => setDraft({ ...draft, title: e.target.value })} /></label>
        <label>Bắt đầu <input type="date" value={draft.startsOn} onChange={e => setDraft({ ...draft, startsOn: e.target.value })} /></label>
        <label>Kết thúc <input type="date" min={draft.startsOn || undefined} value={draft.endsOn} onChange={e => setDraft({ ...draft, endsOn: e.target.value })} /></label>
        <label>Ghi chú <textarea maxLength={20000} value={draft.notes} onChange={e => setDraft({ ...draft, notes: e.target.value })} /></label>
        <button type="submit" disabled={locked}>Lưu kế hoạch</button></form>
    </ResourceFormDialog>
  </section>;
}
