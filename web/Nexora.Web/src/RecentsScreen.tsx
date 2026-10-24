import { useEffect, useRef, useState } from 'react';
import { createIdempotencyKey, NexoraApiError } from './api';
import { ResourceFormDialog } from './ResourceFormDialog';
import { recentCapabilities, recentClear, recentClearPreview, recentList, recentTypes, type RecentCapabilities, type RecentItem, type RecentPage, type RecentType } from './recentApi';

type Intent = { token: string; key: string };
export type RecentView = { resourceType: string; cursor: string };
export function RecentsScreen({ onAuthLost, onOpen, initialClear = false, contextKey = '', onInitialClearConsumed, timeZoneId, locale, initialView, onViewChange }: {
  onAuthLost: () => Promise<void>; onOpen: (type: RecentType, id: string) => void;
  initialClear?: boolean; contextKey?: string; onInitialClearConsumed?: () => void;
  timeZoneId: string; locale: string; initialView?: RecentView; onViewChange?: (value: RecentView) => void;
}) {
  const [type, setType] = useState(initialView?.resourceType ?? ''), [page, setPage] = useState<RecentPage | null>(null), [caps, setCaps] = useState<RecentCapabilities | null>(null);
  const [loading, setLoading] = useState(true), [busy, setBusy] = useState(false), [error, setError] = useState<string | null>(null), [notice, setNotice] = useState<string | null>(null);
  const [preview, setPreview] = useState<string | null>(null), [pending, setPending] = useState<Intent | null>(null), [abandon, setAbandon] = useState(false);
  const pendingRef = useRef<Intent | null>(null), flight = useRef(false), epoch = useRef(0), mounted = useRef(false), reads = useRef<AbortController | null>(null);
  const callbacks = useRef({ onAuthLost, onViewChange }); callbacks.current = { onAuthLost, onViewChange };
  const initialClearConsumed = useRef(false);
  const currentCursor = useRef(initialView?.cursor ?? '');
  const keepReconciling = useRef<HTMLButtonElement>(null), abandonToggle = useRef<HTMLButtonElement>(null);
  useEffect(() => { (abandon ? keepReconciling.current : abandonToggle.current)?.focus(); }, [abandon]);
  const blocked = busy || pending !== null || preview !== null;
  useEffect(() => { mounted.current = true; return () => { mounted.current = false; ++epoch.current; reads.current?.abort(); }; }, []);
  function invalidate() { ++epoch.current; reads.current?.abort(); reads.current = null; setPage(null); }
  function failure(value: unknown) {
    const failure = value instanceof NexoraApiError ? value : new NexoraApiError('Không thể hoàn tất Recents. Hãy thử lại.', 0, 'NetworkError');
    setError(failure.message); setPage(null);
    if ([401, 403, 404, 409].includes(failure.status)) setCaps(null);
    if (failure.status === 401) void callbacks.current.onAuthLost();
    return failure;
  }
  async function load(cursor = '', selectedType = type) {
    if (flight.current || pendingRef.current || preview !== null) return;
    currentCursor.current = cursor; callbacks.current.onViewChange?.({ resourceType: selectedType, cursor });
    invalidate(); const stamp = epoch.current, controller = new AbortController(); reads.current = controller;
    setLoading(true); setError(null); setNotice(null);
    try {
      const current = await recentCapabilities(controller.signal);
      if (!mounted.current || stamp !== epoch.current) return; setCaps(current);
      if (current.canRead) {
        const result = await recentList(selectedType, cursor, controller.signal);
        if (mounted.current && stamp === epoch.current) setPage(result);
      }
    } catch (value) { if (mounted.current && stamp === epoch.current && !controller.signal.aborted) failure(value); }
    finally { if (mounted.current && stamp === epoch.current) { setLoading(false); reads.current = null; } }
  }
  useEffect(() => {
    invalidate(); setCaps(null); setLoading(false);
    if (pendingRef.current || flight.current) {
      setError('Ngữ cảnh quyền đã thay đổi. Giữ nguyên ý định đang chờ để đối chiếu hoặc dừng đối chiếu rõ ràng.'); return;
    }
    if (preview !== null) { setPreview(null); setError('Quyền đã thay đổi. Kiểm tra lại trước khi xem trước phạm vi mới.'); return; }
    void load(currentCursor.current, type);
  }, [type, contextKey]);
  useEffect(() => {
    if (initialClear && !initialClearConsumed.current && !loading && caps?.canClear) {
      initialClearConsumed.current = true; onInitialClearConsumed?.(); void prepare();
    }
  }, [initialClear, loading, caps?.canClear]);
  async function prepare() {
    if (flight.current || pendingRef.current || reads.current) return;
    flight.current = true; invalidate(); const stamp = epoch.current; setBusy(true); setError(null); setNotice(null);
    try { const token = await recentClearPreview(); if (mounted.current && stamp === epoch.current) setPreview(token); }
    catch (value) { if (mounted.current && stamp === epoch.current) failure(value); }
    finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  async function execute() {
    if (flight.current || (!preview && !pendingRef.current)) return;
    const intent = pendingRef.current ?? { token: preview!, key: createIdempotencyKey() };
    pendingRef.current = intent; setPending(intent); flight.current = true; invalidate(); const stamp = epoch.current; setBusy(true); setError(null);
    try {
      await recentClear(intent.token, intent.key);
      if (!mounted.current || stamp !== epoch.current || pendingRef.current !== intent) return;
      pendingRef.current = null; setPending(null); setPreview(null); setAbandon(false);
      currentCursor.current = ''; callbacks.current.onViewChange?.({ resourceType: type, cursor: '' });
      setNotice('Đã xóa metadata trong phạm vi đã xác nhận. Mục mở sau đó được giữ nguyên. Chọn tải lại để đọc hiện tại.');
    } catch (value) {
      if (!mounted.current || stamp !== epoch.current) return;
      const apiError = failure(value);
      if (apiError.status >= 400 && apiError.status < 500 && !['RequestInProgress', 'IdempotencyReplay'].includes(apiError.code ?? '')) { pendingRef.current = null; setPending(null); setPreview(null); }
    } finally { flight.current = false; if (mounted.current) setBusy(false); }
  }
  function abandonPending() {
    if (flight.current) return; invalidate(); pendingRef.current = null; setPending(null); setPreview(null); setAbandon(false);
    setNotice('Đã dừng đối chiếu. Server có thể đã xử lý thao tác; tải lại dữ liệu trước khi tạo ý định mới.');
  }
  function open(item: RecentItem) { if (!blocked && !loading) onOpen(item.resourceType, item.resourceId); }
  function openedLabel(value: string) {
    try { return new Intl.DateTimeFormat(locale, { timeZone: timeZoneId, dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }
    catch { return value; }
  }
  return <section className="content-section" aria-labelledby="recents-title">
    <div className="content-heading"><div><p className="eyebrow">FX25 / DISCOVERY</p><h1 id="recents-title">Đã mở gần đây</h1><p>Lưu tối đa 100 tham chiếu riêng tư; tiêu đề và quyền được đọc lại từ nguồn hiện tại.</p></div>
      <button type="button" className="secondary-button" disabled={loading || blocked} onClick={() => void load()}>Tải lại</button></div>
    <div className="form-panel"><label>Loại nguồn <select value={type} disabled={loading || blocked || !caps?.canRead} onChange={event => { invalidate(); currentCursor.current = ''; setType(event.target.value); }}><option value="">Tất cả</option>{recentTypes.map(value => <option key={value}>{value}</option>)}</select></label>
      <button type="button" className="secondary-button" disabled={loading || blocked || !caps?.canClear} onClick={() => void prepare()}>Xóa lịch sử gần đây…</button></div>
    {error && <p role="alert">{error}</p>}{notice && <p role="status">{notice}</p>}
    {loading ? <p role="status">Đang kiểm tra Recents…</p> : caps && !caps.canRead ? <p>Không có quyền đọc Recents hiện tại.</p> : page && (page.items.length === 0 ? <p>Không có nguồn khả dụng trong trang này.</p> : <ul className="grant-list">{page.items.map(item => <li key={item.id}><span><strong>{item.title}</strong><small>{item.resourceType} · {item.status} · {openedLabel(item.lastOpenedAt)} · {timeZoneId}</small></span><button type="button" className="secondary-button" disabled={blocked} onClick={() => open(item)}>Mở nguồn hiện tại</button></li>)}</ul>)}
    {page?.nextCursor && <button type="button" className="secondary-button" disabled={loading || blocked} onClick={() => void load(page.nextCursor!)}>Trang tiếp</button>}
    {(preview || pending) && <ResourceFormDialog open title="Xóa lịch sử mở gần đây?" busy={busy} closeBlocked={pending !== null} dirty={pending !== null}
      onClose={() => { if (!pendingRef.current && !flight.current) { setPreview(null); setAbandon(false); } }}>
      <button type="button" className="secondary-button" disabled={busy || pending !== null} onClick={() => { if (!pendingRef.current && !flight.current) setPreview(null); }}>Giữ lại Recents</button>
      <p>Xóa metadata Recents của tất cả loại nguồn tại mốc xem trước, kể cả mục đang bị bộ lọc ẩn. Resource và Audit được giữ nguyên; những mục được mở sau mốc này không bị xóa.</p>
      {error && <p role="alert">{error}</p>}
      {pending ? <><p role="alert">{busy ? 'Đang chờ phản hồi cho ý định đã cố định.' : 'Kết quả chưa rõ. Gửi lại đúng ý định để đối chiếu.'}</p><button type="button" disabled={busy} onClick={() => void execute()}>Thử lại đúng thao tác</button>
        {abandon ? <div role="alert"><p>Server có thể đã xử lý. Dừng đối chiếu không hủy kết quả.</p><button ref={keepReconciling} type="button" disabled={busy} onClick={() => setAbandon(false)}>Tiếp tục đối chiếu</button><button type="button" disabled={busy} onClick={abandonPending}>Xác nhận dừng đối chiếu</button></div>
          : <button ref={abandonToggle} type="button" disabled={busy} onClick={() => setAbandon(true)}>Dừng đối chiếu…</button>}</>
        : <button type="button" className="danger-button" disabled={busy} onClick={() => void execute()}>Xóa metadata đã xem trước</button>}
    </ResourceFormDialog>}
  </section>;
}
