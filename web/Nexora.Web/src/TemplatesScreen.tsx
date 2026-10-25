import { useCallback, useEffect, useId, useRef, useState, type FormEvent } from 'react';
import { ActionDialog } from './App';
import { NexoraApiError } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { getTemplate, listTemplates, prepareTemplateDraft, sendTemplateIntent, templateCapabilities,
  type BookmarkTemplateSeed, type TemplateDetail, type TemplateIntent, type TemplatePage, type TemplateSummary } from './templateApi';
import './templates.css';

type Editor = { item?: TemplateSummary; name: string; title: string; url: string; description: string; baseline: string };
const emptyEditor = (): Editor => ({ name: '', title: '', url: '', description: '', baseline: JSON.stringify(['', '', '', '']) });
const draft = (e: Editor) => JSON.stringify([e.name, e.title, e.url, e.description]);
const message = (e: unknown) => e instanceof Error ? e.message : 'Không thể hoàn tất thao tác.';
const labels: Record<string, string> = { Active: 'Đang dùng', Archived: 'Đã lưu trữ', Trash: 'Thùng rác', archive: 'Lưu trữ', unarchive: 'Bỏ lưu trữ', trash: 'Đưa vào Thùng rác', restore: 'Khôi phục', purge: 'Xóa vĩnh viễn' };

