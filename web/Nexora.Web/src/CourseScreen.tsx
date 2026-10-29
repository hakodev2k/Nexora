import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog, registerDirtyLeaveGuard } from './App';
import { ResourceFormDialog } from './ResourceFormDialog';
import { NexoraApiError } from './api';
import { courseCapabilities, courseGet, courseList, courseMilestones, coursePreview, courseProgress, courseSave, courseTransition,
  fractionToPercent, milestoneAdd, milestoneChange, percentToFraction, type Course, type CourseOperation, type CoursePreview, type Milestone } from './courseApi';

type Draft = { title: string; provider: string; url: string; notes: string; progressMode: string; startedOn: string; percent: string; start: boolean; milestoneTitle: string };
type Editor = { item?: Course; child?: Milestone; mode: 'metadata' | 'progress' | 'milestone'; baseline: string };
type CourseWriteIntent = Readonly<{ send: () => Promise<unknown>; refresh?: Course; closeEditor?: boolean; clearPane?: boolean }>;
const empty: Draft = { title: '', provider: '', url: '', notes: '', progressMode: '', startedOn: '', percent: '', start: false, milestoneTitle: '' };
const names: Record<CourseOperation, string> = { complete: 'Complete Course', abandon: 'Abandon Course', archive: 'Archive', unarchive: 'Unarchive', trash: 'Move to Trash', restore: 'Restore', purge: 'Delete permanently' };
const text = (e: unknown) => e instanceof Error ? e.message : 'The operation could not be completed.';
const toDraft = (item?: Course, child?: Milestone): Draft => item ? { title: item.title, provider: item.provider ?? '', url: item.url ?? '', notes: item.notes ?? '', progressMode: item.progressMode,
  startedOn: item.startedOn ?? '', percent: fractionToPercent(item.manualProgress), start: false, milestoneTitle: child?.title ?? '' } : empty;

