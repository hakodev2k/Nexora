import { useEffect, useRef, useState, type FormEvent } from 'react';
import { apiFetch, createIdempotencyKey, isProductivityAcknowledgement, jsonMutationHeaders, NexoraApiError } from './api';
import { courseList } from './courseApi';
import { registerDirtyLeaveGuard } from './navigationGuard';
import { ResourceFormDialog } from './ResourceFormDialog';
import type { Skill } from './skillApi';

type Evidence = { id: string; skillId: string; resourceId: string; resourceType: string; note: string | null; state: string; sourceTitle: string | null; sourceStatus: string | null };
type Page = { items: Evidence[]; nextCursor: string | null; skillETag: string };
const root = '/api/v1/learning/skills/';
export function SkillEvidencePanel({ skill, canWrite, onClose, onFailure }: {
  skill: Skill; canWrite: boolean; onClose: () => void; onFailure: (error: unknown) => void;
}) {
  const [items, setItems] = useState<Evidence[]>([]), [cursor, setCursor] = useState<string | null>(null), [etag, setETag] = useState(skill.etag);
  const [choices, setChoices] = useState<{ id: string; title: string }[]>([]), [choiceCursor, setChoiceCursor] = useState<string | null>(null);
  const [courseStatus, setCourseStatus] = useState('Completed');
  const [source, setSource] = useState(''), [note, setNote] = useState(''), [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false), [loaded, setLoaded] = useState(false), [uncertain, setUncertain] = useState(false);
  const [notice, setNotice] = useState<string | null>(null), [removing, setRemoving] = useState<Evidence | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const epoch = useRef(0), mounted = useRef(false), flight = useRef(false), pending = useRef<(() => Promise<unknown>) | null>(null);
  const dirty = source !== '' || note !== '' || uncertain;
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; }; }, []);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !pending.current) return false; setLeave({ proceed }); return true; }, 1), [dirty]);
  function failure(value: unknown) {
    setError(value instanceof Error ? value.message : 'Không thể đọc/ghi evidence.');
    const status = (value as { status?: number }).status;
    if ([401, 403, 404].includes(status ?? 0)) { ++epoch.current; setItems([]); setChoices([]); setLoaded(false); setSource(''); setNote(''); pending.current = null; setUncertain(false); onFailure(value); }
  }
  async function load(append = false) {
    if (busy || uncertain || flight.current) return; const stamp = ++epoch.current; setBusy(true); setError(null);
    try {
      const page = await apiFetch<Page>(root + skill.id + '/evidence' + (append && cursor ? '?cursor=' + encodeURIComponent(cursor) : ''));
      if (!mounted.current || stamp !== epoch.current) return;
      if (!Array.isArray(page.items) || typeof page.skillETag !== 'string') throw new NexoraApiError('Phản hồi evidence không hợp lệ.', 0, 'UnexpectedResponse');
      setItems(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor); setETag(page.skillETag); setLoaded(true);
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); } finally { if (mounted.current) setBusy(false); }
  }
  async function picker(append = false) {
    if (busy || uncertain) return; const stamp = ++epoch.current; setBusy(true); setError(null);
    try {
      const page = await courseList(courseStatus, '', append ? choiceCursor ?? undefined : undefined);
      if (!mounted.current || stamp !== epoch.current) return;
      setChoices(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setChoiceCursor(page.nextCursor);
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); } finally { if (mounted.current) setBusy(false); }
  }
  async function execute() {
    const intent = pending.current; if (!intent || flight.current) return; flight.current = true; ++epoch.current; setBusy(true); setError(null);
    try {
      await intent(); if (!mounted.current || pending.current !== intent) return;
      pending.current = null; setUncertain(false); setItems([]); setChoices([]); setLoaded(false); setCursor(null); setChoiceCursor(null); setSource(''); setNote(''); setRemoving(null);
      setNotice('Đã ghi thay đổi. Tải lại để đọc evidence và revision hiện tại.');
    } catch (value) {
      if (!mounted.current || pending.current !== intent) return;
      const status = (value as { status?: number }).status, code = (value as { code?: string }).code;
      const unknown = !status || status >= 500 || ['RequestInProgress', 'ReceiptUnavailable'].includes(code ?? '');
      setUncertain(unknown); if (!unknown) pending.current = null; failure(value);
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function add(event: FormEvent) {
    event.preventDefault(); if (!loaded || !source || pending.current || busy) return;
    const body = JSON.stringify({ resourceType: 'learning.Course', resourceId: source, note: note || null }), headers = jsonMutationHeaders(createIdempotencyKey(), { 'If-Match': etag });
    const path = root + skill.id + '/evidence', expected = skill.id;
    pending.current = async () => {
      const result = await apiFetch<unknown>(path, { method: 'POST', headers, body });
      if (!isProductivityAcknowledgement(result) || result.id !== expected) throw new NexoraApiError('Không xác định được kết quả commit.', 0, 'UnexpectedResponse');
      return result;
    }; void execute();
  }
  function remove(item: Evidence) {
    if (pending.current || busy || !loaded) return;
    const path = root + skill.id + '/evidence/' + item.id, headers = jsonMutationHeaders(createIdempotencyKey(), { 'If-Match': etag });
    pending.current = () => apiFetch(path, { method: 'DELETE', headers }); void execute();
  }
  const blocked = busy || uncertain || pending.current !== null;
  return <section className="panel" aria-label="Skill evidence"><h2>Evidence · {skill.title}</h2><p>Level tự đánh giá giữ nguyên khi thêm hoặc gỡ evidence.</p>
    <button disabled={busy} onClick={() => dirty || pending.current ? setLeave({ proceed: onClose }) : onClose()}>Back to Skills</button>
    <button disabled={blocked} onClick={() => void load()}>Tải evidence hiện tại</button>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {uncertain && <button disabled={busy} onClick={() => void execute()}>Thử lại đúng request/mã cũ</button>}
    {loaded && !items.length && <p>Chưa có evidence.</p>}
    {items.map(item => <article className="resource-card" key={item.id}><h3>{item.sourceTitle ?? 'Nguồn không khả dụng'}</h3><p>{item.resourceType} · {item.sourceStatus ?? item.state}</p>{item.note && <p>{item.note}</p>}
      {canWrite && skill.status === 'Active' && <button disabled={blocked || !loaded} onClick={() => setRemoving(item)}>Gỡ liên kết</button>}</article>)}
    {cursor && <button disabled={blocked} onClick={() => void load(true)}>Tải thêm evidence</button>}
    {canWrite && skill.status === 'Active' && <form onSubmit={add}><fieldset disabled={blocked || !loaded}><legend>Thêm Course làm evidence</legend>
      <label>Trạng thái Course<select value={courseStatus} onChange={event => { setCourseStatus(event.target.value); setChoices([]); setChoiceCursor(null); setSource(''); }}>{['Planned', 'InProgress', 'Completed', 'Abandoned', 'Archived'].map(status => <option key={status}>{status}</option>)}</select></label>
      <button type="button" onClick={() => void picker()}>Chọn từ Course hiện tại</button>
      {choiceCursor && <button type="button" onClick={() => void picker(true)}>Tải thêm Course</button>}
      <label>Course<select required value={source} onChange={event => setSource(event.target.value)}><option value="">Chọn Course</option>{choices.map(item => <option key={item.id} value={item.id}>{item.title}</option>)}</select></label>
      <label>Ghi chú<textarea maxLength={1000} value={note} onChange={event => setNote(event.target.value)} /></label><button type="submit">Ghi evidence</button></fieldset></form>}
    <ResourceFormDialog open={removing !== null} title="Gỡ evidence" dirty={false} busy={busy} onClose={() => setRemoving(null)}><p>Chỉ gỡ liên kết; Course và level được giữ nguyên.</p><button disabled={blocked || !loaded} onClick={() => removing && remove(removing)}>Xác nhận gỡ</button></ResourceFormDialog>
    <ResourceFormDialog open={leave !== null} title="Rời evidence" dirty={false} busy={busy} onClose={() => setLeave(null)}><p>Rời màn hình sẽ mất bản nháp/mã đối chiếu; request đã gửi có thể đã commit.</p><button disabled={busy} onClick={() => { const proceed = leave?.proceed; pending.current = null; setLeave(null); proceed?.(); }}>Bỏ bản nháp/mã đối chiếu và rời màn hình</button></ResourceFormDialog>
  </section>;
}
