import { useEffect, useRef, useState, type FormEvent } from 'react';
import { createIdempotencyKey, listCalendarEvents, NexoraApiError, type CalendarEventRecord } from './api';
import { careerAppointments, careerJob, linkCareerAppointment, unlinkCareerAppointment, type AppointmentEvent, type CareerAppointment, type CareerJob } from './careerApi';
import { ResourceFormDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard } from './navigationGuard';

const empty = { title: '', description: '', start: '', end: '', timeZoneId: '', allDay: false };
export function CareerAppointments({ job, caps, onClose, onFailure, onOpenEvent }: {
  job: CareerJob; caps: Record<string, boolean>; onClose: () => void; onFailure: (error: unknown) => void; onOpenEvent: (id: string) => void;
}) {
  const [revision, setRevision] = useState(job.etag), [items, setItems] = useState<CareerAppointment[]>([]), [cursor, setCursor] = useState<string | null>(null);
  const [choices, setChoices] = useState<CalendarEventRecord[]>([]), [choiceCursor, setChoiceCursor] = useState<string | null>(null);
  const [eventId, setEventId] = useState(''), [draft, setDraft] = useState(empty), [mode, setMode] = useState<'create' | 'link'>('create');
  const [busy, setBusy] = useState(false), [loaded, setLoaded] = useState(false), [uncertain, setUncertain] = useState(false), [needsRefresh, setNeedsRefresh] = useState(false);
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null), [removing, setRemoving] = useState<CareerAppointment | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const epoch = useRef(0), flight = useRef(false), mounted = useRef(false), pending = useRef<(() => Promise<unknown>) | null>(null);
  const can = (action: string) => caps['career.appointment.' + action] === true;
  const dirty = uncertain || eventId !== '' || JSON.stringify(draft) !== JSON.stringify(empty);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && pending.current === null) return false; setLeave({ proceed }); return true; }, 1), [dirty]);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; pending.current = null; }; }, []);
  function failure(value: unknown) {
    const apiError = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể hoàn tất Calendar link.', 0, 'NetworkError'); setError(apiError.message);
    if ([401, 403, 404].includes(apiError.status) || ['ModuleUnavailable', 'ActionDenied', 'DependencyUnavailable'].includes(apiError.code ?? '')) {
      ++epoch.current; setItems([]); setCursor(null); setChoices([]); setChoiceCursor(null); setLoaded(false); setDraft(empty); setEventId(''); setRemoving(null); pending.current = null; setUncertain(false); onFailure(apiError);
    }
    return apiError;
  }
  async function load(append = false) {
    if (flight.current || uncertain) return; const stamp = ++epoch.current; setBusy(true); setError(null);
    try {
      const current = await careerJob(job.id); if (!mounted.current || stamp !== epoch.current) return;
      setRevision(current.etag); setNeedsRefresh(false);
      if (can('read')) {
        const page = await careerAppointments(job.id, append ? cursor ?? '' : '');
        if (!mounted.current || stamp !== epoch.current) return;
        setItems(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor); setLoaded(true);
      }
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); }
    finally { if (mounted.current) setBusy(false); }
  }
  async function picker(append = false) {
    if (flight.current || busy || uncertain) return; const stamp = ++epoch.current; setBusy(true); setError(null);
    try {
      const page = await listCalendarEvents(undefined, undefined, 25, append ? choiceCursor ?? '' : '');
      if (!mounted.current || stamp !== epoch.current) return;
      const eligible = page.items.filter(item => item.sourceKind === 'Manual' && !item.taskId && item.status === 'Scheduled');
      setChoices(old => append ? [...new Map([...old, ...eligible].map(item => [item.id, item])).values()] : eligible); setChoiceCursor(page.nextCursor);
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); }
    finally { if (mounted.current) setBusy(false); }
  }
  async function execute() {
    const intent = pending.current; if (!intent || flight.current) return; flight.current = true; ++epoch.current; setBusy(true); setError(null); setNotice(null);
    try {
      await intent(); if (!mounted.current || pending.current !== intent) return;
      pending.current = null; setUncertain(false); setItems([]); setChoices([]); setCursor(null); setChoiceCursor(null); setLoaded(false); setDraft(empty); setEventId(''); setRemoving(null); setNeedsRefresh(true);
      setNotice('Thao tác đã được lưu. Tải lại Job/links để đọc dữ liệu hiện tại. Event và reminder vẫn do Calendar quản lý.');
    } catch (value) {
      if (!mounted.current || pending.current !== intent) return; const apiError = failure(value);
      const unknown = apiError.status === 0 || apiError.status >= 500 || apiError.status === 408 || ['RequestInProgress', 'IdempotencyConflict'].includes(apiError.code ?? '');
      if (unknown && pending.current === intent) { setUncertain(true); setRemoving(null); } else { pending.current = null; if (apiError.status === 412) setNeedsRefresh(true); }
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function submit(event: FormEvent) {
    event.preventDefault(); if (busy || pending.current || needsRefresh) return;
    let source: AppointmentEvent | null = null;
    if (mode === 'create') {
      const startAt = draft.allDay ? `${draft.start}T00:00:00Z` : draft.start, endAt = draft.allDay ? `${draft.end}T00:00:00Z` : draft.end;
      if (!draft.title.trim() || !draft.description.trim() || !draft.timeZoneId.trim() || !/(Z|[+-]\d{2}:\d{2})$/.test(startAt)
          || !/(Z|[+-]\d{2}:\d{2})$/.test(endAt) || !Number.isFinite(Date.parse(startAt)) || !Number.isFinite(Date.parse(endAt)) || Date.parse(endAt) <= Date.parse(startAt)) { setError('Nhập đủ Title/Description/timezone và Start/End có offset; End phải sau Start.'); return; }
      source = { title: draft.title.trim(), description: draft.description.trim(), startAt, endAt, timeZoneId: draft.timeZoneId.trim(), isAllDay: draft.allDay, startDate: draft.allDay ? draft.start : null, endDateExclusive: draft.allDay ? draft.end : null };
    } else if (!eventId) { setError('Chọn Personal Event Scheduled.'); return; }
    const command = { eventId: mode === 'link' ? eventId : null, event: source }, key = createIdempotencyKey(), etag = revision;
    pending.current = () => linkCareerAppointment(job.id, etag, command, key); void execute();
  }
  const blocked = busy || uncertain;
  return <section className="form-panel" aria-label="Linked Calendar Events"><div className="section-heading"><h2>Calendar links — {job.metadata.title}</h2><button type="button" disabled={blocked} onClick={() => { if (dirty) setLeave({ proceed: onClose }); else onClose(); }}>Back to Job</button></div>
    <p>Thời gian, trạng thái và reminder được quản lý tại Calendar. Unlink chỉ gỡ tham chiếu.</p><button type="button" disabled={blocked} onClick={() => void load()}>Tải lại Job/links</button>
    {error && <div role="alert" className="notice notice-error">{error}</div>}{notice && <p role="status">{notice}</p>}{uncertain && <div role="alert">Kết quả chưa rõ; giữ nguyên lệnh đã gửi.<button type="button" disabled={busy} onClick={() => void execute()}>Thử lại thao tác cũ</button></div>}
    {!job.isTrash && <form onSubmit={submit}><fieldset disabled={blocked || needsRefresh}><legend>Create / link Personal Event</legend><label>Thao tác<select value={mode} onChange={event => setMode(event.target.value as 'create' | 'link')}><option value="create">Tạo Event và link atomic</option><option value="link">Link Event đã có</option></select></label>
      {mode === 'create' ? <><label>Title<input value={draft.title} maxLength={200} onChange={event => setDraft(old => ({ ...old, title: event.target.value }))} required /></label><label>Description<textarea value={draft.description} maxLength={20000} onChange={event => setDraft(old => ({ ...old, description: event.target.value }))} required /></label><label>IANA timezone<input value={draft.timeZoneId} maxLength={128} onChange={event => setDraft(old => ({ ...old, timeZoneId: event.target.value }))} required /></label><label className="check-row"><input type="checkbox" checked={draft.allDay} onChange={event => setDraft(old => ({ ...old, allDay: event.target.checked, start: '', end: '' }))} />All-day</label><label>Start {draft.allDay ? '(date)' : '(ISO8601 có offset)'}<input type={draft.allDay ? 'date' : 'text'} value={draft.start} placeholder={draft.allDay ? undefined : '2026-10-20T09:00:00+07:00'} onChange={event => setDraft(old => ({ ...old, start: event.target.value }))} required /></label><label>End {draft.allDay ? '(exclusive date)' : '(ISO8601 có offset)'}<input type={draft.allDay ? 'date' : 'text'} value={draft.end} onChange={event => setDraft(old => ({ ...old, end: event.target.value }))} required /></label></> : <><button type="button" disabled={!can('link_event')} onClick={() => void picker()}>Đọc lựa chọn Personal Event</button><label>Scheduled Personal Event<select value={eventId} onChange={event => setEventId(event.target.value)}><option value="">Chọn Event</option>{choices.map(item => <option key={item.id} value={item.id}>{item.title} · {item.startAt}</option>)}</select></label>{choiceCursor && <button type="button" onClick={() => void picker(true)}>Tải thêm Event</button>}</>}
      <button type="submit" disabled={mode === 'create' ? !can('create_event') : !can('link_event')}>{mode === 'create' ? 'Tạo Event và link' : 'Link Event đã chọn'}</button></fieldset></form>}
    {needsRefresh && <p>Tải lại revision của Job trước thao tác tiếp theo.</p>}{loaded && !error && items.length === 0 && <p>Chưa có Calendar link.</p>}
    <ul className="resource-cards">{items.map(item => <li className="resource-card" key={item.id}><div><strong>{item.event?.title ?? 'Event không khả dụng'}</strong>{item.event && <p>{item.event.status} · {item.event.isAllDay ? `${item.event.startDate} — ${item.event.endDateExclusive} (exclusive)` : `${item.event.startAt} — ${item.event.endAt}`} · {item.event.timeZoneId}</p>}</div>{item.event && <button type="button" disabled={blocked} onClick={() => onOpenEvent(item.eventId)}>Mở Event tại nguồn</button>}<button type="button" disabled={blocked || !can('unlink_event')} onClick={() => setRemoving(item)}>Unlink</button></li>)}</ul>{cursor && <button type="button" disabled={blocked} onClick={() => void load(true)}>Tải thêm links</button>}
    <ResourceFormDialog open={removing !== null} title="Unlink Calendar Event?" busy={busy} dirty={false} onClose={() => setRemoving(null)}><p>Event và reminder không bị xóa hoặc hủy.</p><button type="button" onClick={() => { if (!removing || pending.current) return; const item = removing, key = createIdempotencyKey(); pending.current = () => unlinkCareerAppointment(item, key); void execute(); }}>Xác nhận unlink</button></ResourceFormDialog>
    <ResourceFormDialog open={leave !== null} title="Rời Calendar links?" busy={busy} dirty={false} onClose={() => setLeave(null)}><p>Bản nháp sẽ bị bỏ. Nếu request đã gửi, rời màn hình không hoàn tác Event/link có thể đã commit.</p><button type="button" disabled={busy} onClick={() => { const proceed = leave?.proceed; pending.current = null; ++epoch.current; setLeave(null); proceed?.(); }}>Bỏ bản nháp/mã đối chiếu và rời màn hình</button></ResourceFormDialog>
  </section>;
}