type CourseNavigation = { courseId?: string; onOpenCourse?: (id: string) => void; onBackToCourses?: () => void };
export function CourseScreen({ onAuthLost, courseId, onOpenCourse, onBackToCourses }: { onAuthLost: () => void } & CourseNavigation) {
  const prefix = useId(); const [caps, setCaps] = useState<Record<string, boolean>>({}); const [items, setItems] = useState<Course[]>([]);
  const [filter, setFilter] = useState({ status: 'Planned', query: '' }); const [query, setQuery] = useState(''); const [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState<string | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const [editor, setEditor] = useState<Editor | null>(null); const [draft, setDraft] = useState<Draft>(empty);
  const [conflict, setConflict] = useState(false); const [current, setCurrent] = useState<Course | null>(null); const [currentChild, setCurrentChild] = useState<Milestone | null>(null);
  const [pane, setPane] = useState<{ item: Course; children: Milestone[]; cursor: string | null } | null>(null);
  const [pending, setPending] = useState<{ preview: CoursePreview; title: string } | null>(null); const [completeDate, setCompleteDate] = useState('');
  const [remove, setRemove] = useState<{ item: Course; child: Milestone } | null>(null); const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const [uncertain, setUncertain] = useState(false), [abandon, setAbandon] = useState(false), [notice, setNotice] = useState<string | null>(null);
  const originalWrite = useRef<CourseWriteIntent | null>(null), unresolved = useRef(false);
  const writeError = useRef<unknown>(null);
  const sequence = useRef(0); const inFlight = useRef(false); const key = useRef(crypto.randomUUID());
  const alive = useRef(false), readAbort = useRef<AbortController | null>(null);
  const capsRef = useRef(caps); capsRef.current = caps;
  const authLost = useRef(onAuthLost); authLost.current = onAuthLost;
  const fail = useCallback((e: unknown) => {
    if (!alive.current) return;
    setError(text(e)); const status = (e as { status?: number }).status;
    if ([401, 403, 404].includes(status ?? 0)) { sequence.current++; readAbort.current?.abort(); capsRef.current = {}; setLoading(false); setItems([]); setCaps({}); setCursor(null); setEditor(null); setDraft(empty); setPane(null); setPending(null); setRemove(null); setLeave(null); setConflict(false); setCurrent(null); setCurrentChild(null); setFormError(null); if (status === 401) authLost.current(); }
  }, []);
  const load = useCallback(async () => {
    if (!alive.current) return false;
    const stamp = ++sequence.current; setLoading(true); setError(null);
    readAbort.current?.abort(); const abort = new AbortController(); readAbort.current = abort;
    if (courseId) setPane(null);
    try { const allowed = await courseCapabilities(abort.signal);
      if (stamp !== sequence.current || !alive.current) return false;
      capsRef.current = allowed; setCaps(allowed);
      const parent = allowed['learning.course.read'] && courseId ? await courseGet(courseId, abort.signal) : null;
      if (stamp !== sequence.current || !alive.current) return false;
      const page = allowed['learning.course.read'] && !courseId ? await courseList(filter.status, filter.query, undefined, abort.signal) : null;
      if (stamp !== sequence.current || !alive.current) return false;
      const milestones = parent ? await courseMilestones(parent, undefined, abort.signal) : null;
      if (stamp !== sequence.current || !alive.current) return false;
      setItems(page?.items ?? []); setCursor(page?.nextCursor ?? null);
      if (parent && milestones) setPane({ item: parent, children: milestones.items, cursor: milestones.nextCursor });
      if (!allowed['learning.course.read']) { setEditor(null); setDraft(empty); setPane(null); setPending(null); setRemove(null); setLeave(null); setConflict(false); setCurrent(null); setCurrentChild(null); }
      return allowed['learning.course.read'] === true;
    } catch (e) { if (stamp === sequence.current && !abort.signal.aborted) fail(e); return false; } finally { if (stamp === sequence.current && alive.current) setLoading(false); }
  }, [filter, fail, courseId]);
  useEffect(() => { alive.current = true; void load(); return () => { alive.current = false; sequence.current++; readAbort.current?.abort(); }; }, [load]);
  const dirty = editor !== null && editor.baseline !== JSON.stringify(draft);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (originalWrite.current || inFlight.current) return true; if (!dirty && !busy) return false; setLeave({ proceed }); return true; }), [dirty, busy]);
  useEffect(() => { if (!dirty && !busy && !uncertain) return; const prevent = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ''; }; window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent); }, [dirty, busy, uncertain]);
  const can = (action: string) => caps['learning.course.' + action] === true;
  function begin(item?: Course, mode: Editor['mode'] = 'metadata', child?: Milestone) {
    const next = toDraft(item, child); setDraft(next); setEditor({ item, child, mode, baseline: JSON.stringify(next) }); setConflict(false); setCurrent(null); setCurrentChild(null); setFormError(null); key.current = crypto.randomUUID();
  }
  async function refreshPane(item: Course, append = false) {
    if (!alive.current || !capsRef.current['learning.course.read']) return;
    const stamp = sequence.current;
    const parent = await courseGet(item.id);
    if (!alive.current || stamp !== sequence.current || !capsRef.current['learning.course.read']) return;
    const page = await courseMilestones(parent, append ? pane?.cursor ?? undefined : undefined);
    if (!alive.current || stamp !== sequence.current || !capsRef.current['learning.course.read']) return;
    setPane(old => ({ item: parent, children: append && old?.item.id === parent.id ? [...old.children, ...page.items] : page.items, cursor: page.nextCursor }));
  }
  async function openPane(item: Course) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true); try { await refreshPane(item); } catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function executeWrite(intent: CourseWriteIntent) {
    if (inFlight.current || (originalWrite.current && originalWrite.current !== intent)) return false;
    originalWrite.current ??= Object.freeze(intent); inFlight.current = true; setBusy(true); setFormError(null); setError(null); setNotice(null); writeError.current = null;
    let committed = false;
    try {
      await intent.send(); committed = true;
      if (!alive.current) return true;
      originalWrite.current = null; unresolved.current = false; setUncertain(false);
      if (intent.closeEditor) { setEditor(null); setDraft(empty); setCurrent(null); setCurrentChild(null); }
      if (intent.clearPane) setPane(null);
      setPending(null); setRemove(null); setNotice('Đã nhận xác nhận lưu.');
      const readable = await load();
      if (readable && !courseId && intent.refresh) await refreshPane(intent.refresh);
      return true;
    } catch (e) {
      if (!alive.current) return committed;
      writeError.current = e;
      if (committed) { fail(e); return true; }
      const status = e instanceof NexoraApiError ? e.status : 0;
      const unknown = status === 0 || status >= 500 || status === 408 ||
        (e instanceof NexoraApiError && ['RequestInProgress', 'IdempotencyConflict', 'UnexpectedAcknowledgement'].includes(e.code ?? ''));
      if (unknown || unresolved.current) { unresolved.current = true; setUncertain(true); }
      else { originalWrite.current = null; setUncertain(false); }
      setFormError(text(e));
      if ([401, 403, 404].includes(status)) fail(e);
      return false;
    } finally { inFlight.current = false; if (alive.current) setBusy(false); }
  }
  async function persist() {
    if (!editor || conflict || inFlight.current || originalWrite.current) return false;
    const item = editor.item ? { ...editor.item } : undefined, child = editor.child ? { ...editor.child } : undefined;
    const requestKey = key.current;
    try {
      let send: CourseWriteIntent['send'];
      if (editor.mode === 'progress' && item) {
        const body = { ...(item.progressMode === 'ManualPercent' ? { manualProgress: percentToFraction(draft.percent) } : {}), start: draft.start, ...(draft.start ? { startedOn: draft.startedOn } : {}) };
        send = () => courseProgress(item, body, requestKey);
      } else if (editor.mode === 'milestone' && item) {
        const title = draft.milestoneTitle;
        send = child ? () => milestoneChange(item, child, 'update', { title }, requestKey) : () => milestoneAdd(item, title, requestKey);
      } else {
        const body = { title: draft.title, provider: draft.provider || null, url: draft.url || null, notes: draft.notes || null, progressMode: draft.progressMode, startedOn: draft.startedOn || null };
        send = () => courseSave(body, requestKey, item);
      }
      const ok = await executeWrite({ send, closeEditor: true, refresh: item && pane?.item.id === item.id ? item : undefined });
      if (!ok && !unresolved.current && (writeError.current as { status?: number })?.status === 412 && item && alive.current) {
        setConflict(true); try { const latest = await courseGet(item.id); setCurrent(latest); if (child) setCurrentChild(await findChild(latest, child.id)); } catch (e) { fail(e); }
      }
      return ok;
    } catch (e) { setFormError(text(e)); return false; }
  }
  async function findChild(parent: Course, id: string) {
    const stamp = sequence.current;
    let after: string | undefined;
    const seen = new Set<string>();
    do {
      if (!alive.current || stamp !== sequence.current) return null;
      const page = await courseMilestones(parent, after);
      if (!alive.current || stamp !== sequence.current) return null;
      const child = page.items.find(c => c.id === id); if (child) return child;
      after = page.nextCursor ?? undefined;
      if (after && seen.has(after)) throw new Error('Không thể xác nhận trang milestone tiếp theo. Hãy tải lại.');
      if (after) seen.add(after);
    } while (after);
    return null;
  }
  async function resolveConflict(reapply: boolean) {
    if (!editor?.item || inFlight.current) return; inFlight.current = true; setBusy(true);
    try {
      const latest = await courseGet(editor.item.id); const child = editor.child ? await findChild(latest, editor.child.id) : undefined;
      if (latest.status === 'Archived' || latest.status === 'Trash' || (editor.child && !child)) { setFormError('The course or milestone is no longer editable. Cancel the draft.'); return; }
      if (reapply && (latest.etag !== current?.etag || (editor.child && child?.etag !== currentChild?.etag))) { setCurrent(latest); setCurrentChild(child ?? null); setFormError('The revision changed again. Compare the latest version before reapplying.'); return; }
      const next = toDraft(latest, child ?? undefined); setEditor({ ...editor, item: latest, child: child ?? undefined, baseline: JSON.stringify(next) }); if (!reapply) setDraft(next);
      setConflict(false); setCurrent(null); setCurrentChild(null); setFormError(reapply ? 'Current revision loaded. Review the draft and Save to apply it.' : null); key.current = crypto.randomUUID();
    } catch (e) { if ((e as { status?: number }).status === 404) { setEditor(null); setDraft(empty); } fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function preview(item: Course, operation: CourseOperation) {
    if (inFlight.current) return; inFlight.current = true; setBusy(true);
    try { const result = await coursePreview(item, operation); key.current = crypto.randomUUID(); setCompleteDate(''); setPending({ preview: result, title: item.title }); }
    catch (e) { fail(e); } finally { inFlight.current = false; setBusy(false); }
  }
  async function commit() {
    if (!pending || inFlight.current || originalWrite.current) return false;
    const preview = { ...pending.preview }, requestKey = key.current, date = preview.operation === 'complete' ? completeDate : undefined;
    const ok = await executeWrite({ send: () => courseTransition(preview, requestKey, date), clearPane: true });
    if (!ok && !unresolved.current && (writeError.current as { status?: number })?.status === 412) { setPending(null); await load(); }
    return ok ? true as const : { error: text(writeError.current) };
  }
  async function childAction(item: Course, child: Milestone, operation: 'completion' | 'move' | 'delete', body: object) {
    if (inFlight.current || originalWrite.current) return false;
    const parent = { ...item }, milestone = { ...child }, payload = structuredClone(body), requestKey = key.current;
    const ok = await executeWrite({ send: () => milestoneChange(parent, milestone, operation, payload, requestKey), refresh: parent });
    if (!ok && !unresolved.current && (writeError.current as { status?: number })?.status === 412) { try { await refreshPane(parent); } catch (e) { fail(e); } }
    return ok ? true as const : { error: text(writeError.current) };
  }
  const blocked = loading || busy || uncertain;
  function field(field: keyof Draft, value: string | boolean) { key.current = crypto.randomUUID(); setDraft(old => ({ ...old, [field]: value })); }
  const editablePane = pane && !['Archived', 'Trash'].includes(pane.item.status);
  return <section className="content-section" aria-labelledby="course-title">
    <div className="content-heading"><div><p className="eyebrow">FX40 / LEARNING</p><h1 id="course-title">Courses</h1><p>Personal course tracking with explicit progress and completion.</p></div><div className="resource-actions">{courseId && onBackToCourses && <button className="secondary-button" disabled={busy} onClick={onBackToCourses}>Về danh sách khóa học</button>}<button disabled={blocked} onClick={() => { setPane(null); void load(); }}>Tải lại</button></div></div>
    {error && <p role="alert">{error}</p>}{loading && <p role="status">Loading…</p>}
    {notice && <p role="status">{notice}</p>}
    {uncertain && <div role="alert"><p>Chưa xác nhận được kết quả. Yêu cầu gốc được giữ nguyên để tránh gửi trùng.</p>
      <button type="button" disabled={busy} onClick={() => { if (originalWrite.current) void executeWrite(originalWrite.current); }}>Thử lại yêu cầu gốc</button>
      <button type="button" disabled={busy} onClick={() => setAbandon(true)}>Dừng đối chiếu</button></div>}
    {!loading && !error && !can('read') && <p role="status">Không có quyền xem khóa học trong ngữ cảnh hiện tại.</p>}
    <button className="primary-button" disabled={!can('create') || blocked} onClick={() => begin()}>New Course</button>
    {can('read') && <>
      {!courseId && <form className="resource-form" onSubmit={e => { e.preventDefault(); setPane(null); setFilter(old => ({ ...old, query })); }}>
        <div><label htmlFor={`${prefix}-search`}>Title search</label><input id={`${prefix}-search`} value={query} maxLength={200} onChange={e => setQuery(e.target.value)} /></div><button disabled={blocked}>Search Course</button>
        <div><label htmlFor={`${prefix}-status`}>Status</label><select id={`${prefix}-status`} value={filter.status} disabled={blocked} onChange={e => { setPane(null); setFilter(old => ({ ...old, status: e.target.value })); }}>{['Planned', 'InProgress', 'Completed', 'Abandoned', 'Archived', 'Trash'].map(s => <option key={s}>{s}</option>)}</select></div>
        <button type="button" disabled={blocked} onClick={() => { setPane(null); setQuery(''); setFilter({ status: 'Planned', query: '' }); }}>Clear filters</button>
      </form>}
      {!courseId && !loading && !error && items.length === 0 && <p>No courses in this selection.</p>}
      <div className="resource-cards">{items.map(item => {
        const active = !['Archived', 'Trash'].includes(item.status);
        const operations: CourseOperation[] = item.status === 'Trash' ? ['restore', 'purge'] : item.status === 'Archived' ? ['unarchive', 'trash'] : ['Planned', 'InProgress'].includes(item.status) ? ['complete', 'abandon', 'archive', 'trash'] : ['archive', 'trash'];
        return <article key={item.id} className="resource-card"><h2>{item.title}</h2><p>{item.status} · {item.progressMode === 'ManualPercent' ? item.manualProgress === null ? 'Progress not recorded' : `${fractionToPercent(item.manualProgress)}%` : item.milestonesTotal === 0 ? 'No Milestones' : `${item.milestonesDone}/${item.milestonesTotal} milestones`}</p>
          {item.provider && <p>Provider: {item.provider}</p>}{item.startedOn && <p>Started: {item.startedOn}</p>}{item.completedOn && <p>Completed: {item.completedOn}</p>}{item.notes && <p>{item.notes}</p>}
          <div className="resource-actions">{active && <><button disabled={!can('update') || blocked} onClick={() => begin(item)}>Edit Course</button><button disabled={!can('progress') || blocked} onClick={() => begin(item, 'progress')}>Record progress</button></>}
            <button disabled={blocked} onClick={() => onOpenCourse ? onOpenCourse(item.id) : void openPane(item)}>{onOpenCourse ? 'Chi tiết khóa học' : 'Milestones'}</button>{operations.map(op => <button key={op} disabled={!can(op) || blocked} onClick={() => void preview(item, op)}>{names[op]}</button>)}</div></article>;
      })}</div>
      {cursor && <button disabled={blocked} onClick={async () => { const stamp = sequence.current; setLoading(true); try { const page = await courseList(filter.status, filter.query, cursor); if (stamp === sequence.current) { setItems(old => [...old, ...page.items]); setCursor(page.nextCursor); } } catch (e) { if (stamp === sequence.current) fail(e); } finally { if (stamp === sequence.current) setLoading(false); } }}>Load more Course items</button>}
    </>}
    {pane && <section aria-label="Course milestones"><h2>{pane.item.title} — Milestones</h2>{!courseId && <button disabled={blocked} onClick={() => setPane(null)}>Close milestones</button>}
      {courseId && <article className="resource-card"><p>{pane.item.status} · {pane.item.progressMode === 'ManualPercent' ? pane.item.manualProgress === null ? 'Chưa ghi nhận tiến độ' : `${fractionToPercent(pane.item.manualProgress)}%` : pane.item.milestonesTotal === 0 ? 'No Milestones' : `${pane.item.milestonesDone}/${pane.item.milestonesTotal} milestones`}</p>
        {pane.item.provider && <p>{pane.item.provider}</p>}{pane.item.url && <p style={{ overflowWrap: 'anywhere' }}>{pane.item.url}</p>}{pane.item.notes && <p>{pane.item.notes}</p>}
        <p>{pane.item.startedOn ?? 'Chưa bắt đầu'} — {pane.item.completedOn ?? 'Chưa hoàn tất'}</p>
        <div className="resource-actions">{editablePane && <><button disabled={!can('update') || blocked} onClick={() => begin(pane.item)}>Edit Course</button><button disabled={!can('progress') || blocked} onClick={() => begin(pane.item, 'progress')}>Record progress</button></>}
          {(pane.item.status === 'Trash' ? ['restore', 'purge'] : pane.item.status === 'Archived' ? ['unarchive', 'trash'] : ['Planned', 'InProgress'].includes(pane.item.status) ? ['complete', 'abandon', 'archive', 'trash'] : ['archive', 'trash']).map(op => <button key={op} disabled={!can(op) || blocked} onClick={() => void preview(pane.item, op as CourseOperation)}>{names[op as CourseOperation]}</button>)}
        </div></article>}
      <button disabled={!editablePane || !can('milestone') || blocked} onClick={() => begin(pane.item, 'milestone')}>Add milestone</button>
      {pane.children.length === 0 && <p>No Milestones</p>}{pane.children.map(child => <article className="resource-card" key={child.id}><h3>{child.title}</h3><p>{child.completed ? 'Done' : 'Pending'}</p><div className="resource-actions">
        <button disabled={!editablePane || !can('milestone') || blocked} onClick={() => begin(pane.item, 'milestone', child)}>Edit milestone</button>
        <button disabled={!editablePane || pane.item.progressMode !== 'Milestones' || !can('progress') || blocked} onClick={() => { key.current = crypto.randomUUID(); void childAction(pane.item, child, 'completion', { completed: !child.completed }); }}>{child.completed ? 'Undo milestone' : 'Complete milestone'}</button>
        {(['up', 'down'] as const).map(direction => <button key={direction} disabled={!editablePane || !can('milestone') || blocked} onClick={() => { key.current = crypto.randomUUID(); void childAction(pane.item, child, 'move', { direction }); }}>Move {direction}</button>)}
        <button disabled={!editablePane || !can('milestone') || (child.completed && !can('progress')) || blocked} onClick={() => { key.current = crypto.randomUUID(); setRemove({ item: pane.item, child }); }}>Remove milestone</button>
      </div></article>)}{pane.cursor && <button disabled={blocked} onClick={() => { if (inFlight.current) return; inFlight.current = true; setBusy(true); void refreshPane(pane.item, true).catch(fail).finally(() => { inFlight.current = false; setBusy(false); }); }}>Load more milestones</button>}
    </section>}
    <ResourceFormDialog open={editor !== null && leave === null && !uncertain} title={editor?.mode === 'progress' ? 'Record Course progress' : editor?.mode === 'milestone' ? editor.child ? 'Edit milestone' : 'Add milestone' : editor?.item ? 'Edit Course' : 'New Course'} busy={busy} dirty={dirty} onClose={() => { setEditor(null); setDraft(empty); }}>
      <form className="resource-form" onSubmit={(e: FormEvent) => { e.preventDefault(); void persist(); }}>{formError && <p role="alert">{formError}</p>}
        {conflict && <div><p>The revision changed. Compare current values, then reload or reapply your draft.</p>{current && <div aria-label="Current Course version"><p>{current.status} · {current.etag} · {new Date(current.updatedAt).toLocaleString()}</p><dl>{(['title', 'provider', 'url', 'notes', 'progressMode', 'startedOn', 'completedOn'] as const).map(name => <div key={name}><dt>{name}</dt><dd>{current[name] ?? '—'}</dd></div>)}<div><dt>Current progress percent</dt><dd>{current.manualProgress === null ? 'Not recorded' : fractionToPercent(current.manualProgress)}</dd></div><div><dt>Milestones</dt><dd>{current.milestonesDone}/{current.milestonesTotal}</dd></div></dl>{currentChild && <p>{currentChild.title} · {currentChild.etag}</p>}</div>}<button type="button" disabled={busy} onClick={() => void resolveConflict(false)}>Reload current</button><button type="button" disabled={busy || !current} onClick={() => void resolveConflict(true)}>Reapply draft</button></div>}
        {editor?.mode === 'metadata' && <>
          <div><label htmlFor={`${prefix}-mode`}>Progress mode</label><select id={`${prefix}-mode`} required disabled={editor.item?.progressModeLocked} value={draft.progressMode} onChange={e => field('progressMode', e.target.value)}><option value="">Choose a progress mode</option><option value="ManualPercent">Manual percent</option><option value="Milestones">Milestones</option></select>{editor.item?.progressModeLocked && <p>Mode is fixed after recorded progress.</p>}</div>
          {(['title', 'provider', 'url', 'notes', 'startedOn'] as const).map((name, i) => <div key={name}><label htmlFor={`${prefix}-${name}`}>{['Title', 'Provider', 'URL', 'Notes', 'Actual start date'][i]}</label>{name === 'notes' ? <textarea id={`${prefix}-${name}`} maxLength={20000} value={draft[name]} onChange={e => field(name, e.target.value)} /> : <input id={`${prefix}-${name}`} required={name === 'title'} type={name === 'startedOn' ? 'date' : name === 'url' ? 'url' : 'text'} maxLength={name === 'url' ? 2048 : 200} value={draft[name]} onChange={e => field(name, e.target.value)} />}</div>)}
        </>}
        {editor?.mode === 'progress' && <>
          {editor.item?.progressMode === 'ManualPercent' ? <div><label htmlFor={`${prefix}-percent`}>Progress percent</label><input id={`${prefix}-percent`} required inputMode="decimal" value={draft.percent} onChange={e => field('percent', e.target.value)} /><p>0–100; up to six decimal places. Completion is a separate action.</p></div> : <p>Use milestone completion controls to record progress. This form can explicitly start the course.</p>}
          {editor.item?.status === 'Planned' && <><div><label htmlFor={`${prefix}-start`}>Start course explicitly</label><input id={`${prefix}-start`} type="checkbox" checked={draft.start} onChange={e => field('start', e.target.checked)} /></div>{draft.start && <div><label htmlFor={`${prefix}-startdate`}>Actual start date</label><input id={`${prefix}-startdate`} type="date" required value={draft.startedOn} onChange={e => field('startedOn', e.target.value)} /></div>}</>}
        </>}
        {editor?.mode === 'milestone' && <div><label htmlFor={`${prefix}-milestone`}>Milestone title</label><input id={`${prefix}-milestone`} required maxLength={200} value={draft.milestoneTitle} onChange={e => field('milestoneTitle', e.target.value)} /></div>}
        <button className="primary-button" disabled={busy || conflict || (editor?.mode === 'progress' && editor.item?.progressMode === 'Milestones' && !draft.start)}>Save Course</button>
      </form>
    </ResourceFormDialog>
    {leave && !uncertain && <ActionDialog title="Bỏ thay đổi chưa lưu?" description="Course draft has not been saved." confirmLabel="Bỏ bản nháp" confirmDisabled={busy} onClose={() => { if (!inFlight.current) setLeave(null); }} onConfirm={() => { if (inFlight.current || originalWrite.current) return false; const proceed = leave.proceed; setEditor(null); setDraft(empty); setCurrent(null); setCurrentChild(null); setLeave(null); proceed(); return true; }}><button disabled={busy || conflict} onClick={async () => { const proceed = leave.proceed; if (await persist()) { setLeave(null); proceed(); } else setLeave(null); }}>Save and leave</button></ActionDialog>}
    {pending && !uncertain && <ActionDialog title={names[pending.preview.operation]} description={<span>{pending.title}. {pending.preview.operation === 'purge' ? 'Permanent deletion cannot be undone.' : 'Confirm this explicit course operation.'}{pending.preview.referenceCount > 0 && ` ${pending.preview.referenceCount} retained references.`}</span>} confirmLabel={names[pending.preview.operation]}
      confirmDisabled={busy || (pending.preview.operation === 'purge' && pending.preview.referenceCount > 0) || (pending.preview.operation === 'complete' && !completeDate)} onClose={() => { if (!inFlight.current) setPending(null); }} onConfirm={commit}>
      {pending.preview.operation === 'complete' && <div><label htmlFor={`${prefix}-completed`}>Actual completion date</label><input id={`${prefix}-completed`} type="date" value={completeDate} disabled={busy} onChange={e => { key.current = crypto.randomUUID(); setCompleteDate(e.target.value); }} /></div>}
    </ActionDialog>}
    {remove && !uncertain && <ActionDialog title="Remove milestone" description={<span>Remove “{remove.child.title}” from “{remove.item.title}”? This owned progress unit will be deleted permanently.</span>} confirmLabel="Remove milestone" confirmDisabled={busy} onClose={() => { if (!inFlight.current) setRemove(null); }} onConfirm={() => childAction(remove.item, remove.child, 'delete', {})} />}
    <ResourceFormDialog open={abandon} title="Dừng đối chiếu yêu cầu?" busy={busy} dirty={false} onClose={() => setAbandon(false)}>
      <p>Server có thể đã ghi nhận thao tác. Dừng đối chiếu chỉ bỏ yêu cầu đang giữ trong trình duyệt, không hủy hoặc hoàn tác dữ liệu. Kiểm tra trạng thái hiện tại trước khi tạo ý định mới.</p>
      <button type="button" disabled={busy} onClick={() => { originalWrite.current = null; unresolved.current = false; setUncertain(false); setAbandon(false); setEditor(null); setDraft(empty); setPending(null); setRemove(null); setLeave(null); setNotice('Đã dừng đối chiếu. Hãy tải lại để kiểm tra dữ liệu trước khi thao tác tiếp.'); }}>Dừng đối chiếu, không hoàn tác</button>
    </ResourceFormDialog>
  </section>;
}
