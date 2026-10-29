import { useEffect, useRef, useState, type FormEvent } from 'react';
import { createIdempotencyKey, NexoraApiError, removeReadingItem, type ReadingItemRecord } from './api';
import { newsArticle, newsArticles, newsCapabilities, newsClearHistory, newsFollow, newsHistory, newsMarkAllRead, newsMarkRead, newsSaveArticle, newsSaved, newsSources, newsSourceState, newsUpdateSource, type NewsArticle, type NewsSource, type NewsHistoryItem } from './newsReaderApi';
import { newsCategoryList, type NewsCategoryRecord } from './newsCategoryApi';
import { NewsCategoryScreen } from './NewsCategoryScreen';
import { ResourceFormDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard, requestDirtyLeave } from './navigationGuard';

export function NewsReaderScreen({ onAuthLost }: { onAuthLost: () => void }) {
  const [tab, setTab] = useState<'articles' | 'sources' | 'saved' | 'history' | 'categories'>('articles'), [caps, setCaps] = useState<Record<string, boolean>>({});
  const [sourceId, setSourceId] = useState(''), [loading, setLoading] = useState(true), [error, setError] = useState<string | null>(null);
  const epoch = useRef(0);
  async function loadCapabilities() {
    const stamp = ++epoch.current; setLoading(true); setError(null); setCaps({});
    try { const result = await newsCapabilities(); if (stamp === epoch.current) setCaps(result); }
    catch (value) { if (stamp !== epoch.current) return; setError(value instanceof Error ? value.message : 'News không khả dụng.'); if ((value as { status?: number }).status === 401) onAuthLost(); }
    finally { if (stamp === epoch.current) setLoading(false); }
  }
  useEffect(() => { void loadCapabilities(); return () => { ++epoch.current; }; }, []);
  return <section className="content-section"><h1>News · Stored reader</h1><p>Đọc dữ liệu đã lưu. URL mới chưa có snapshot được duyệt sẽ chưa thể follow; fetch/refresh ra mạng chưa khả dụng.</p>
    <nav aria-label="News views">{(['articles', 'sources', 'saved', 'history', 'categories'] as const).map(view => <button key={view} aria-current={tab === view ? 'page' : undefined} onClick={() => requestDirtyLeave(() => setTab(view))}>{view === 'articles' ? 'Articles' : view === 'sources' ? 'Sources' : view === 'saved' ? 'Saved / Read Later' : view === 'history' ? 'History' : 'Categories'}</button>)}
      <button onClick={() => requestDirtyLeave(() => void loadCapabilities())}>Kiểm tra lại quyền News</button></nav>
    {error && <p role="alert">{error}</p>}
    {tab === 'categories' ? <NewsCategoryScreen onAuthLost={onAuthLost} /> : loading ? <p role="status">Đang đọc quyền News…</p> : <ReaderPane key={tab + sourceId} mode={tab} sourceId={sourceId} caps={caps} onAuthLost={onAuthLost} onOpenSource={id => requestDirtyLeave(() => { setSourceId(id); setTab('articles'); })} />}
  </section>;
}

