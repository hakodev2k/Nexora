import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog } from './App';
import { registerDirtyLeaveGuard } from './navigationGuard';
import { ResourceFormDialog } from './ResourceFormDialog';
import { certificationCapabilities, certificationCreate, certificationGet, certificationHistory, certificationList,
  certificationPreview, certificationRenew, certificationTransition, certificationUpdate,
  type Certification, type CertificationAcknowledgement, type CertificationPreview, type CertificationVersion } from './certificationApi';

type Draft = { title: string; issuer: string; verificationUrl: string; issuedOn: string; expiresOn: string; reason: string };
type Editor = { mode: 'create' | 'edit' | 'renew'; row?: Certification; baseline: string };
type Intent = { send: () => Promise<CertificationAcknowledgement>; label: string };
const blank: Draft = { title: '', issuer: '', verificationUrl: '', issuedOn: '', expiresOn: '', reason: '' };
const stateLabel = { Active: 'Còn hiệu lực', Expired: 'Đã hết hạn', Archived: 'Đã lưu trữ' };
const historyLabel: Record<string, string> = { Created: 'Tạo mới', Updated: 'Cập nhật', Renewed: 'Gia hạn', Archived: 'Lưu trữ', Unarchived: 'Bỏ lưu trữ' };
const text = (error: unknown) => error instanceof Error ? error.message : 'Không thể hoàn tất thao tác. Hãy thử lại.';
const date = (value: string | null) => value ? value.split('-').reverse().join('/') : 'Chưa ghi nhận';