export function TemplatesScreen({ onAuthLost, onUseBookmarkTemplate }: {
  onAuthLost: () => void; onUseBookmarkTemplate?: (seed: BookmarkTemplateSeed) => void;
}) {
  const prefix = useId(), mounted = useRef(false), epoch = useRef(0), flight = useRef(false), original = useRef<TemplateIntent | null>(null);
  const [caps, setCaps] = useState<Record<string, boolean>>({}), [page, setPage] = useState<TemplatePage | null>(null);
  const [query, setQuery] = useState(''), [filter, setFilter] = useState({ query: '', state: 'Active', page: 1 });
  const [detail, setDetail] = useState<TemplateDetail | null>(null), [editor, setEditor] = useState<Editor | null>(null);
  const [transition, setTransition] = useState<{ item: TemplateSummary; action: TemplateIntent['action'] } | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false), [conflict, setConflict] = useState(false);
  const [error, setError] = useState<string | null>(null), [formError, setFormError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const clear = useCallback(() => { setPage(null); setDetail(null); setEditor(null); setTransition(null); setCaps({}); setUncertain(false); setConflict(false); original.current = null; }, []);
  const fail = useCallback((e: unknown) => {
    setError(message(e));
    if (e instanceof NexoraApiError && [401, 403, 404].includes(e.status)) { ++epoch.current; clear(); setLoading(false); if (e.status === 401) onAuthLost(); }
  }, [clear, onAuthLost]);
  const load = useCallback(async () => {
    const stamp = ++epoch.current; setLoading(true); setError(null); setPage(null); setDetail(null);
    try {
      const nextCaps = await templateCapabilities();
      if (!mounted.current || stamp !== epoch.current) return;
      setCaps(nextCaps);
      if (!nextCaps['organization.template.read']) { setPage(null); return; }
      const next = await listTemplates(filter.query, filter.state, filter.page);
      if (mounted.current && stamp === epoch.current) setPage(next);
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { if (mounted.current && stamp === epoch.current) setLoading(false); }
  }, [filter, fail]);
  useEffect(() => { mounted.current = true; void load(); return () => { mounted.current = false; ++epoch.current; }; }, [load]);
  useEffect(() => {
    const refresh = async () => {
      if (flight.current || original.current) return;
      const stamp = epoch.current;
      try {
        const next = await templateCapabilities();
        if (!mounted.current || stamp !== epoch.current) return;
        if (!next['organization.template.read'] || (editor && !next['organization.template.' + (editor.item ? 'update' : 'create')])) {
          clear(); setCaps(next); setNotice('Quyền sử dụng mẫu đã thay đổi. Dữ liệu riêng đã được đóng.'); return;
        }
        setCaps(next);
      } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    };
    window.addEventListener('focus', refresh); return () => window.removeEventListener('focus', refresh);
  }, [editor, clear, fail]);
  const can = (action: string) => caps['organization.template.' + action] === true;
  const blocked = loading || busy || editor !== null || transition !== null || uncertain;

  async function open(item: TemplateSummary, edit = false) {
    if (flight.current || blocked) return;
    const stamp = epoch.current; flight.current = true; setBusy(true); setError(null);
    try {
      const current = await getTemplate(item.id);
      if (!mounted.current || stamp !== epoch.current) return;
      setDetail(current);
      if (edit) {
        const e = { item: current.template, name: current.template.name, title: current.seed.title ?? '', url: current.seed.url ?? '', description: current.seed.description ?? '', baseline: '' };
        e.baseline = draft(e); setEditor(e); setConflict(false); setFormError(null);
      }
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function persist(intent: TemplateIntent): Promise<boolean> {
    if (flight.current) return false;
    original.current ??= Object.freeze({ ...intent }); intent = original.current;
    const stamp = epoch.current; flight.current = true; setBusy(true); setFormError(null); setError(null);
    try {
      await sendTemplateIntent(intent);
      if (!mounted.current || stamp !== epoch.current) return false;
      original.current = null; setUncertain(false); setEditor(null); setTransition(null); setDetail(null); setConflict(false);
      setNotice('Đã lưu thao tác. Dữ liệu đã tạo từ mẫu không bị thay đổi.'); await load(); return true;
    } catch (e) {
      if (!mounted.current || stamp !== epoch.current) return false;
      setFormError(message(e));
      const status = e instanceof NexoraApiError ? e.status : 0;
      if (status === 0 || status === 408 || status >= 500 || (status === 409 && e instanceof NexoraApiError && e.code === 'RequestInProgress')) setUncertain(true);
      else { original.current = null; setUncertain(false); if (status === 412) { setConflict(true); setDetail(null); } }
      if ([401, 403, 404].includes(status)) { clear(); fail(e); }
      return false;
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function save(event: FormEvent) {
    event.preventDefault(); if (!editor || conflict || busy || !can(editor.item ? 'update' : 'create')) return;
    void persist(original.current ?? { id: editor.item?.id, action: editor.item ? 'update' : 'create', etag: editor.item?.etag,
      key: crypto.randomUUID(), body: JSON.stringify({ name: editor.name, seed: { title: editor.title, url: editor.url, description: editor.description || null } }) });
  }
  function abandon() { original.current = null; setUncertain(false); setConflict(false); setEditor(null); setTransition(null); setFormError(null); void load(); }
  async function useTemplate() {
    if (!detail || !onUseBookmarkTemplate || !detail.canUse || blocked || flight.current) return;
    const stamp = epoch.current; flight.current = true; setBusy(true); setError(null);
    try {
      const prepared = await prepareTemplateDraft(detail.template);
      if (mounted.current && stamp === epoch.current) onUseBookmarkTemplate(prepared.seed);
    } catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  return <section className="content-section templates-screen" aria-labelledby={`${prefix}-title`}>
    <div className="content-heading"><div><p className="eyebrow">THƯ VIỆN CÁ NHÂN</p><h1 id={`${prefix}-title`}>Mẫu của bạn</h1><p>Lưu ý tưởng một lần, bắt đầu nhanh hơn lần sau.</p></div>
      {can('create') && <button className="primary-button" disabled={blocked} onClick={() => { setEditor(emptyEditor()); setFormError(null); setConflict(false); }}>Tạo mẫu mới</button>}</div>
    <div className="templates-note"><span aria-hidden="true">✦</span><p>Mẫu chỉ điền trước nội dung. Bạn luôn xem lại và lưu trong biểu mẫu tạo Bookmark; các mục đã tạo không đổi khi sửa mẫu.</p></div>
    {notice && <p role="status">{notice}</p>}{error && <div role="alert"><p>{error}</p><button className="secondary-button" disabled={blocked} onClick={() => void load()}>Thử lại</button></div>}
    <form className="templates-toolbar" onSubmit={e => { e.preventDefault(); if (!blocked) setFilter(f => ({ ...f, query, page: 1 })); }}>
      <label htmlFor={`${prefix}-query`}>Tìm mẫu<input id={`${prefix}-query`} placeholder="Tên mẫu…" maxLength={100} value={query} disabled={blocked} onChange={e => setQuery(e.target.value)} /></label>
      <label htmlFor={`${prefix}-state`}>Khu vực<select id={`${prefix}-state`} value={filter.state} disabled={blocked} onChange={e => setFilter(f => ({ ...f, state: e.target.value, page: 1 }))}>{['Active', 'Archived', 'Trash'].map(s => <option key={s} value={s}>{labels[s]}</option>)}</select></label>
      <button className="secondary-button" disabled={blocked}>Tìm kiếm</button><button type="button" className="secondary-button" disabled={blocked} onClick={() => { setQuery(''); setFilter(f => ({ ...f, query: '', page: 1 })); }}>Xóa bộ lọc</button>
    </form>
    {loading && <p role="status">Đang tải thư viện mẫu…</p>}
    {!loading && !error && !can('read') && <p role="status">Thư viện mẫu chưa khả dụng với quyền hiện tại.</p>}
    {!loading && !error && can('read') && page?.items.length === 0 && <div className="templates-empty"><span aria-hidden="true">▧</span><h2>{filter.query ? 'Chưa tìm thấy mẫu phù hợp' : 'Một khởi đầu gọn gàng'}</h2><p>{filter.query ? 'Thử tên khác hoặc xóa bộ lọc.' : filter.state === 'Active' ? 'Tạo mẫu Bookmark để lưu tiêu đề, đường dẫn và mô tả thường dùng.' : 'Chưa có mẫu trong khu vực này.'}</p></div>}
    <div className="templates-grid">{page?.items.map(item => <article className="resource-card template-card" key={item.id}><div><span className="template-kind">Bookmark · {labels[item.status]}</span><h2>{item.name}</h2><p>Mẫu nội dung cá nhân · không liên kết trực tiếp</p></div><div className="resource-actions"><button className="secondary-button" disabled={blocked} onClick={() => void open(item)}>Xem trước</button>
      {item.status !== 'Trash' && can('update') && <button className="secondary-button" disabled={blocked} onClick={() => void open(item, true)}>Sửa mẫu</button>}
      {(item.status === 'Active' ? ['archive', 'trash'] : item.status === 'Archived' ? ['unarchive', 'trash'] : ['restore', 'purge']).filter(can).map(action => <button className={action === 'purge' ? 'danger-button' : 'secondary-button'} key={action} disabled={blocked} onClick={() => { setTransition({ item, action: action as TemplateIntent['action'] }); setFormError(null); }}>{labels[action]}</button>)}</div></article>)}</div>
    {page && <div className="resource-actions"><button className="secondary-button" disabled={blocked || page.page <= 1} onClick={() => setFilter(f => ({ ...f, page: f.page - 1 }))}>Trang trước</button><span>Trang {page.page}</span><button className="secondary-button" disabled={blocked || !page.hasMore} onClick={() => setFilter(f => ({ ...f, page: f.page + 1 }))}>Trang sau</button></div>}
    {detail && !editor && <aside className="template-preview" aria-label="Xem trước mẫu"><div className="content-heading"><h2>{detail.template.name}</h2><button className="secondary-button" disabled={busy} onClick={() => setDetail(null)}>Đóng xem trước</button></div><dl><dt>Tiêu đề gợi ý</dt><dd>{detail.seed.title || 'Bạn sẽ điền khi tạo Bookmark'}</dd><dt>Đường dẫn</dt><dd>{detail.seed.url || 'Bạn sẽ điền khi tạo Bookmark'}</dd><dt>Mô tả</dt><dd>{detail.seed.description || 'Không có mô tả'}</dd></dl><p>{detail.unavailableReason ?? 'Bạn có thể chỉnh lại mọi nội dung trước khi lưu Bookmark mới.'}</p><button className="primary-button" disabled={blocked || !detail.canUse || !onUseBookmarkTemplate} onClick={() => void useTemplate()}>Dùng mẫu này</button>{!onUseBookmarkTemplate && <p>Biểu mẫu tạo Bookmark chưa được kết nối tại màn hình này.</p>}</aside>}
    <ResourceFormDialog open={editor !== null} title={editor?.item ? 'Chỉnh sửa mẫu' : 'Tạo mẫu Bookmark'} busy={busy} closeBlocked={uncertain} dirty={!!editor && draft(editor) !== editor.baseline} onClose={() => { setEditor(null); setFormError(null); setConflict(false); }}>
      {editor && <form className="resource-form" onSubmit={save}>
        {formError && <p role="alert">{formError}</p>}{uncertain && <p role="status">Chưa rõ kết quả lưu. Hãy thử lại cùng yêu cầu; nội dung đang được giữ nguyên.</p>}
        {conflict && <p role="alert">Mẫu đã thay đổi ở nơi khác. Bản nháp được giữ lại; hãy so sánh trước khi áp dụng lại.</p>}
        <label htmlFor={`${prefix}-name`}>Tên mẫu<input id={`${prefix}-name`} required maxLength={100} value={editor.name} disabled={busy || uncertain} onChange={e => setEditor({ ...editor, name: e.target.value })} /></label>
        <fieldset disabled={busy || uncertain}><legend>Nội dung điền trước · không bắt buộc</legend><label htmlFor={`${prefix}-seed-title`}>Tiêu đề Bookmark<input id={`${prefix}-seed-title`} maxLength={200} value={editor.title} onChange={e => setEditor({ ...editor, title: e.target.value })} /></label><label htmlFor={`${prefix}-url`}>Đường dẫn HTTP(S)<input id={`${prefix}-url`} type="url" maxLength={2048} value={editor.url} onChange={e => setEditor({ ...editor, url: e.target.value })} /></label><label htmlFor={`${prefix}-description`}>Mô tả<textarea id={`${prefix}-description`} maxLength={4000} rows={4} value={editor.description} onChange={e => setEditor({ ...editor, description: e.target.value })} /></label></fieldset>
        <button className="primary-button" disabled={busy || conflict || editor.item?.status === 'Trash' || !can(editor.item ? 'update' : 'create')}>{uncertain ? 'Thử lại cùng yêu cầu' : 'Lưu mẫu'}</button>
        {uncertain && <button type="button" className="secondary-button" disabled={busy} onClick={abandon}>Đóng — thao tác có thể đã được lưu</button>}
        {conflict && editor.item && <button type="button" className="secondary-button" disabled={busy} onClick={async () => {
          const current = editor.item; if (!current || flight.current) return;
          const stamp = epoch.current; flight.current = true; setBusy(true);
          try { const latest = await getTemplate(current.id); if (mounted.current && stamp === epoch.current) { setDetail(latest); setEditor(e => e ? { ...e, item: latest.template } : e); setConflict(false); setFormError('Đã tải phiên bản hiện tại trong phần xem trước. Bản nháp vẫn được giữ; chỉ chọn Lưu sau khi so sánh.'); } }
          catch (e) { if (mounted.current && stamp === epoch.current) fail(e); }
          finally { flight.current = false; if (mounted.current) setBusy(false); }
        }}>Tải bản hiện tại để so sánh</button>}
        {detail && editor.item && <div className="template-preview"><h3>Bản hiện tại · {labels[detail.template.status]}</h3><p>{detail.template.name}</p><p>{detail.seed.title}</p><p>{detail.seed.url}</p><p>{detail.seed.description}</p></div>}
      </form>}
    </ResourceFormDialog>
    {transition && <ActionDialog title={`${labels[transition.action]} mẫu?`} description={`Chỉ mẫu “${transition.item.name}” bị ảnh hưởng (1 mẫu). ${transition.action === 'purge' ? 'Xóa vĩnh viễn không thể hoàn tác.' : ''} Những Bookmark đã tạo từ mẫu không thay đổi.`}
      confirmLabel={uncertain ? 'Thử lại cùng yêu cầu' : labels[transition.action]} confirmDisabled={busy || conflict} onClose={() => { if (!flight.current && !uncertain) { setTransition(null); setFormError(null); setConflict(false); } }}
      onConfirm={() => persist(original.current ?? { id: transition.item.id, action: transition.action, etag: transition.item.etag, key: crypto.randomUUID() })}>
      {formError && <p role="alert">{formError}</p>}{uncertain && <><p role="status">Kết quả chưa rõ. Thử lại cùng yêu cầu hoặc đóng để kiểm tra lại thư viện.</p><button className="secondary-button" disabled={busy} onClick={abandon}>Đóng — thao tác có thể đã được lưu</button></>}{conflict && <button className="secondary-button" onClick={abandon}>Đóng và tải lại</button>}
    </ActionDialog>}
  </section>;
}