const emptyDraft = { url: '', categoryId: '', titleOverride: '' };
function ReaderPane({ mode, sourceId, caps, onAuthLost, onOpenSource }: {
  mode: 'sources' | 'articles' | 'saved' | 'history'; sourceId: string; caps: Record<string, boolean>; onAuthLost: () => void; onOpenSource: (id: string) => void;
}) {
  const [sources, setSources] = useState<NewsSource[]>([]), [articles, setArticles] = useState<NewsArticle[]>([]), [detail, setDetail] = useState<NewsArticle | null>(null);
  const [history, setHistory] = useState<NewsHistoryItem[]>([]), [watermark, setWatermark] = useState<string | null>(null), [bulk, setBulk] = useState<'read' | 'history' | null>(null);
  const [saved, setSaved] = useState<ReadingItemRecord[]>([]), [unsaving, setUnsaving] = useState<ReadingItemRecord | null>(null);
  const [cursor, setCursor] = useState<string | null>(null), [loaded, setLoaded] = useState(false), [busy, setBusy] = useState(false), [uncertain, setUncertain] = useState(false);
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null), [denied, setDenied] = useState(false);
  const [categories, setCategories] = useState<NewsCategoryRecord[]>([]), [categoryCursor, setCategoryCursor] = useState<string | null>(null);
  const [draft, setDraft] = useState(emptyDraft), [editing, setEditing] = useState<NewsSource | null>(null), [unfollow, setUnfollow] = useState<NewsSource | null>(null);
  const [conflict, setConflict] = useState(false);
  const [filters, setFilters] = useState({ query: '', categoryId: '', state: '', unreadOnly: false, subscriptionId: sourceId, from: '', to: '' });
  const [filterDraft, setFilterDraft] = useState(filters), [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const epoch = useRef(0), mounted = useRef(false), flight = useRef(false), pending = useRef<(() => Promise<unknown>) | null>(null), baseline = useRef(JSON.stringify(emptyDraft));
  const dirty = JSON.stringify(draft) !== baseline.current || uncertain;
  const can = (key: string) => !denied && caps['news.' + key] === true;
  const readAction = mode === 'sources' ? 'source.read' : mode === 'history' ? 'history.read' : 'article.read';
  const blocked = busy || uncertain || pending.current !== null;
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; }; }, []);
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !pending.current) return false; setLeave({ proceed }); return true; }, 1), [dirty, busy]);
  useEffect(() => {
    if (!dirty && !pending.current) return;
    const prevent = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
  }, [dirty, busy]);
  function clear() { ++epoch.current; setSources([]); setArticles([]); setHistory([]); setSaved([]); setWatermark(null); setDetail(null); setCursor(null); setLoaded(false); setCategories([]); setCategoryCursor(null); }
  function failure(value: unknown) {
    const apiError = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể hoàn tất News.', 0, 'NetworkError'); setError(apiError.message);
    if ([401, 403].includes(apiError.status) || ['ModuleUnavailable', 'ActionDenied', 'DependencyUnavailable', 'SourceUnavailable'].includes(apiError.code ?? '')) {
      clear(); setDenied(true); pending.current = null; setUncertain(false); setDraft(emptyDraft); baseline.current = JSON.stringify(emptyDraft); setEditing(null); setUnfollow(null);
    }
    else if (apiError.status === 404 || apiError.code === 'ScopeUnavailable') { clear(); setDraft(emptyDraft); baseline.current = JSON.stringify(emptyDraft); setEditing(null); setUnfollow(null); setUnsaving(null); setBulk(null); }
    if (apiError.status === 401) onAuthLost(); return apiError;
  }
  async function load(append = false) {
    if (pending.current || flight.current || busy) return; const stamp = ++epoch.current; setBusy(true); setError(null); setDetail(null);
    try {
      if (mode === 'sources') {
        const page = await newsSources(filters, append ? cursor ?? undefined : undefined);
        if (!mounted.current || stamp !== epoch.current) return;
        setSources(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor);
      } else if (mode === 'saved') {
        const page = await newsSaved(append ? cursor ?? undefined : undefined);
        if (!mounted.current || stamp !== epoch.current) return;
        setSaved(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor);
      } else if (mode === 'history') {
        const page = await newsHistory(append ? cursor ?? undefined : undefined);
        if (!mounted.current || stamp !== epoch.current) return;
        setHistory(old => append ? [...new Map([...old, ...page.items].map(item => [item.articleId, item])).values()] : page.items); setCursor(page.nextCursor); setWatermark(page.watermark);
      } else {
        const page = await newsArticles(filters, append ? cursor ?? undefined : undefined);
        if (!mounted.current || stamp !== epoch.current) return;
        setArticles(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCursor(page.nextCursor); setWatermark(page.watermark);
      }
      setLoaded(true);
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); }
    finally { if (mounted.current) setBusy(false); }
  }
  useEffect(() => { if (can(readAction)) void load(); }, [filters]);
  async function categoryChoices(append = false) {
    if (blocked) return; const stamp = ++epoch.current; setBusy(true); setError(null);
    try { const page = await newsCategoryList('', append ? categoryCursor ?? undefined : undefined); if (!mounted.current || stamp !== epoch.current) return;
      setCategories(old => append ? [...new Map([...old, ...page.items].map(item => [item.id, item])).values()] : page.items); setCategoryCursor(page.nextCursor);
    } catch (value) { if (mounted.current && stamp === epoch.current) failure(value); } finally { if (mounted.current) setBusy(false); }
  }
  async function execute() {
    const intent = pending.current; if (!intent || flight.current) return; flight.current = true; ++epoch.current; setBusy(true); setError(null); setNotice(null);
    try {
      await intent(); if (!mounted.current || pending.current !== intent) return;
      pending.current = null; setUncertain(false); clear(); setDraft(emptyDraft); baseline.current = JSON.stringify(emptyDraft); setEditing(null); setConflict(false); setUnfollow(null); setBulk(null); setUnsaving(null);
      setNotice('Đã ghi nhận. Chọn tải lại để đọc trạng thái và revision hiện tại.');
    } catch (value) {
      if (!mounted.current || pending.current !== intent) return;
      const apiError = failure(value), unknown = !apiError.status || apiError.status >= 500 || ['RequestInProgress', 'ReceiptUnavailable'].includes(apiError.code ?? '');
      setUncertain(unknown); if (!unknown) pending.current = null; setUnfollow(null); setBulk(null); setUnsaving(null);
      if (apiError.status === 412 && editing) setConflict(true);
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function save(event: FormEvent) {
    event.preventDefault(); if (blocked || conflict) return;
    const body = { categoryId: draft.categoryId || null, titleOverride: draft.titleOverride.trim() || null }, key = createIdempotencyKey();
    if (editing) { const original = { ...editing }; pending.current = () => newsUpdateSource(original, body, key); }
    else { const request = { ...body, url: draft.url.trim() }; pending.current = () => newsFollow(request, key); }
    void execute();
  }
  function transition(source: NewsSource, operation: 'pause' | 'resume' | 'unfollow') {
    if (blocked) return; const original = { ...source }, key = createIdempotencyKey(); pending.current = () => newsSourceState(original, operation, key); void execute();
  }
  async function open(article: Pick<NewsArticle, 'id'>) {
    if (blocked) return; const stamp = ++epoch.current; setBusy(true); setDetail(null); setError(null);
    try { const current = await newsArticle(article.id); if (mounted.current && stamp === epoch.current) { setDetail(current); setArticles(old => old.map(item => item.id === current.id ? { ...current, body: null } : item)); } }
    catch (value) { if (mounted.current && stamp === epoch.current) failure(value); } finally { if (mounted.current) setBusy(false); }
  }
  function mark(article: NewsArticle) {
    if (blocked) return; const original = { ...article }, read = !article.read, key = createIdempotencyKey(); pending.current = () => newsMarkRead(original, read, key); void execute();
  }
  function saveArticle(article: NewsArticle) {
    if (blocked) return; const id = article.id, key = createIdempotencyKey(); pending.current = () => newsSaveArticle(id, key); void execute();
  }
  function confirmBulk() {
    if (blocked || !watermark || !bulk) return;
    const through = watermark, key = createIdempotencyKey();
    if (bulk === 'read') { if (!filters.subscriptionId) return; const selected = filters.subscriptionId; pending.current = () => newsMarkAllRead(selected, through, key); }
    else pending.current = () => newsClearHistory(through, key);
    void execute();
  }
  const categoryOptions = <>{categories.map(category => <option key={category.id} value={category.id}>{category.metadata.name}</option>)}</>;
  return <section aria-label={mode === 'sources' ? 'Stored News sources' : 'Stored News articles'}>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}{busy && <p role="status">Đang xử lý News…</p>}
    {uncertain && <button disabled={busy} onClick={() => void execute()}>Đối chiếu đúng request/key cũ</button>}
    <button disabled={blocked || !can(readAction)} onClick={() => void load()}>Tải lại {mode}</button>
    {!can(readAction) && <p>Quyền đọc này hiện không khả dụng.</p>}
    {mode !== 'history' && mode !== 'saved' && <form className="resource-form" onSubmit={event => {
      event.preventDefault(); if (blocked) return;
      const instant = (value: string) => !value || (/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:\d{2})$/.test(value) && Number.isFinite(Date.parse(value)));
      if (!instant(filterDraft.from) || !instant(filterDraft.to) || (filterDraft.from && filterDraft.to && Date.parse(filterDraft.from) >= Date.parse(filterDraft.to))) { setError('Nhập ISO 8601 với UTC offset, từ trước đến.'); return; }
      clear(); setFilters({ ...filterDraft, from: filterDraft.from ? new Date(filterDraft.from).toISOString() : '', to: filterDraft.to ? new Date(filterDraft.to).toISOString() : '' });
    }}><fieldset disabled={blocked}><legend>Lọc {mode}</legend>
      <label>Title/source<input maxLength={mode === 'sources' ? 200 : 500} value={filterDraft.query} onChange={event => setFilterDraft(old => ({ ...old, query: event.target.value }))} /></label>
      <button type="button" onClick={() => void categoryChoices()}>Đọc category picker</button>{categoryCursor && <button type="button" onClick={() => void categoryChoices(true)}>Thêm categories</button>}
      <label>Category<select value={filterDraft.categoryId} onChange={event => setFilterDraft(old => ({ ...old, categoryId: event.target.value }))}><option value="">Tất cả</option>{filterDraft.categoryId && !categories.some(value => value.id === filterDraft.categoryId) && <option value={filterDraft.categoryId}>Category đã chọn (chưa đọc label)</option>}{categoryOptions}</select></label>
      {mode === 'sources' ? <label>State<select value={filterDraft.state} onChange={event => setFilterDraft(old => ({ ...old, state: event.target.value }))}><option value="">Đang theo dõi + paused</option>{['Followed', 'Paused', 'Unfollowed'].map(state => <option key={state}>{state}</option>)}</select></label> : <><label><input type="checkbox" checked={filterDraft.unreadOnly} onChange={event => setFilterDraft(old => ({ ...old, unreadOnly: event.target.checked }))} />Chỉ unread</label>{filterDraft.subscriptionId && <button type="button" onClick={() => setFilterDraft(old => ({ ...old, subscriptionId: '' }))}>Bỏ source filter</button>}</>}
      {mode === 'articles' && <><label>Từ thời điểm UTC (bao gồm)<input maxLength={40} value={filterDraft.from} placeholder="2026-10-01T00:00:00Z" onChange={event => setFilterDraft(old => ({ ...old, from: event.target.value }))} /></label><label>Đến thời điểm UTC (không bao gồm)<input maxLength={40} value={filterDraft.to} placeholder="2026-11-01T00:00:00Z" onChange={event => setFilterDraft(old => ({ ...old, to: event.target.value }))} /></label></>}
      <button type="submit">Áp dụng</button></fieldset></form>}
    {mode === 'articles' && can('article.mark_all_read') && <button disabled={blocked || !loaded || !watermark || !filters.subscriptionId} onClick={() => setBulk('read')}>Mark all read trong source đang chọn</button>}
    {mode === 'history' && can('history.clear') && <button disabled={blocked || !loaded || !watermark} onClick={() => setBulk('history')}>Clear history đến watermark đã đọc</button>}
    {mode === 'saved' && <p>Canonical Read Later references · sắp xếp theo SavedAt, 25/trang. Gỡ ref giữ nguyên nguồn/read state.</p>}
    {loaded && !error && !(mode === 'sources' ? sources : mode === 'history' ? history : mode === 'saved' ? saved : articles).length && <p>Không có {mode} phù hợp trong dữ liệu đã lưu.</p>}
    {mode === 'sources' ? <>
      {sources.map(source => <article className="resource-card" key={source.id}><h2>{source.title}</h2><p>{source.url}</p><p>{source.state} · {source.fetchState}</p><p>Last attempt: {source.lastAttempt ?? 'chưa có'} · Last success: {source.lastSuccess ?? 'chưa có'}</p>{source.lastErrorCode && <p>{source.lastErrorCode}</p>}
        <button disabled={blocked || source.state === 'Unfollowed' || !can('article.read')} onClick={() => onOpenSource(source.id)}>Đọc articles</button>
        {can('source.update') && source.state !== 'Unfollowed' && <button disabled={blocked} onClick={() => requestDirtyLeave(() => { const next = { url: source.url, titleOverride: source.titleOverride ?? '', categoryId: source.categoryId ?? '' }; setEditing(source); setConflict(false); setDraft(next); baseline.current = JSON.stringify(next); })}>Sửa subscription</button>}
        {source.state === 'Followed' && can('source.pause') && <button disabled={blocked} onClick={() => transition(source, 'pause')}>Pause</button>}
        {source.state === 'Paused' && can('source.resume') && <button disabled={blocked} onClick={() => transition(source, 'resume')}>Resume</button>}
        {source.state !== 'Unfollowed' && can('source.unfollow') && <button disabled={blocked} onClick={() => setUnfollow(source)}>Unfollow</button>}
      </article>)}
      {(can('source.follow') || editing) && <form className="resource-form" onSubmit={save}><fieldset disabled={blocked}><legend>{editing ? 'Sửa subscription' : 'Follow source đã lưu'}</legend>
        {conflict && <div role="alert"><p>Revision đã thay đổi. Tải lại sources và so sánh bản hiện tại trước khi Reapply; bản nháp đang được giữ.</p>{sources.find(source => source.id === editing?.id) && <p>Current title: {sources.find(source => source.id === editing?.id)?.titleOverride ?? '(dùng title nguồn)'}</p>}<button type="button" disabled={!loaded || !sources.some(source => source.id === editing?.id && source.etag !== editing.etag && source.state !== 'Unfollowed')} onClick={() => { const current = sources.find(source => source.id === editing?.id); if (!current || current.state === 'Unfollowed' || current.etag === editing?.etag) return; setEditing(current); baseline.current = JSON.stringify({ url: current.url, titleOverride: current.titleOverride ?? '', categoryId: current.categoryId ?? '' }); setConflict(false); setNotice('Đã lấy revision mới; kiểm tra bản nháp rồi Lưu để áp dụng.'); }}>Reapply draft với revision hiện tại</button></div>}
        <label>URL<input required disabled={editing !== null} maxLength={2048} type="url" value={draft.url} onChange={event => setDraft(old => ({ ...old, url: event.target.value }))} /></label>
        <label>Title override<input maxLength={200} value={draft.titleOverride} onChange={event => setDraft(old => ({ ...old, titleOverride: event.target.value }))} /></label>
        <button type="button" onClick={() => void categoryChoices()}>Đọc categories hiện tại</button><label>Category<select value={draft.categoryId} onChange={event => setDraft(old => ({ ...old, categoryId: event.target.value }))}><option value="">Không category</option>{draft.categoryId && !categories.some(value => value.id === draft.categoryId) && <option value={draft.categoryId}>Category đang gắn (chưa đọc label)</option>}{categoryOptions}</select></label>
        <button type="submit" disabled={conflict}>{editing ? 'Lưu subscription' : 'Follow stored source'}</button>{editing && <button type="button" onClick={() => requestDirtyLeave(() => { setEditing(null); setConflict(false); setDraft(emptyDraft); baseline.current = JSON.stringify(emptyDraft); })}>Cancel edit</button>}
      </fieldset></form>}
    </> : <>
      {detail && <article className="form-panel"><h2>{detail.title}</h2><p>{detail.sourceTitle} · {detail.publishedAt ?? detail.fetchedAt}{!detail.publishedAt && ' (fallback fetched time)'}</p><p>{detail.isExcerpt ? 'Excerpt do feed cung cấp' : 'Nội dung đã lưu do feed cung cấp'}</p><pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{detail.body ?? 'Feed không có nội dung để đọc.'}</pre><p>Attribution URL: {detail.originalUrl}</p><button disabled={blocked} onClick={() => setDetail(null)}>Đóng reader</button>{can('article.save') && <button disabled={blocked} onClick={() => saveArticle(detail)}>Lưu vào Read Later</button>}{can(detail.read ? 'article.mark_unread' : 'article.mark_read') && <button disabled={blocked} onClick={() => mark(detail)}>{detail.read ? 'Mark unread' : 'Mark read'}</button>}</article>}
      {mode === 'saved' ? saved.map(item => <article key={item.id} className="resource-card"><h2>{item.safeTitleSnapshot}</h2><p>{item.state} · saved {item.savedAt}</p>{item.sourceAvailable ? <button disabled={blocked} onClick={() => void open({ id: item.sourceId })}>Đọc nguồn trong Nexora</button> : <p>Nguồn không khả dụng; retained reference.</p>}{caps['reading.item.remove'] && <button disabled={blocked} onClick={() => setUnsaving(item)}>Gỡ khỏi Saved / Read Later</button>}</article>) : mode === 'history' ? history.map(item => <article key={item.articleId} className="resource-card"><h2>{item.article?.title ?? 'Nguồn không khả dụng'}</h2><p>Opened: {item.openedAt}</p>{item.article && <button disabled={blocked} onClick={() => void open(item.article!)}>Đọc nguồn hiện tại</button>}</article>) : articles.map(article => <article key={article.id} className="resource-card"><h2>{article.title}</h2><p>{article.sourceTitle} · {article.read ? 'Read' : 'Unread'} · {article.publishedAt ?? article.fetchedAt}</p><button disabled={blocked} onClick={() => void open(article)}>Đọc trong Nexora</button><button disabled={blocked} onClick={() => { clear(); const next = { ...filters, subscriptionId: article.subscriptionId }; setFilters(next); setFilterDraft(next); }}>Lọc source này</button>{can('article.save') && <button disabled={blocked} onClick={() => saveArticle(article)}>Lưu vào Read Later</button>}{can(article.read ? 'article.mark_unread' : 'article.mark_read') && <button disabled={blocked} onClick={() => mark(article)}>{article.read ? 'Mark unread' : 'Mark read'}</button>}</article>)}
    </>}
    {cursor && <button disabled={blocked} onClick={() => void load(true)}>Tải thêm · 25 mục</button>}
    <ResourceFormDialog open={unsaving !== null} title="Gỡ saved reference" dirty={false} busy={busy} onClose={() => setUnsaving(null)}><p>Gỡ cùng canonical ref khỏi cả Saved và Read Later; giữ article/subscription/read state.</p><button disabled={blocked} onClick={() => { if (!unsaving || blocked) return; const id = unsaving.id, etag = unsaving.etag, key = createIdempotencyKey(); pending.current = () => removeReadingItem(id, etag, key); void execute(); }}>Xác nhận gỡ reference</button></ResourceFormDialog>
    <ResourceFormDialog open={bulk !== null} title={bulk === 'read' ? 'Mark source read' : 'Clear private history'} dirty={false} busy={busy} onClose={() => setBulk(null)}><p>{bulk === 'read' ? 'Đánh dấu toàn bộ articles trong đúng source đã chọn, fetched đến watermark này; không chỉ những mục khớp bộ lọc title/date. Article mới fetched sau mốc vẫn unread.' : 'Chỉ xóa lịch sử mở đến mốc này; giữ read-state, audit, source và lần mở mới sau mốc.'}</p><p>{watermark}</p><button disabled={blocked || !watermark} onClick={confirmBulk}>Xác nhận phạm vi và watermark</button></ResourceFormDialog>
    <ResourceFormDialog open={unfollow !== null} title="Unfollow source" dirty={false} busy={busy} onClose={() => setUnfollow(null)}><p>Dừng theo dõi; giữ read state, public articles và saved references. Không xóa dữ liệu owner khác.</p><button disabled={blocked} onClick={() => unfollow && transition(unfollow, 'unfollow')}>Xác nhận unfollow</button></ResourceFormDialog>
    <ResourceFormDialog open={leave !== null} title="Rời News" dirty={false} busy={busy} onClose={() => setLeave(null)}><p>Rời màn hình sẽ mất bản nháp/mã đối chiếu; request đã gửi có thể đã commit.</p><button disabled={busy} onClick={() => { const proceed = leave?.proceed; pending.current = null; ++epoch.current; setLeave(null); proceed?.(); }}>Bỏ bản nháp/mã đối chiếu và tiếp tục</button></ResourceFormDialog>
  </section>;
}