export function CertificationScreen({ onAuthLost, certificationId, onOpenCertification, onBackToCertifications }: {
  onAuthLost: () => void; certificationId?: string; onOpenCertification?: (id: string) => void; onBackToCertifications?: () => void;
}) {
  const prefix = useId(); const [localId, setLocalId] = useState<string>(); const activeId = certificationId ?? localId;
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [items, setItems] = useState<Certification[]>([]), [detail, setDetail] = useState<Certification | null>(null);
  const [filter, setFilter] = useState({ status: 'All', query: '' }), [query, setQuery] = useState(''), [cursor, setCursor] = useState<string | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const [editor, setEditor] = useState<Editor | null>(null), [draft, setDraft] = useState<Draft>(blank), [formError, setFormError] = useState<string | null>(null), [uncertain, setUncertain] = useState(false), [conflict, setConflict] = useState(false);
  const [preview, setPreview] = useState<CertificationPreview | null>(null), [history, setHistory] = useState<CertificationVersion[] | null>(null), [historyCursor, setHistoryCursor] = useState<string | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const alive = useRef(true), epoch = useRef(0), flight = useRef(false), original = useRef<Intent | null>(null);
  const capsRef = useRef(caps); capsRef.current = caps;
  const authLost = useRef(onAuthLost); authLost.current = onAuthLost;
  const can = (action: string) => caps['learning.certification.' + action] === true;
  const clear = useCallback(() => { setItems([]); setDetail(null); setEditor(null); setDraft(blank); setPreview(null); setHistory(null); setHistoryCursor(null); setCursor(null); setUncertain(false); setConflict(false); setFormError(null); setNotice(null); setLeave(null); original.current = null; }, []);
  const fail = useCallback((e: unknown) => {
    if (!alive.current) return;
    setError(text(e)); const status = (e as { status?: number }).status;
    if (status === 401 || status === 403 || status === 404) { epoch.current++; clear(); setCaps({}); capsRef.current = {}; setLoading(false); if (status === 401) authLost.current(); }
  }, [clear]);
  const load = useCallback(async () => {
    const stamp = ++epoch.current; setLoading(true); setError(null); setHistory(null); setHistoryCursor(null);
    try {
      const allowed = await certificationCapabilities(); if (!alive.current || stamp !== epoch.current) return;
      setCaps(allowed); capsRef.current = allowed;
      if (!allowed['learning.certification.read']) { setItems([]); setDetail(null); setCursor(null); return; }
      if (activeId) { const row = await certificationGet(activeId); if (!alive.current || stamp !== epoch.current) return; setDetail(row); setItems([]); setCursor(null); }
      else { const page = await certificationList(filter.status, filter.query); if (!alive.current || stamp !== epoch.current) return; setDetail(null); setItems(page.items); setCursor(page.nextCursor); }
    } catch (e) { if (alive.current && stamp === epoch.current) fail(e); }
    finally { if (alive.current && stamp === epoch.current) setLoading(false); }
  }, [activeId, filter, fail]);
  useEffect(() => { alive.current = true; void load(); return () => { alive.current = false; epoch.current++; }; }, [load]);
  useEffect(() => registerDirtyLeaveGuard(proceed => {
    if (!original.current) return false;
    if (!flight.current) setLeave({ proceed }); return true;
  }, -90), []);
  useEffect(() => { const warn = (event: BeforeUnloadEvent) => { if (original.current) { event.preventDefault(); event.returnValue = ''; } }; window.addEventListener('beforeunload', warn); return () => window.removeEventListener('beforeunload', warn); }, []);
  const dirty = editor !== null && JSON.stringify(draft) !== editor.baseline;
  const blocked = loading || busy || editor !== null || preview !== null || uncertain;
  function begin(mode: Editor['mode'], row?: Certification) {
    const next = row ? { title: row.title, issuer: row.issuer, verificationUrl: row.verificationUrl ?? '', issuedOn: row.issuedOn ?? '', expiresOn: row.expiresOn ?? '', reason: '' } : blank;
    setDraft(next); setEditor({ mode, row, baseline: JSON.stringify(next) }); setFormError(null); setConflict(false); setUncertain(false); original.current = null;
  }
  async function perform(intent: Intent) {
    if (flight.current) return false;
    original.current ??= intent; const stamp = epoch.current;
    flight.current = true; setBusy(true); setFormError(null); setError(null);
    try {
      await original.current.send(); if (!alive.current || stamp !== epoch.current) return false;
      original.current = null; setUncertain(false); setEditor(null); setDraft(blank); setPreview(null); setConflict(false); setLeave(null); setNotice(intent.label);
      await load(); return true;
    } catch (e) {
      if (!alive.current || stamp !== epoch.current) return false;
      const status = (e as { status?: number }).status; setFormError(text(e));
      if (status === 401 || status === 403 || status === 404) fail(e);
      else if (status === undefined || status === 0 || status === 408 || status >= 500 || (status === 409 && (e as { code?: string }).code === 'RequestInProgress')) setUncertain(true);
      else { original.current = null; setUncertain(false); if (status === 412) setConflict(true); }
      return false;
    } finally { flight.current = false; if (alive.current) setBusy(false); }
  }
  async function save(event: FormEvent) {
    event.preventDefault(); if (!editor || flight.current || conflict) return;
    const action = editor.mode === 'edit' ? 'update' : editor.mode;
    if (!capsRef.current['learning.certification.' + action]) { fail(Object.assign(new Error('Thao tác hiện không khả dụng.'), { status: 403 })); return; }
    if (original.current) { await perform(original.current); return; }
    if (editor.mode !== 'renew' && (!draft.title.trim() || !draft.issuer.trim())) { setFormError('Vui lòng nhập tên chứng chỉ và đơn vị cấp.'); return; }
    if (editor.mode !== 'edit' && draft.issuedOn && draft.expiresOn && draft.expiresOn < draft.issuedOn) { setFormError('Ngày hết hạn phải bằng hoặc sau ngày cấp.'); return; }
    const key = crypto.randomUUID(); const row = editor.row;
    const metadata = { title: draft.title, issuer: draft.issuer, verificationUrl: draft.verificationUrl.trim() || null };
    const dates = { issuedOn: draft.issuedOn || null, expiresOn: draft.expiresOn || null };
    const send = editor.mode === 'create' ? () => certificationCreate({ ...metadata, ...dates }, key)
      : editor.mode === 'edit' ? () => certificationUpdate(row!, metadata, key)
        : () => certificationRenew(row!, { ...dates, reason: draft.reason.trim() || null }, key);
    await perform({ send, label: editor.mode === 'renew' ? 'Đã ghi nhận lần gia hạn và giữ lại lịch sử trước đó.' : 'Đã lưu chứng chỉ.' });
  }
  async function lifecycle(row: Certification) {
    if (blocked) return; const operation = row.status === 'Archived' ? 'unarchive' : 'archive'; const stamp = epoch.current;
    flight.current = true; setBusy(true); setError(null); setFormError(null);
    try { const next = await certificationPreview(row.id, operation); if (alive.current && stamp === epoch.current) setPreview(next); }
    catch (e) { if (alive.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (alive.current) setBusy(false); }
  }
  async function more(kind: 'list' | 'history') {
    if (blocked || !can('read')) return; const stamp = epoch.current; flight.current = true; setBusy(true); setError(null);
    try {
      if (kind === 'list' && cursor) { const page = await certificationList(filter.status, filter.query, cursor); if (alive.current && stamp === epoch.current) { setItems(old => [...old, ...page.items.filter(row => !old.some(item => item.id === row.id))]); setCursor(page.nextCursor); } }
      else if (kind === 'history' && detail) { const page = await certificationHistory(detail.id, historyCursor); if (alive.current && stamp === epoch.current) { setHistory(old => [...(old ?? []), ...page.items.filter(row => !(old ?? []).some(item => item.id === row.id))]); setHistoryCursor(page.nextCursor); } }
    } catch (e) { if (alive.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (alive.current) setBusy(false); }
  }
  function closeEditor() {
    if (flight.current) return;
    if (original.current) { setLeave({ proceed: () => { setEditor(null); setDraft(blank); setUncertain(false); void load(); } }); return; }
    setEditor(null); setDraft(blank); setFormError(null); setConflict(false);
  }
  function open(row: Certification) { if (onOpenCertification) onOpenCertification(row.id); else setLocalId(row.id); }
  return <section className="content-section" aria-labelledby={`${prefix}-title`}>
    <div className="content-heading"><div><p className="eyebrow">HỌC TẬP · CHỨNG CHỈ</p><h1 id={`${prefix}-title`}>{detail?.title ?? 'Chứng chỉ của bạn'}</h1><p>Lưu dấu mốc học tập, theo dõi ngày hết hạn và giữ lại lịch sử gia hạn.</p></div>
      <button className="secondary-button" disabled={blocked} onClick={() => void load()}>Tải lại</button></div>
    {activeId && <button className="secondary-button" disabled={blocked} onClick={() => onBackToCertifications ? onBackToCertifications() : setLocalId(undefined)}>← Danh sách chứng chỉ</button>}
    <p>Thông tin do bạn nhập; Nexora không xác nhận tính xác thực của chứng chỉ. Nhắc hạn tự động và tệp minh chứng chưa khả dụng.</p>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}{loading && <p role="status">Đang tải chứng chỉ…</p>}
    {!loading && !Object.values(caps).some(Boolean) && <p role="status">Chứng chỉ chưa khả dụng với quyền hoặc module hiện tại.</p>}
    {!activeId && <>{can('create') && <button className="primary-button" disabled={blocked} onClick={() => begin('create')}>Thêm chứng chỉ</button>}
      {can('read') && <><form className="certification-filters" onSubmit={e => { e.preventDefault(); if (!blocked) setFilter(old => ({ ...old, query })); }}>
        <div className="field-group"><label htmlFor={`${prefix}-query`}>Tìm theo tên hoặc đơn vị cấp</label><input id={`${prefix}-query`} maxLength={200} value={query} disabled={blocked} onChange={e => setQuery(e.target.value)} /></div>
        <div className="field-group"><label htmlFor={`${prefix}-status`}>Trạng thái</label><select id={`${prefix}-status`} value={filter.status} disabled={blocked} onChange={e => setFilter(old => ({ ...old, status: e.target.value }))}>
          <option value="All">Tất cả</option><option value="Active">Còn hiệu lực</option><option value="Expired">Đã hết hạn</option><option value="Archived">Đã lưu trữ</option></select></div>
        <button disabled={blocked}>Tìm chứng chỉ</button><button type="button" disabled={blocked} onClick={() => { setQuery(''); setFilter({ status: 'All', query: '' }); }}>Xóa bộ lọc</button>
      </form>{!loading && !error && items.length === 0 && <p>{filter.query || filter.status !== 'All' ? 'Chưa có chứng chỉ khớp bộ lọc này.' : 'Bắt đầu bằng chứng chỉ đầu tiên của bạn.'}</p>}
      <div className="resource-cards">{items.map(row => <article className="resource-card" key={row.id}><div><span className="state-pill">{stateLabel[row.effectiveStatus]}</span><h2>{row.title}</h2><p>{row.issuer}</p><p>{row.expiresOn ? 'Hết hạn: ' + date(row.expiresOn) : 'Không ghi ngày hết hạn'}</p></div><button disabled={blocked} onClick={() => open(row)}>Xem chứng chỉ</button></article>)}</div>
      {cursor && <button disabled={blocked} onClick={() => void more('list')}>Xem thêm chứng chỉ</button>}</>}
      {!can('read') && can('create') && <p>Bạn có thể thêm chứng chỉ. Xem thông tin đã lưu cần quyền đọc riêng.</p>}
    </>}
    {detail && <article className="resource-card"><div><span className="state-pill">{stateLabel[detail.effectiveStatus]}</span><h2>{detail.issuer}</h2><dl><dt>Ngày cấp</dt><dd>{date(detail.issuedOn)}</dd><dt>Ngày hết hạn</dt><dd>{detail.expiresOn ? date(detail.expiresOn) : 'Không ghi ngày hết hạn'}</dd><dt>Đường dẫn xác minh</dt><dd>{detail.verificationUrl ?? 'Chưa ghi nhận'}</dd></dl>
      {detail.status === 'Archived' && <p>Chứng chỉ đang được lưu trữ ở chế độ chỉ đọc. Bỏ lưu trữ trước khi cập nhật.</p>}
      <div className="resource-actions">{detail.status === 'Active' && can('update') && <button disabled={blocked} onClick={() => begin('edit', detail)}>Sửa thông tin</button>}
      {detail.status === 'Active' && can('renew') && <button disabled={blocked} onClick={() => begin('renew', detail)}>Ghi nhận gia hạn</button>}
      {can(detail.status === 'Archived' ? 'unarchive' : 'archive') && <button disabled={blocked} onClick={() => void lifecycle(detail)}>{detail.status === 'Archived' ? 'Bỏ lưu trữ' : 'Lưu trữ'}</button>}
      {history === null && <button disabled={blocked} onClick={() => void more('history')}>Xem lịch sử</button>}</div>
      {history && <section aria-label="Lịch sử chứng chỉ"><h3>Lịch sử chứng chỉ</h3>{history.length === 0 && <p role="status">Chưa có lần thay đổi nào được ghi nhận.</p>}{history.map(version => <article key={version.id}><h4>Phiên bản {version.versionNumber} · {historyLabel[version.operation] ?? 'Ghi nhận thay đổi'}</h4><p>{version.snapshot.issuer} · Ngày cấp: {date(version.snapshot.issuedOn)} · Hết hạn: {date(version.snapshot.expiresOn)}</p>{version.reason && <p>{version.reason}</p>}</article>)}{historyCursor && <button disabled={blocked} onClick={() => void more('history')}>Lịch sử trước đó</button>}</section>}
    </div></article>}
    <ResourceFormDialog open={editor !== null && leave === null} title={editor?.mode === 'create' ? 'Thêm chứng chỉ' : editor?.mode === 'renew' ? 'Ghi nhận gia hạn' : 'Sửa thông tin chứng chỉ'} busy={busy} dirty={dirty && !uncertain} onClose={closeEditor}>
      <form className="resource-form" onSubmit={e => void save(e)}>{formError && <p role="alert">{formError}</p>}
        {uncertain && <p role="status">Chưa rõ kết quả lưu. Thử lại cùng yêu cầu để khôi phục xác nhận; dữ liệu có thể đã được lưu.</p>}
        {conflict && <p role="status">Thông tin đã thay đổi ở nơi khác. Đóng biểu mẫu và tải lại trước khi sửa trên phiên bản mới.</p>}
        {editor?.mode !== 'renew' && <><label htmlFor={`${prefix}-name`}>Tên chứng chỉ</label><input id={`${prefix}-name`} required maxLength={200} value={draft.title} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, title: e.target.value }))} />
          <label htmlFor={`${prefix}-issuer`}>Đơn vị cấp</label><input id={`${prefix}-issuer`} required maxLength={200} value={draft.issuer} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, issuer: e.target.value }))} />
          <label htmlFor={`${prefix}-url`}>Đường dẫn xác minh (tùy chọn)</label><input id={`${prefix}-url`} type="url" maxLength={2048} value={draft.verificationUrl} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, verificationUrl: e.target.value }))} /></>}
        {editor?.mode !== 'edit' && <><label htmlFor={`${prefix}-issued`}>Ngày cấp (tùy chọn)</label><input id={`${prefix}-issued`} type="date" value={draft.issuedOn} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, issuedOn: e.target.value }))} />
          <label htmlFor={`${prefix}-expiry`}>Ngày hết hạn (tùy chọn)</label><input id={`${prefix}-expiry`} type="date" min={draft.issuedOn || undefined} value={draft.expiresOn} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, expiresOn: e.target.value }))} /></>}
        {editor?.mode === 'renew' && <><p>Ngày cấp và ngày hết hạn trước đó được giữ trong lịch sử. Thao tác này không tự tạo nhắc hạn.</p><label htmlFor={`${prefix}-reason`}>Ghi chú gia hạn (tùy chọn)</label><textarea id={`${prefix}-reason`} maxLength={2000} value={draft.reason} disabled={busy || uncertain || conflict} onChange={e => setDraft(old => ({ ...old, reason: e.target.value }))} /></>}
        <button className="primary-button" disabled={busy || conflict}>{uncertain ? 'Thử lại cùng yêu cầu' : 'Lưu chứng chỉ'}</button>
      </form>
    </ResourceFormDialog>
    {preview && leave === null && <ActionDialog title={preview.operation === 'archive' ? 'Lưu trữ chứng chỉ?' : 'Bỏ lưu trữ chứng chỉ?'} description="Chỉ thay đổi trạng thái chứng chỉ này. Lịch sử và các ngày đã ghi được giữ nguyên." confirmLabel={uncertain ? 'Thử lại cùng yêu cầu' : preview.operation === 'archive' ? 'Lưu trữ' : 'Bỏ lưu trữ'} confirmDisabled={busy} onClose={() => {
      if (flight.current) return;
      if (original.current) setLeave({ proceed: () => { setPreview(null); setUncertain(false); void load(); } }); else { setPreview(null); setFormError(null); }
    }} onConfirm={async () => { const selected = preview; const key = crypto.randomUUID(); return await perform(original.current ?? { send: () => certificationTransition(selected, key), label: selected.operation === 'archive' ? 'Đã lưu trữ chứng chỉ.' : 'Đã bỏ lưu trữ chứng chỉ.' }); }}>{formError && <p role="alert">{formError}</p>}{uncertain && <p>Kết quả chưa rõ; xác nhận lại sẽ gửi đúng yêu cầu trước đó.</p>}</ActionDialog>}
    {leave && <ActionDialog title="Rời khi chưa xác nhận kết quả?" description="Yêu cầu có thể đã được lưu. Rời màn hình không hoàn tác dữ liệu đã lưu trên máy chủ." confirmLabel="Rời màn hình" confirmDisabled={busy} onClose={() => { if (!flight.current) setLeave(null); }} onConfirm={() => { if (flight.current) return false; const proceed = leave.proceed; original.current = null; setUncertain(false); setLeave(null); proceed(); return true; }} />}
  </section>;
}
