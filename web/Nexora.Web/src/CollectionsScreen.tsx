import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { NexoraApiError } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard } from './navigationGuard';
import { collectionCapabilities, collectionCandidates, collectionTypes, getCollection, listCollections, sendCollectionIntent,
  type CollectionCandidate, type CollectionDetail, type CollectionIntent, type CollectionPage, type CollectionSummary } from './collectionApi';
import './collections.css';

type Editor = { item?: CollectionSummary; name: string; description: string; baseline: string };
const draft = (name: string, description: string) => JSON.stringify({ name, description });
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';

export function CollectionsScreen({ onAuthLost, initialCollectionId, onSourceOpen, onSelectCollection }: {
  onAuthLost: () => void; initialCollectionId?: string;
  onSourceOpen?: (resourceType: string, resourceId: string) => void;
  onSelectCollection?: (id?: string) => void;
}) {
  const prefix = useId();
  const [caps, setCaps] = useState<Record<string, boolean>>({});
  const [page, setPage] = useState<CollectionPage | null>(null);
  const [filter, setFilter] = useState({ query: '', page: 1 }); const [query, setQuery] = useState('');
  const [selected, setSelected] = useState<string | null>(initialCollectionId ?? null);
  const [committedNavigation, setCommittedNavigation] = useState<{ id?: string } | null>(null);
  const [detail, setDetail] = useState<CollectionDetail | null>(null);
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null); const [notice, setNotice] = useState<string | null>(null);
  const [editor, setEditor] = useState<Editor | null>(null); const [formError, setFormError] = useState<string | null>(null);
  const [conflict, setConflict] = useState(false); const [deleting, setDeleting] = useState<CollectionSummary | null>(null);
  const [picker, setPicker] = useState(false); const [sourceType, setSourceType] = useState<string>('Project');
  const [sourceQuery, setSourceQuery] = useState(''); const [candidateQuery, setCandidateQuery] = useState('');
  const [memberQuery, setMemberQuery] = useState('');
  const [candidates, setCandidates] = useState<CollectionCandidate[] | null>(null);
  const [candidateError, setCandidateError] = useState<string | null>(null); const [candidateLoading, setCandidateLoading] = useState(false);
  const [uncertain, setUncertain] = useState<CollectionIntent | null>(null);
  const [abandon, setAbandon] = useState(false);
  const sequence = useRef(0); const inFlight = useRef(false); const mounted = useRef(true);
  const originalIntent = useRef<CollectionIntent | null>(null), unresolved = useRef(false);
  const authLost = useRef(onAuthLost); authLost.current = onAuthLost;
  const allowed = (action: string) => caps[`organization.collection.${action}`] === true;
  const dirty = editor !== null && editor.baseline !== draft(editor.name, editor.description);

  const clearPrivate = useCallback(() => {
    setCaps({}); setPage(null); setDetail(null); setEditor(null); setDeleting(null); setPicker(false);
    setCandidates(null); setFormError(null); setConflict(false); setNotice(null);
  }, []);
  const fail = useCallback((e: unknown) => {
    setError(message(e));
    if (e instanceof NexoraApiError && (e.status === 401 || e.status === 403 || e.code === 'ModuleUnavailable')) {
      sequence.current++; clearPrivate(); setLoading(false); if (e.status === 401) authLost.current();
    }
  }, [clearPrivate]);

  const load = useCallback(async () => {
    const stamp = ++sequence.current; setLoading(true); setError(null); setDetail(null); setPage(null); setCandidates(null);
    try {
      const capabilities = await collectionCapabilities();
      if (stamp !== sequence.current || !mounted.current) return;
      if (!capabilities['organization.collection.read']) { clearPrivate(); setCaps(capabilities); return; }
      const [next, current] = await Promise.all([listCollections(filter.query, filter.page), selected ? getCollection(selected) : Promise.resolve(null)]);
      if (stamp !== sequence.current || !mounted.current) return;
      setCaps(capabilities); setPage(next); setDetail(current);
    } catch (e) {
      if (stamp === sequence.current && mounted.current) {
        if (e instanceof NexoraApiError && e.status === 404) { setDetail(null); setSelected(null); setEditor(null); setDeleting(null); setPicker(false); }
        fail(e);
      }
    } finally { if (stamp === sequence.current && mounted.current) setLoading(false); }
  }, [filter, selected, clearPrivate, fail]);
  useEffect(() => { mounted.current = true; void load(); return () => { mounted.current = false; sequence.current++; }; }, [load]);
  useEffect(() => {
    const refresh = () => { if (!inFlight.current && !uncertain) void load(); };
    window.addEventListener('focus', refresh); return () => window.removeEventListener('focus', refresh);
  }, [load, uncertain]);
  useEffect(() => registerDirtyLeaveGuard(() => inFlight.current || originalIntent.current !== null), []);
  useEffect(() => {
    if (!busy && !uncertain) return;
    const warn = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ''; };
    window.addEventListener('beforeunload', warn); return () => window.removeEventListener('beforeunload', warn);
  }, [busy, uncertain]);
  // Navigate after the committed render has released modal and write guards.
  useEffect(() => {
    if (!committedNavigation || busy || uncertain || editor || deleting || picker || !onSelectCollection) return;
    setCommittedNavigation(null); onSelectCollection(committedNavigation.id);
  }, [committedNavigation, busy, uncertain, editor, deleting, picker, onSelectCollection]);

  useEffect(() => {
    if (!picker || !caps['organization.collection.read']) return;
    const controller = new AbortController(); const stamp = sequence.current;
    setCandidates(null); setCandidateError(null); setCandidateLoading(true);
    collectionCandidates(sourceType, candidateQuery, controller.signal).then(rows => {
      if (!controller.signal.aborted && mounted.current && stamp === sequence.current) setCandidates(rows);
    }).catch(e => { if (!controller.signal.aborted && mounted.current && stamp === sequence.current) { setCandidateError(message(e)); fail(e); } })
      .finally(() => { if (!controller.signal.aborted && mounted.current && stamp === sequence.current) setCandidateLoading(false); });
    return () => controller.abort();
  }, [picker, caps, sourceType, candidateQuery, fail]);

  async function persist(intent: CollectionIntent) {
    if (inFlight.current) return;
    if (originalIntent.current && originalIntent.current !== intent) return;
    originalIntent.current ??= Object.freeze({ ...intent });
    intent = originalIntent.current;
    inFlight.current = true; setBusy(true); setError(null); setFormError(null); setNotice(null);
    try {
      const ack = await sendCollectionIntent(intent);
      if (!mounted.current) return;
      originalIntent.current = null; unresolved.current = false;
      setUncertain(null); setEditor(null); setDeleting(null); setPicker(false); setCandidates(null); setConflict(false);
      setNotice('Đã lưu thao tác. Quyền đọc nguồn luôn được kiểm tra lại.');
      if (intent.method === 'DELETE' && !intent.path.includes('/members/')) {
        if (onSelectCollection) setCommittedNavigation({}); else setSelected(null);
      }
      else if (intent.method === 'POST' && intent.path === '' && onSelectCollection) setCommittedNavigation({ id: ack.id });
      else if (selected !== ack.id) setSelected(ack.id);
      else await load();
    } catch (e) {
      if (!mounted.current) return;
      const status = e instanceof NexoraApiError ? e.status : 0;
      const unknown = status === 0 || status >= 500 || status === 408 ||
        (e instanceof NexoraApiError && ['RequestInProgress', 'IdempotencyConflict', 'UnexpectedAcknowledgement'].includes(e.code ?? ''));
      if (unknown || unresolved.current) { unresolved.current = true; setUncertain(intent); }
      else { originalIntent.current = null; setUncertain(null); }
      if (status === 412 || (e instanceof NexoraApiError && e.code === 'MembershipConflict')) setConflict(true);
      setFormError(message(e)); fail(e);
    } finally { inFlight.current = false; if (mounted.current) setBusy(false); }
  }
  function intent(method: CollectionIntent['method'], path: string, body?: unknown): CollectionIntent {
    return Object.freeze({ method, path, body: body === undefined ? undefined : JSON.stringify(body), etag: detail?.collection.etag, key: crypto.randomUUID(), expectedId: detail?.collection.id });
  }
  function edit(item?: CollectionSummary) {
    setFormError(null); setConflict(false);
    setEditor({ item, name: item?.name ?? '', description: item?.description ?? '', baseline: draft(item?.name ?? '', item?.description ?? '') });
  }
  function save(e: FormEvent) {
    e.preventDefault(); if (!editor || conflict || uncertain || !editor.name.trim()) return;
    const body = { name: editor.name.trim(), description: editor.description.trim() || null };
    void persist(editor.item ? { ...intent('PUT', `/${editor.item.id}`, body), etag: editor.item.etag, expectedId: editor.item.id }
      : Object.freeze({ method: 'POST', path: '', body: JSON.stringify(body), key: crypto.randomUUID() }));
  }
  async function reloadRevision() {
    if (!selected || busy || originalIntent.current || inFlight.current) return;
    const id = selected; const stamp = ++sequence.current;
    inFlight.current = true; setBusy(true); setFormError(null);
    try {
      const currentCaps = await collectionCapabilities();
      if (!mounted.current || stamp !== sequence.current) return;
      setCaps(currentCaps);
      if (!currentCaps['organization.collection.read']) { clearPrivate(); return; }
      const next = await getCollection(id);
      if (!mounted.current || stamp !== sequence.current) return;
      setDetail(next);
      // The user explicitly accepts a new base while retaining their unsaved draft.
      setEditor(current => current ? { ...current, item: next.collection } : current);
      setDeleting(null); setConflict(false); setNotice('Đã nạp bản hiện tại. Kiểm tra bản nháp trước khi Lưu.');
    } catch (e) {
      if (mounted.current && stamp === sequence.current) {
        if (e instanceof NexoraApiError && e.status === 404) { setDetail(null); setEditor(null); setDeleting(null); setPicker(false); }
        fail(e);
      }
    } finally { inFlight.current = false; if (mounted.current) setBusy(false); }
  }
  function move(index: number, delta: number) {
    if (!detail || busy || uncertain) return;
    const ids = detail.members.map(m => m.id); const target = index + delta;
    if (target < 0 || target >= ids.length) return;
    [ids[index], ids[target]] = [ids[target], ids[index]];
    void persist(intent('PUT', `/${detail.collection.id}/members/order`, { memberIds: ids }));
  }
  const blocked = busy || uncertain !== null || loading;
  return <section className="collections-screen" aria-labelledby={`${prefix}-title`} aria-busy={loading || busy}>
    <header className="collections-toolbar"><h1 id={`${prefix}-title`}>Collections</h1>
      {allowed('create') && <button type="button" disabled={blocked} onClick={() => edit()}>Tạo Collection</button>}
      <button type="button" disabled={busy || !!uncertain} onClick={() => void load()}>Tải lại</button>
    </header>
    <p>Nhóm tham chiếu của bạn. Collection không cấp quyền truy cập nguồn và không xóa nguồn.</p>
    {notice && <p role="status">{notice}</p>}
    {error && <p role="alert">{error}</p>}
    {uncertain && <div role="alert"><p>Chưa xác nhận được kết quả. Thử lại cùng yêu cầu để tránh thao tác trùng.</p>
      <button type="button" disabled={busy} onClick={() => void persist(uncertain)}>Thử lại yêu cầu</button>
      <button type="button" disabled={busy} onClick={() => setAbandon(true)}>Dừng đối chiếu</button></div>}
    {conflict && !editor && <div role="alert"><p>Collection hoặc quyền đọc nguồn đã thay đổi. Tải lại trước khi thao tác.</p>
      <button type="button" disabled={busy} onClick={() => void reloadRevision()}>Nạp bản hiện tại</button></div>}
    {loading && <p role="status">Đang tải Collections…</p>}
    {!loading && !allowed('read') && !error && <p role="status">Collections không khả dụng với quyền hiện tại.</p>}
    {allowed('read') && <div className="collections-layout">
      <div>
        <form onSubmit={e => { e.preventDefault(); setFilter({ query: query.trim(), page: 1 }); }} className="collections-toolbar">
          <label htmlFor={`${prefix}-search`}>Tìm tên</label><input id={`${prefix}-search`} value={query} maxLength={100} onChange={e => setQuery(e.target.value)} />
          <button disabled={blocked}>Tìm</button>
          {filter.query && <button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ query: '', page: 1 }); }}>Xóa bộ lọc</button>}
        </form>
        {!loading && page?.items.length === 0 && <p>{filter.query ? 'Không có Collection khớp bộ lọc.' : 'Bạn chưa có Collection. Tạo Collection để nhóm các nguồn hiện có.'}</p>}
        <ul className="collections-list">{page?.items.map(item => <li key={item.id}>
          <button type="button" disabled={blocked} aria-current={selected === item.id ? 'true' : undefined} onClick={() => { if (onSelectCollection) onSelectCollection(item.id); else setSelected(item.id); setConflict(false); }}>{item.name}</button>
          {item.description && <p>{item.description}</p>}
        </li>)}</ul>
        <nav aria-label="Phân trang Collection" className="collections-toolbar">
          <button type="button" disabled={blocked || filter.page === 1} onClick={() => setFilter(f => ({ ...f, page: f.page - 1 }))}>Trước</button>
          <span>Trang {filter.page}</span><button type="button" disabled={blocked || !page?.hasMore} onClick={() => setFilter(f => ({ ...f, page: f.page + 1 }))}>Sau</button>
        </nav>
      </div>
      <div>
        {detail && <article aria-labelledby={`${prefix}-detail`}>
          <div className="collections-toolbar"><h2 id={`${prefix}-detail`}>{detail.collection.name}</h2>
            <button type="button" disabled={blocked} onClick={() => onSelectCollection ? onSelectCollection() : setSelected(null)}>Đóng chi tiết</button></div>
          <p className="collections-description">{detail.collection.description}</p>
          <div className="collections-toolbar">
            {allowed('update') && <button type="button" disabled={blocked} onClick={() => edit(detail.collection)}>Sửa</button>}
            {allowed('add') && <button type="button" disabled={blocked} onClick={() => { setPicker(true); setSourceQuery(''); setCandidateQuery(''); }}>Thêm nguồn hiện có</button>}
            {allowed('delete') && <button type="button" disabled={blocked} onClick={() => { setDeleting(detail.collection); setFormError(null); }}>Xóa Collection</button>}
          </div>
          <p>Chỉ hiển thị nguồn bạn hiện được phép đọc. Hiện hỗ trợ Project, Document, Bookmark và Snippet.</p>
          <label htmlFor={`${prefix}-member-query`}>Lọc tên nguồn trong Collection</label>{' '}
          <input id={`${prefix}-member-query`} value={memberQuery} maxLength={100} onChange={e => setMemberQuery(e.target.value)} />
          {detail.members.length === 0 && <p>Chưa có nguồn hiện đọc được. Dùng “Thêm nguồn hiện có” để chọn nguồn hợp lệ.</p>}
          {detail.members.length > 0 && !detail.members.some(m => m.title.toLocaleLowerCase().includes(memberQuery.trim().toLocaleLowerCase())) && <p>Không có nguồn khớp bộ lọc. Xóa từ khóa để xem lại.</p>}
          <ol className="collections-members">{detail.members.map((member, index) => ({ member, index })).filter(({ member }) => member.title.toLocaleLowerCase().includes(memberQuery.trim().toLocaleLowerCase())).map(({ member, index }) => <li key={member.id}>
            <div><strong>{member.title}</strong><span> · {member.resourceType} · {member.status}</span></div>
            <div className="collections-toolbar">
              {onSourceOpen && <button type="button" disabled={blocked} onClick={() => onSourceOpen(member.resourceType, member.resourceId)}>Mở nguồn</button>}
              {allowed('reorder') && <><button type="button" disabled={blocked || conflict || index === 0} aria-label={`Di chuyển ${member.title} lên`} onClick={() => move(index, -1)}>Lên</button>
                <button type="button" disabled={blocked || conflict || index === detail.members.length - 1} aria-label={`Di chuyển ${member.title} xuống`} onClick={() => move(index, 1)}>Xuống</button></>}
              {allowed('remove') && <button type="button" disabled={blocked || conflict} onClick={() => void persist(intent('DELETE', `/${detail.collection.id}/members/${member.id}`))}>Gỡ tham chiếu</button>}
            </div>
          </li>)}</ol>
        </article>}
        {!selected && <p>Chọn một Collection để xem và sắp xếp các tham chiếu.</p>}
      </div>
    </div>}
    <ResourceFormDialog open={editor !== null && !uncertain} title={editor?.item ? 'Sửa Collection' : 'Tạo Collection'} busy={busy} dirty={dirty} closeBlocked={!!uncertain}
      onClose={() => { setEditor(null); setFormError(null); setConflict(false); }}>
      {editor && <form onSubmit={save} className="collections-form">
        <label htmlFor={`${prefix}-name`}>Tên (1–100 ký tự)</label><input id={`${prefix}-name`} required maxLength={100} value={editor.name} disabled={busy || !!uncertain} onChange={e => setEditor({ ...editor, name: e.target.value })} />
        <label htmlFor={`${prefix}-description`}>Mô tả (tùy chọn)</label><textarea id={`${prefix}-description`} maxLength={4000} rows={4} value={editor.description} disabled={busy || !!uncertain} onChange={e => setEditor({ ...editor, description: e.target.value })} />
        {formError && <p role="alert">{formError}</p>}
        {conflict && <div><p>Bản nháp được giữ nguyên. Nạp bản hiện tại, so sánh rồi chọn Lưu.</p>
          {detail && <p>Tên hiện tại: {detail.collection.name}. Mô tả hiện tại: {detail.collection.description}</p>}
          <button type="button" disabled={busy} onClick={() => void reloadRevision()}>Nạp bản hiện tại và giữ bản nháp</button></div>}
        <button disabled={busy || !!uncertain || conflict || !editor.name.trim()}>Lưu</button>
        {uncertain && <button type="button" disabled={busy} onClick={() => void persist(uncertain)}>Thử lại cùng yêu cầu</button>}
      </form>}
    </ResourceFormDialog>
    <ResourceFormDialog open={picker && !uncertain} title="Thêm nguồn hiện có" busy={busy} dirty={false} closeBlocked={!!uncertain} onClose={() => setPicker(false)}>
      <form className="collections-form" onSubmit={e => { e.preventDefault(); setCandidateQuery(sourceQuery.trim()); }}>
        <label htmlFor={`${prefix}-type`}>Loại nguồn</label><select id={`${prefix}-type`} value={sourceType} disabled={busy || !!uncertain} onChange={e => { setSourceType(e.target.value); setCandidateQuery(''); setSourceQuery(''); }}>
          {collectionTypes.map(type => <option key={type}>{type}</option>)}
        </select>
        <label htmlFor={`${prefix}-source-query`}>Tìm tên nguồn</label><input id={`${prefix}-source-query`} maxLength={100} value={sourceQuery} onChange={e => setSourceQuery(e.target.value)} />
        <button disabled={busy || !!uncertain}>Tìm nguồn</button>
      </form>
      <p>Tối đa 25 kết quả. Thu hẹp tên để tìm thêm. Nguồn bị ẩn, trong Thùng rác hoặc thiếu quyền đọc không xuất hiện.</p>
      {candidateLoading && <p role="status">Đang tải nguồn…</p>}{candidateError && <p role="alert">{candidateError}</p>}
      {!candidateLoading && candidates?.length === 0 && <p>Không có nguồn đủ điều kiện khớp tên. Kiểm tra loại, từ khóa và quyền đọc nguồn.</p>}
      <ul className="collections-list">{candidates?.map(candidate => <li key={candidate.resourceId}>
        <span>{candidate.title} · {candidate.status}</span>
        <button type="button" disabled={busy || !!uncertain || !!detail?.members.some(m => m.resourceType === candidate.resourceType && m.resourceId === candidate.resourceId)}
          onClick={() => detail && void persist(intent('POST', `/${detail.collection.id}/members`, { resourceType: candidate.resourceType, resourceId: candidate.resourceId }))}>Thêm</button>
      </li>)}</ul>
      {formError && <p role="alert">{formError}</p>}{uncertain && <button type="button" disabled={busy} onClick={() => void persist(uncertain)}>Thử lại cùng yêu cầu</button>}
    </ResourceFormDialog>
    <ResourceFormDialog open={deleting !== null && !uncertain} title="Xóa Collection" busy={busy} dirty={false} closeBlocked={!!uncertain} onClose={() => setDeleting(null)}>
      <p>Xóa Collection “{deleting?.name}” và các tham chiếu bên trong? Các nguồn gốc vẫn được giữ nguyên. Container đã xóa không thể khôi phục trong giao diện này.</p>
      {formError && <p role="alert">{formError}</p>}
      <button type="button" disabled={busy || !!uncertain} onClick={() => setDeleting(null)}>Hủy</button>
      <button type="button" disabled={busy || !!uncertain || conflict} onClick={() => deleting && void persist({ method: 'DELETE', path: `/${deleting.id}`, etag: deleting.etag, key: crypto.randomUUID(), expectedId: deleting.id })}>Xóa Collection, giữ nguồn</button>
      {conflict && <button type="button" disabled={busy} onClick={() => void reloadRevision()}>Nạp lại trước khi xác nhận</button>}
      {uncertain && <button type="button" disabled={busy} onClick={() => void persist(uncertain)}>Thử lại cùng yêu cầu</button>}
    </ResourceFormDialog>
    <ResourceFormDialog open={abandon} title="Dừng đối chiếu yêu cầu?" busy={busy} dirty={false} onClose={() => setAbandon(false)}>
      <p>Server có thể đã ghi nhận thao tác. Bỏ yêu cầu trong trình duyệt không hủy hoặc hoàn tác dữ liệu. Tải lại trạng thái trước khi tạo ý định mới.</p>
      <button type="button" disabled={busy} onClick={() => { originalIntent.current = null; unresolved.current = false; setUncertain(null); setAbandon(false); setEditor(null); setDeleting(null); setPicker(false); setNotice('Đã dừng đối chiếu. Tải lại trước khi thao tác tiếp.'); }}>Dừng đối chiếu, không hoàn tác</button>
    </ResourceFormDialog>
  </section>;
}
