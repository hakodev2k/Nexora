import { useEffect, useRef, useState, type FormEvent } from 'react';
import { createIdempotencyKey, deleteSavedSearch, isProductivityAcknowledgement, listSavedSearches, NexoraApiError, runSavedSearch, saveSavedSearch,
  type SavedSearchDefinition, type SavedSearchRecord, type SearchPage, type SearchResultRecord } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { registerDirtyLeaveGuard } from './navigationGuard';

export function SavedSearches({ definition, onOpen, onAuthLost }: { definition: SavedSearchDefinition; onOpen: (item: SearchResultRecord) => void; onAuthLost: () => Promise<void> }) {
  const [items, setItems] = useState<SavedSearchRecord[]>([]), [cursor, setCursor] = useState<string | null>(null);
  const [title, setTitle] = useState(''), [editing, setEditing] = useState<SavedSearchRecord | null>(null);
  const [deleting, setDeleting] = useState<SavedSearchRecord | null>(null), [page, setPage] = useState<SearchPage | null>(null);
  const [busy, setBusy] = useState(false), [loaded, setLoaded] = useState(false), [uncertain, setUncertain] = useState(false);
  const [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const listEpoch = useRef(0), resultEpoch = useRef(0), flight = useRef(false), mounted = useRef(true);
  const pending = useRef<{ send: () => ReturnType<typeof saveSavedSearch> | ReturnType<typeof deleteSavedSearch>; definition?: SavedSearchDefinition } | null>(null);
  const running = useRef<string | null>(null);
  const [leave, setLeave] = useState<{ proceed: () => void } | null>(null);
  const dirty = title !== (editing?.title ?? '') || uncertain;
  useEffect(() => registerDirtyLeaveGuard(proceed => { if (!dirty && !pending.current) return false; setLeave({ proceed }); return true; }, 1), [dirty, busy]);
  useEffect(() => {
    if (!dirty && !pending.current) return;
    const prevent = (event: BeforeUnloadEvent) => { event.preventDefault(); event.returnValue = ''; };
    window.addEventListener('beforeunload', prevent); return () => window.removeEventListener('beforeunload', prevent);
  }, [dirty, busy]);
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++listEpoch.current; ++resultEpoch.current; pending.current = null; }; }, []);
  function failure(value: unknown) {
    const apiError = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể hoàn thành Saved Search.', 0, 'NetworkError');
    setError(apiError.message);
    if ([401, 403, 404].includes(apiError.status) || ['ModuleUnavailable', 'ActionDenied', 'DependencyUnavailable'].includes(apiError.code ?? '')) {
      ++listEpoch.current; ++resultEpoch.current; setItems([]); setCursor(null); setPage(null); setEditing(null); setTitle(''); setDeleting(null); pending.current = null; setUncertain(false); setLoaded(false);
    }
    if (apiError.status === 401) void onAuthLost(); return apiError;
  }
  async function load(append = false) {
    if (flight.current || uncertain) return; const stamp = ++listEpoch.current; setBusy(true); setError(null);
    try {
      const result = await listSavedSearches(append ? cursor ?? '' : '');
      if (!mounted.current || stamp !== listEpoch.current) return;
      setItems(old => append ? [...new Map([...old, ...result.items].map(item => [item.id, item])).values()] : result.items); setCursor(result.nextCursor); setLoaded(true);
    } catch (value) { if (mounted.current && stamp === listEpoch.current) failure(value); }
    finally { if (mounted.current) setBusy(false); }
  }
  async function execute() {
    const intent = pending.current; if (!intent || flight.current) return;
    flight.current = true; ++listEpoch.current; ++resultEpoch.current; setPage(null); setBusy(true); setError(null); setNotice(null);
    try {
      const result = await intent.send(); if (!mounted.current || pending.current !== intent) return;
      pending.current = null; setUncertain(false); setEditing(null); setDeleting(null); setTitle('');
      if (result === undefined || isProductivityAcknowledgement(result)) { setItems([]); setCursor(null); setLoaded(false); setNotice('Thao tác đã được lưu. Tải lại khi cần đọc danh sách hiện tại.'); }
      else { setItems(old => [result, ...old.filter(item => item.id !== result.id)]); setNotice('Đã lưu định nghĩa; kết quả luôn được truy vấn lại từ nguồn hiện tại.'); }
    } catch (value) {
      if (!mounted.current || pending.current !== intent) return; const apiError = failure(value);
      const unknown = apiError.status === 0 || apiError.status >= 500 || apiError.status === 408 || ['RequestInProgress', 'IdempotencyConflict'].includes(apiError.code ?? '');
      if (unknown && pending.current === intent) { setUncertain(true); setDeleting(null); } else pending.current = null;
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function save(event: FormEvent) {
    event.preventDefault(); if (pending.current || busy || !title.trim()) return;
    const key = createIdempotencyKey(), snapshot = { ...definition }, item = editing, name = title.trim();
    pending.current = { send: () => saveSavedSearch(item?.id ?? null, item?.etag ?? null, name, snapshot, key), definition: snapshot }; void execute();
  }
  async function run(item: SavedSearchRecord, append = false) {
    if (flight.current || busy || uncertain) return; const stamp = ++resultEpoch.current; setBusy(true); setError(null);
    if (!append) setPage(null);
    try {
      const result = await runSavedSearch(item.id, append ? page?.nextCursor ?? '' : '');
      if (!mounted.current || stamp !== resultEpoch.current) return;
      running.current = item.id;
      const available = new Set(result.providers.filter(provider => provider.state === 'Ready' || provider.state === 'Empty').map(provider => provider.resourceType));
      setPage(old => append && old ? { ...result, items: [...new Map([...old.items.filter(hit => available.has(hit.resourceType)), ...result.items].map(hit => [hit.resourceType + hit.id, hit])).values()] } : result);
    } catch (value) { if (mounted.current && stamp === resultEpoch.current) { setPage(null); failure(value); } }
    finally { if (mounted.current) setBusy(false); }
  }
  const blocked = busy || uncertain;
  return <section className="form-panel" aria-label="Saved Searches"><div className="section-heading"><h2>Saved Searches</h2><button type="button" disabled={blocked} onClick={() => void load()}>Đọc danh sách đã lưu</button></div>
    {error && <div role="alert" className="notice notice-error">{error}</div>}{notice && <p role="status">{notice}</p>}
    {uncertain && <div role="alert">Kết quả chưa rõ; thao tác đang được giữ nguyên.<button type="button" disabled={busy} onClick={() => void execute()}>Thử lại thao tác cũ</button></div>}
    <form onSubmit={save}><fieldset disabled={blocked}><legend>{editing ? 'Thay định nghĩa đã chọn' : 'Lưu bộ lọc hiện tại'}</legend><label>Tên Saved Search<input value={title} maxLength={100} onChange={event => setTitle(event.target.value)} required /></label>
      <p>{pending.current?.definition ? 'Định nghĩa đã gửi đang được giữ nguyên để đối chiếu; bộ lọc mới không thay thế thao tác này.' : 'Chỉ lưu định nghĩa sau đây, không lưu kết quả. Sửa các bộ lọc ở form tìm kiếm bên trên để thay đổi định nghĩa.'}</p><pre className="conflict-preview">{JSON.stringify(pending.current?.definition ?? definition, null, 2)}</pre>
      <button type="submit" disabled={!title.trim()}>{editing ? 'Xác nhận thay bằng định nghĩa phiên bản 1' : 'Lưu Saved Search'}</button>{editing && <button type="button" onClick={() => { setEditing(null); setTitle(''); }}>Hủy sửa</button>}
    </fieldset></form>
    {loaded && !error && items.length === 0 && <p>Chưa có Saved Search.</p>}
    <ul className="resource-cards">{items.map(item => <li className="resource-card" key={item.id}><div><strong>{item.title}</strong><p>{item.state === 'MigrationRequired' ? 'Cần định nghĩa tương thích; không tự nới bộ lọc.' : item.query?.text}</p></div><div className="resource-actions">
      <button type="button" disabled={blocked || item.state !== 'Compatible'} onClick={() => void run(item)}>Chạy trên dữ liệu hiện tại</button><button type="button" disabled={blocked} onClick={() => { setEditing(item); setTitle(item.title); }}>Sửa định nghĩa</button><button type="button" disabled={blocked} onClick={() => setDeleting(item)}>Xóa định nghĩa</button>
    </div></li>)}</ul>{cursor && <button type="button" disabled={blocked} onClick={() => void load(true)}>Tải thêm Saved Search</button>}
    <ResourceFormDialog open={deleting !== null} title="Xóa Saved Search?" busy={busy} dirty={false} onClose={() => setDeleting(null)}><p>Chỉ xóa định nghĩa, không xóa tài nguyên hay Audit.</p><button type="button" onClick={() => { if (!deleting || pending.current) return; const item = deleting, key = createIdempotencyKey(); pending.current = { send: () => deleteSavedSearch(item, key) }; void execute(); }}>Xác nhận xóa định nghĩa</button></ResourceFormDialog>
    {page && <div className="form-panel"><h3>Kết quả hiện tại cho “{page.query}”</h3>{page.providers.map(provider => <p key={provider.resourceType}>{provider.resourceType}: {provider.state}{provider.state === 'Ready' || provider.state === 'Empty' ? ` · ${provider.count}` : ''}</p>)}<ul className="resource-cards">{page.items.map(hit => <li className="resource-card" key={hit.resourceType + hit.id}><div><strong>{hit.title}</strong><p>{hit.snippet}</p></div><button type="button" onClick={() => onOpen(hit)}>Mở nguồn</button></li>)}</ul>{page.nextCursor && <button type="button" disabled={blocked} onClick={() => { const item = items.find(value => value.id === running.current); if (item) void run(item, true); }}>Tải thêm kết quả</button>}</div>}
    <ResourceFormDialog open={leave !== null} title="Rời Saved Searches" dirty={false} busy={busy} onClose={() => setLeave(null)}><p>Rời màn hình sẽ mất bản nháp/mã đối chiếu; request đã gửi có thể đã commit.</p><button disabled={busy} onClick={() => { const proceed = leave?.proceed; pending.current = null; ++listEpoch.current; ++resultEpoch.current; setLeave(null); proceed?.(); }}>Bỏ bản nháp/mã đối chiếu và rời màn hình</button></ResourceFormDialog>
  </section>;
}
